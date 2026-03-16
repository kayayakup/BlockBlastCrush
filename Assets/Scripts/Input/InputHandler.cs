// ── InputHandler.cs ───────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Input/InputHandler.cs
//
// Unity 6 — New Input System
// Requires: com.unity.inputsystem (built-in to Unity 6)
// In Project Settings → Player → Active Input Handling → set to "Both" or "New"
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using DG.Tweening;
using Touch      = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Handles all pointer input (mouse + touch) for drag-and-drop placement.
/// Uses the Unity 6 / New Input System APIs exclusively.
/// </summary>
public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    private Camera _cam;
    private Block  _dragging;
    private int    _draggingSlot;

    void Awake() => Instance = this;

    public void Initialize()
    {
        _cam          = Camera.main;
        _draggingSlot = -1;

        // EnhancedTouch must be explicitly enabled — Touch.activeTouches won't
        // populate without this call.
        EnhancedTouchSupport.Enable();
    }

    void OnDestroy()
    {
        if (EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Disable();
    }

    // ── Unity Update ──────────────────────────────────────────────────────────

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        bool    down;
        bool    held;
        bool    up;
        Vector2 screenPos;

        bool hasTouches = Touch.activeTouches.Count > 0;

        if (hasTouches)
        {
            // ── Touch input ───────────────────────────────────────────────────
            var t = Touch.activeTouches[0];
            screenPos = t.screenPosition;
            down      = t.phase == TouchPhase.Began;
            held      = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            up        = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
        }
        else
        {
            // ── Mouse input ───────────────────────────────────────────────────
            var mouse = Mouse.current;
            if (mouse == null) return;

            screenPos = mouse.position.ReadValue();
            down      = mouse.leftButton.wasPressedThisFrame;
            // "held" = button is down but was NOT just pressed this frame
            held      = mouse.leftButton.isPressed && !mouse.leftButton.wasPressedThisFrame;
            up        = mouse.leftButton.wasReleasedThisFrame;
        }

        if      (down && _dragging == null) TryPickUp(screenPos);
        else if (held && _dragging != null) DragUpdate(screenPos);
        else if (up   && _dragging != null) TryDrop(screenPos);
    }

    // ── Coordinate conversion ─────────────────────────────────────────────────

    private Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 wp = _cam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_cam.transform.position.z)));
        return new Vector2(wp.x, wp.y);
    }

    // ── Pick-up ───────────────────────────────────────────────────────────────

    private void TryPickUp(Vector2 screenPos)
    {
        Vector2 world = ScreenToWorld(screenPos);
        for (int i = 0; i < Constants.TRAY_COUNT; i++)
        {
            var block = BlockSpawner.Instance.GetBlock(i);
            if (block == null || !block.IsAvailable) continue;
            if (block.ContainsPoint(world))
            {
                _dragging     = block;
                _draggingSlot = i;
                block.BeginDrag();
                AudioManager.Instance?.PlayPickup();
                return;
            }
        }
    }

    // ── Drag ──────────────────────────────────────────────────────────────────

    private void DragUpdate(Vector2 screenPos)
    {
        Vector2 world   = ScreenToWorld(screenPos);
        // Lift block upward so it's visible above the finger/cursor
        Vector2 lifted  = world + new Vector2(0f, Constants.DRAG_OFFSET_Y);

        _dragging.transform.position = new Vector3(lifted.x, lifted.y, -1f);
        UpdateHighlight(lifted);
    }

    private void UpdateHighlight(Vector2 blockCentre)
    {
        if (!IsOverGrid(blockCentre)) { GridManager.Instance.ClearHighlight(); return; }

        var (col, row) = CalcOriginCell(_dragging.Data, blockCentre);
        bool valid = GridManager.Instance.CanPlace(_dragging.Data, col, row);

        var cells = new List<Vector2Int>();
        foreach (var c in _dragging.Data.Cells)
            cells.Add(new Vector2Int(col + c.x, row + c.y));

        GridManager.Instance.ShowHighlight(cells, valid);
    }

    // ── Drop ──────────────────────────────────────────────────────────────────

    private void TryDrop(Vector2 screenPos)
    {
        Vector2 world   = ScreenToWorld(screenPos);
        Vector2 lifted  = world + new Vector2(0f, Constants.DRAG_OFFSET_Y);

        GridManager.Instance.ClearHighlight();

        var (col, row) = CalcOriginCell(_dragging.Data, lifted);
        bool overGrid  = IsOverGrid(lifted);
        bool canPlace  = overGrid && GridManager.Instance.CanPlace(_dragging.Data, col, row);

        var  block = _dragging;
        int  slot  = _draggingSlot;
        _dragging     = null;
        _draggingSlot = -1;

        if (canPlace)
        {
            Vector3 snapWorld = CalcBlockSnapWorld(block.Data, col, row);

            block.PlaySnapThenHide(snapWorld, () =>
            {
                GridManager.Instance.PlaceBlock(block.Data, col, row, () =>
                {
                    BlockSpawner.Instance.NotifyBlockPlaced(slot);
                });
            });
        }
        else
        {
            block.EndDragReturn();
            AudioManager.Instance?.PlayReturn();
        }
    }

    // ── Coordinate helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the grid (col, row) that corresponds to cell (0,0) of the block,
    /// given the block's bounding-box centre in world space.
    /// </summary>
    private (int col, int row) CalcOriginCell(BlockData data, Vector2 blockCentre)
    {
        float cs = Constants.CELL_SIZE;
        // World position of cell (0,0): centre minus the centre-offset of the shape
        float c00x = blockCentre.x - data.CenterCol * cs;
        float c00y = blockCentre.y - data.CenterRow * cs;

        var gm  = GridManager.Instance;
        int col = Mathf.RoundToInt((c00x - gm.OriginX - cs * 0.5f) / cs);
        int row = Mathf.RoundToInt((c00y - gm.OriginY - cs * 0.5f) / cs);
        return (col, row);
    }

    /// <summary>
    /// World position the Block's transform should be at when placed at (originCol, originRow).
    /// (The block's transform is at the bounding-box centre.)
    /// </summary>
    private Vector3 CalcBlockSnapWorld(BlockData data, int originCol, int originRow)
    {
        var gm = GridManager.Instance;
        float cs = Constants.CELL_SIZE;
        float c00x = gm.OriginX + (originCol + 0.5f) * cs;
        float c00y = gm.OriginY + (originRow + 0.5f) * cs;
        return new Vector3(c00x + data.CenterCol * cs, c00y + data.CenterRow * cs, -1f);
    }

    private bool IsOverGrid(Vector2 world)
    {
        var gm = GridManager.Instance;
        float cs = Constants.CELL_SIZE;
        return world.x >= gm.OriginX &&
               world.x <= gm.OriginX + Constants.GRID_COLS * cs &&
               world.y >= gm.OriginY &&
               world.y <= gm.OriginY + Constants.GRID_ROWS * cs;
    }
}

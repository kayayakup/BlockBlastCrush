// ── BlockSpawner.cs ───────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Blocks/BlockSpawner.cs
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manages the tray of TRAY_COUNT next blocks.
/// Spawns a fresh batch (with staggered spawn animations) whenever all tray slots have been placed.
/// </summary>
public class BlockSpawner : MonoBehaviour
{
    public static BlockSpawner Instance { get; private set; }

    private Block[]     _trayBlocks;
    private BlockData[] _trayData;
    private Vector3[]   _slotPositions;
    private bool[]      _slotFilled;
    private int         _filledCount;
    private Tween       _batchDelayTween;

    void Awake() => Instance = this;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        int n = Constants.TRAY_COUNT;
        _trayBlocks    = new Block[n];
        _trayData      = new BlockData[n];
        _slotPositions = new Vector3[n];
        _slotFilled    = new bool[n];

        // Compute evenly-spaced horizontal slot positions
        float spacing = Constants.TRAY_SLOT_SPACING;
        float startX  = -(n - 1) * 0.5f * spacing;
        for (int i = 0; i < n; i++)
            _slotPositions[i] = new Vector3(startX + i * spacing, Constants.TRAY_Y, 0f);

        // Pre-create Block GameObjects (reused for the whole session)
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject($"TrayBlock_{i}");
            go.transform.SetParent(transform);
            go.AddComponent<BoxCollider2D>(); // satisfies [RequireComponent]
            _trayBlocks[i] = go.AddComponent<Block>();
            go.SetActive(false);
        }

        SpawnBatch();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public Block     GetBlock(int slot)     => _trayBlocks[slot];
    public int       FilledCount            => _filledCount;

    /// <summary>BlockData for every still-available tray slot.</summary>
    public BlockData[] GetAvailableData()
    {
        var list = new List<BlockData>();
        for (int i = 0; i < Constants.TRAY_COUNT; i++)
            if (_slotFilled[i]) list.Add(_trayData[i]);
        return list.ToArray();
    }

    /// <summary>
    /// Called by InputHandler after a block is successfully placed on the grid.
    /// Triggers a new batch if all slots are now empty.
    /// </summary>
    public void NotifyBlockPlaced(int slot)
    {
        _slotFilled[slot] = false;
        _filledCount--;

        if (_filledCount <= 0)
        {
            // All placed — wait briefly then spawn fresh batch
            _batchDelayTween?.Kill();
            _batchDelayTween = DOVirtual.DelayedCall(Constants.ANIM_BATCH_DELAY, () =>
            {
                SpawnBatch();
                GameManager.Instance.CheckGameOver();
            });
        }
        else
        {
            // Remaining pieces might still cause game over
            GameManager.Instance.CheckGameOver();
        }
    }

    /// <summary>Resets all tray slots and spawns a fresh batch immediately.</summary>
    public void ResetTray()
    {
        _batchDelayTween?.Kill();
        for (int i = 0; i < Constants.TRAY_COUNT; i++)
        {
            _trayBlocks[i].Hide();
            _slotFilled[i] = false;
        }
        _filledCount = 0;
        SpawnBatch();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SpawnBatch()
    {
        _filledCount = 0;

        // Sample occupancy once for the whole batch so all three pieces
        // are chosen with the same difficulty context.
        float occ = GridManager.Instance != null
                    ? GridManager.Instance.GetOccupancyRatio()
                    : 0f;

        // On a tight grid, guarantee at least ONE piece in the batch fits.
        // We generate all three first, then do one safety pass.
        var candidates = new BlockData[Constants.TRAY_COUNT];
        for (int i = 0; i < Constants.TRAY_COUNT; i++)
            candidates[i] = BlockDefinitions.GetRandom(occ);

        // Safety pass: if none of the three fit anywhere, replace one with
        // a single-cell piece (always fits while any cell is free).
        if (occ > 0.55f && GridManager.Instance != null)
        {
            bool anyFits = false;
            foreach (var d in candidates)
                if (GridManager.Instance.CanPlaceAnywhere(d)) { anyFits = true; break; }

            if (!anyFits)
            {
                // Force the middle slot to a guaranteed-fit tiny piece
                candidates[1] = BlockDefinitions.GetGuaranteedFit();
            }
        }

        for (int i = 0; i < Constants.TRAY_COUNT; i++)
        {
            var data       = candidates[i];
            _trayData[i]   = data;
            _slotFilled[i] = true;
            _filledCount++;

            int   capturedI = i;
            var   capturedD = data;
            float delay     = i * 0.06f;
            DOVirtual.DelayedCall(delay, () => {
                _trayBlocks[capturedI].Setup(capturedD, capturedI, _slotPositions[capturedI]);
                if (capturedI == 0) AudioManager.Instance?.PlaySpawn();
            });
        }
    }
}

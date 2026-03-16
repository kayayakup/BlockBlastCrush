// ── Block.cs ──────────────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Blocks/Block.cs
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Visual and logical representation of one tray piece.
/// Reused across the session — call Setup() to reconfigure with new data.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Block : MonoBehaviour
{
    // ── State ─────────────────────────────────────────────────────────────────
    public BlockData Data        { get; private set; }
    public int       TraySlot    { get; private set; }
    public bool      IsAvailable { get; private set; }

    private Vector3 _trayPos;
    private Vector3 _trayScale;

    private BoxCollider2D _col;

    // Pooled cell objects that make up the visible piece
    private readonly List<GameObject>     _cellObjects   = new List<GameObject>();
    private readonly List<SpriteRenderer> _cellRenderers = new List<SpriteRenderer>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _col           = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reconfigures this block with new data at tray slot position.
    /// Animates a spawn effect (scale-up + fade-in).
    /// </summary>
    public void Setup(BlockData data, int slot, Vector3 slotPos)
    {
        Data        = data;
        TraySlot    = slot;
        _trayPos    = slotPos;
        _trayScale  = Vector3.one * Constants.TRAY_SCALE;

        ReturnCellsToPool();
        SpawnCells();
        ResizeCollider();

        // Always re-enable collider — may have been disabled by BeginDrag()
        // or PlaySnapThenHide() on the previous use of this recycled Block object.
        _col.enabled = true;

        transform.position   = slotPos;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(true);
        IsAvailable = true;

        // Spawn animation
        DOTween.Kill(transform);
        transform.DOScale(_trayScale, Constants.ANIM_SPAWN).SetEase(Ease.OutBack);

        foreach (var sr in _cellRenderers)
        {
            var c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, 0f);
            sr.DOFade(1f, Constants.ANIM_SPAWN * 0.9f);
        }
    }

    /// <summary>Called when the player picks this block up to drag.</summary>
    public void BeginDrag()
    {
        _col.enabled = false;
        SetCellSortOrder(10);

        DOTween.Kill(transform);
        transform.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutQuad);
    }

    /// <summary>Returns the block to its tray slot smoothly.</summary>
    public void EndDragReturn()
    {
        _col.enabled = true;
        SetCellSortOrder(5);

        DOTween.Kill(transform);
        DOTween.Sequence()
               .Append(transform.DOMove(_trayPos, 0.22f).SetEase(Ease.OutQuad))
               .Join(transform.DOScale(_trayScale, 0.22f).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// Animates the block snapping to a grid position, then hides it.
    /// <paramref name="onComplete"/> is called when the snap finishes.
    /// </summary>
    public void PlaySnapThenHide(Vector3 gridSnapPos, System.Action onComplete)
    {
        IsAvailable  = false;
        _col.enabled = false;
        DOTween.Kill(transform);

        transform.DOMove(gridSnapPos, Constants.ANIM_SNAP)
                 .SetEase(Ease.OutQuad)
                 .OnComplete(() =>
                 {
                     gameObject.SetActive(false);
                     onComplete?.Invoke();
                 });
    }

    /// <summary>Immediately hides the block (no animation).</summary>
    public void HideImmediate()
    {
        IsAvailable  = false;
        _col.enabled = false;
        DOTween.Kill(transform);
        gameObject.SetActive(false);
    }

    /// <summary>Hides without returning pooled cells (cells stay as children until Setup).</summary>
    public void Hide()
    {
        IsAvailable  = false;
        _col.enabled = false;
        gameObject.SetActive(false);
    }

    /// <summary>True if the given world point falls inside this block's collider.</summary>
    public bool ContainsPoint(Vector2 worldPoint)
    {
        if (!isActiveAndEnabled || !IsAvailable) return false;
        return _col.OverlapPoint(worldPoint);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SpawnCells()
    {
        float cs    = Constants.CELL_SIZE;
        float vSize = cs * Constants.CELL_VISUAL_RATIO;

        foreach (var cell in Data.Cells)
        {
            var go = PoolManager.Instance.GetCell();
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(
                (cell.x - Data.CenterCol) * cs,
                (cell.y - Data.CenterRow) * cs,
                0f);
            go.transform.localScale = Vector3.one * vSize;

            var sr          = go.GetComponent<SpriteRenderer>();
            sr.sprite       = TextureUtils.GetBlockSprite(Data.Color);
            sr.color        = Color.white;   // bevel colour is baked into the sprite
            sr.sortingOrder = 5;

            _cellObjects.Add(go);
            _cellRenderers.Add(sr);
        }
    }

    private void ReturnCellsToPool()
    {
        foreach (var go in _cellObjects)
            PoolManager.Instance.ReturnCell(go);
        _cellObjects.Clear();
        _cellRenderers.Clear();
    }

    private void ResizeCollider()
    {
        float cs  = Constants.CELL_SIZE;
        float pad = cs * 5f;   // extra touch area for mobile comfort
        _col.size   = new Vector2(Data.BBoxWidth  * cs + pad, Data.BBoxHeight * cs + pad);
        _col.offset = Vector2.zero;
    }

    private void SetCellSortOrder(int order)
    {
        foreach (var sr in _cellRenderers) sr.sortingOrder = order;
    }
}

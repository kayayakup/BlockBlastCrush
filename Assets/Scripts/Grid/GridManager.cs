// ── GridManager.cs ────────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Grid/GridManager.cs
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Owns the deterministic 8×8 grid.
/// Maintains two sprite-renderer layers per cell:
///   _cellSR   – main visual (dark when empty, block-colour when occupied)
///   _overlaySR – transparent highlight overlay shown during drag
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    // ── State ──────────────────────────────────────────────────────────────────
    private bool[,] _occupied;
    private Color[,] _cellColor;

    // ── Visuals ────────────────────────────────────────────────────────────────
    private SpriteRenderer[,] _cellSR;     // main cell visual
    private SpriteRenderer[,] _overlaySR;  // highlight overlay
    private SpriteRenderer _bgSR;           // background sprite renderer

    // ── Geometry (world space) ─────────────────────────────────────────────────
    public float OriginX { get; private set; }
    public float OriginY { get; private set; }

    // ── Highlight tracking ─────────────────────────────────────────────────────
    private readonly List<Vector2Int> _highlighted = new List<Vector2Int>();
    private int _consecutiveClears = 0;

    // ── Events ─────────────────────────────────────────────────────────────────
    /// <summary>Fired when lines are cleared. Args: (fullCols, fullRows, cellCount).</summary>
    public event System.Action<List<int>, List<int>, int> OnLinesCleared;

    void Awake()
    {
        Instance = this;
        if (StyleManager.Instance != null)
            StyleManager.Instance.OnStyleChanged += RefreshAllCells;
    }

    void OnDestroy()
    {
        if (StyleManager.Instance != null)
            StyleManager.Instance.OnStyleChanged -= RefreshAllCells;
    }

    // ── Initialisation ─────────────────────────────────────────────────────────

    public void Initialize()
    {
        int C = Constants.GRID_COLS, R = Constants.GRID_ROWS;
        _occupied = new bool[C, R];
        _cellColor = new Color[C, R];
        _cellSR = new SpriteRenderer[C, R];
        _overlaySR = new SpriteRenderer[C, R];

        float cs = Constants.CELL_SIZE;
        OriginX = Constants.GRID_CENTER_X - C * cs * 0.5f;
        OriginY = Constants.GRID_CENTER_Y - R * cs * 0.5f;

        BuildBackground();
        BuildCells();
    }

    // ── Grid Geometry ──────────────────────────────────────────────────────────

    /// <summary>World-space centre of cell (col, row).</summary>
    public Vector3 CellWorldPos(int col, int row) =>
        new Vector3(
            OriginX + (col + 0.5f) * Constants.CELL_SIZE,
            OriginY + (row + 0.5f) * Constants.CELL_SIZE,
            0f);

    /// <summary>Snapped (col, row) for a given world position.</summary>
    public Vector2Int WorldToCell(Vector2 worldPos)
    {
        float cs = Constants.CELL_SIZE;
        return new Vector2Int(
            Mathf.RoundToInt((worldPos.x - OriginX - cs * 0.5f) / cs),
            Mathf.RoundToInt((worldPos.y - OriginY - cs * 0.5f) / cs));
    }

    /// <summary>
    /// Returns 0..1 ratio of filled cells to total cells.
    /// Used by BlockSpawner to bias piece selection when the grid is crowded.
    /// </summary>
    public float GetOccupancyRatio()
    {
        int filled = 0;
        for (int col = 0; col < Constants.GRID_COLS; col++)
            for (int row = 0; row < Constants.GRID_ROWS; row++)
                if (_occupied[col, row]) filled++;
        return (float)filled / (Constants.GRID_COLS * Constants.GRID_ROWS);
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    public bool InBounds(int col, int row) =>
        col >= 0 && col < Constants.GRID_COLS &&
        row >= 0 && row < Constants.GRID_ROWS;

    public bool IsFree(int col, int row) => InBounds(col, row) && !_occupied[col, row];

    /// <summary>True when all cells of data fit at (originCol, originRow).</summary>
    public bool CanPlace(BlockData data, int originCol, int originRow)
    {
        foreach (var c in data.Cells)
            if (!IsFree(originCol + c.x, originRow + c.y))
                return false;
        return true;
    }

    /// <summary>True when data can be placed anywhere on the current grid.</summary>
    public bool CanPlaceAnywhere(BlockData data)
    {
        for (int c = 0; c < Constants.GRID_COLS; c++)
            for (int r = 0; r < Constants.GRID_ROWS; r++)
                if (CanPlace(data, c, r)) return true;
        return false;
    }

    /// <summary>
    /// Returns indices into BlockDefinitions.AllShapes that can be placed
    /// somewhere on the current grid. Used by BlockSpawner for gap-aware spawning.
    /// </summary>
    public List<int> FindFittingShapeIndices()
    {
        var fitting = new List<int>();
        var shapes = BlockDefinitions.AllShapes;
        // Dummy colour – CanPlace only checks geometry
        Color dummyColor = Color.white;

        for (int i = 0; i < shapes.Length; i++)
        {
            var data = new BlockData(shapes[i], dummyColor);
            if (CanPlaceAnywhere(data))
                fitting.Add(i);
        }
        return fitting;
    }

    // ── Placement ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Stamps the block onto the grid, awards cell points, then runs line-clearing.
    /// <paramref name="onDone"/> fires after all animations complete.
    /// </summary>
    public void PlaceBlock(BlockData data, int originCol, int originRow,
                           System.Action onDone = null)
    {
        int cellsPlaced = 0;

        foreach (var c in data.Cells)
        {
            int col = originCol + c.x;
            int row = originRow + c.y;

            _occupied[col, row] = true;
            _cellColor[col, row] = data.Color;

            var sr = _cellSR[col, row];
            sr.sprite = TextureUtils.GetBlockSprite(data.Color);
            sr.color = Color.white;

            // Placement punch
            DOTween.Kill(sr.transform);
            sr.transform.localScale =
                Vector3.one * Constants.CELL_SIZE * Constants.CELL_VISUAL_RATIO;
            sr.transform
              .DOPunchScale(Vector3.one * 0.20f, Constants.ANIM_SNAP, 2, 0.45f)
              .SetEase(Ease.OutQuad);

            cellsPlaced++;
        }

        AudioManager.Instance?.PlayPlace();
        ScoreManager.Instance.AddScore(cellsPlaced * Constants.POINTS_PER_CELL);

        // Brief pause so the punch plays before clearing begins
        DOVirtual.DelayedCall(Constants.ANIM_SNAP + 0.06f, () => CheckAndClearLines(onDone));
    }

    // ── Highlight ──────────────────────────────────────────────────────────────

    public void ShowHighlight(IEnumerable<Vector2Int> cells, bool valid)
    {
        ClearHighlight();
        if (!valid)
        {
            foreach (var cell in cells)
                if (InBounds(cell.x, cell.y))
                {
                    _overlaySR[cell.x, cell.y].color = Constants.HighlightInvalid;
                    _highlighted.Add(cell);
                }
            return;
        }

        // 1. Temporarily mark potential cells as occupied to check for completed lines
        List<Vector2Int> proposed = new List<Vector2Int>(cells);
        foreach (var cell in proposed)
            if (InBounds(cell.x, cell.y)) _occupied[cell.x, cell.y] = true;

        var fullCols = new List<int>();
        var fullRows = new List<int>();
        for (int c = 0; c < Constants.GRID_COLS; c++)
        {
            bool full = true;
            for (int r = 0; r < Constants.GRID_ROWS; r++) if (!_occupied[c, r]) { full = false; break; }
            if (full) fullCols.Add(c);
        }
        for (int r = 0; r < Constants.GRID_ROWS; r++)
        {
            bool full = true;
            for (int c = 0; c < Constants.GRID_COLS; c++) if (!_occupied[c, r]) { full = false; break; }
            if (full) fullRows.Add(r);
        }

        // Revert temporary occupation
        foreach (var cell in proposed)
            if (InBounds(cell.x, cell.y)) _occupied[cell.x, cell.y] = false;

        // 2. Draw line highlights (yellow)
        foreach (int c in fullCols)
        {
            for (int r = 0; r < Constants.GRID_ROWS; r++)
            {
                _overlaySR[c, r].color = Constants.HighlightLine;
                _highlighted.Add(new Vector2Int(c, r));
            }
        }
        foreach (int r in fullRows)
        {
            for (int c = 0; c < Constants.GRID_COLS; c++)
            {
                _overlaySR[c, r].color = Constants.HighlightLine;
                _highlighted.Add(new Vector2Int(c, r));
            }
        }

        // 3. Draw block highlight (white) - overrides line highlight if overlapping
        foreach (var cell in proposed)
        {
            if (InBounds(cell.x, cell.y))
            {
                _overlaySR[cell.x, cell.y].color = Constants.HighlightValid;
                _highlighted.Add(cell);
            }
        }
    }

    public void ClearHighlight()
    {
        foreach (var cell in _highlighted)
            if (InBounds(cell.x, cell.y))
                _overlaySR[cell.x, cell.y].color = Color.clear;
        _highlighted.Clear();
    }

    // ── Reset ──────────────────────────────────────────────────────────────────

    public void ResetGrid()
    {
        ClearHighlight();
        float vScale = Constants.CELL_SIZE * Constants.CELL_VISUAL_RATIO;

        for (int c = 0; c < Constants.GRID_COLS; c++)
        {
            for (int r = 0; r < Constants.GRID_ROWS; r++)
            {
                _occupied[c, r] = false;
                _cellColor[c, r] = default;

                var sr = _cellSR[c, r];
                DOTween.Kill(sr.transform);
                DOTween.Kill(sr);
                sr.sprite = TextureUtils.WhiteCellSprite;
                sr.color = Constants.CellEmptyColor;
                sr.transform.localScale = Vector3.one * vScale;
            }
        }
    }
    public void UpdateGridColors(Color bgColor, Color gridBgColor)
    {
        if (_bgSR != null) _bgSR.color = gridBgColor;

        // Update empty cell colors to match background color (invisible on BG but visible on GridBG)
        for (int c = 0; c < Constants.GRID_COLS; c++)
        {
            for (int r = 0; r < Constants.GRID_ROWS; r++)
            {
                if (!_occupied[c, r])
                {
                    _cellSR[c, r].color = Constants.CellEmptyColor;
                }
            }
        }
    }

    // ── Private: build visuals ──────────────────────────────────────────────────

    private void BuildBackground()
    {
        float cs = Constants.CELL_SIZE;
        float gridWidth = Constants.GRID_COLS * cs;
        float gridHeight = Constants.GRID_ROWS * cs;

        var bg = new GameObject("GridBG");
        bg.transform.SetParent(transform);
        bg.transform.position = new Vector3(Constants.GRID_CENTER_X, Constants.GRID_CENTER_Y, 0.5f);
        bg.transform.localScale = Vector3.one;

        _bgSR = bg.AddComponent<SpriteRenderer>();
        _bgSR.sortingOrder = -1; // En arkada görünmesi için

        // Resources klasöründen "GridBG" sprite'ını yükle
        Sprite gridBGSprite = Resources.Load<Sprite>("GridBG");

        if (gridBGSprite != null)
        {
            _bgSR.sprite = gridBGSprite;

            // Sprite'ın orijinal boyutunu al
            float spriteWidth = gridBGSprite.bounds.size.x - 1.25f;
            float spriteHeight = gridBGSprite.bounds.size.y - 1.25f;

            // Grid boyutlarına göre scale hesapla (en-boy oranını korumadan)
            float scaleX = gridWidth / spriteWidth;
            float scaleY = gridHeight / spriteHeight;
            bg.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            Debug.LogError("GridBG sprite not found in Resources folder! Please place a sprite named 'GridBG' in Assets/Resources/");

            // Fallback: eğer sprite bulunamazsa, eski yöntemle düz renkli bir arkaplan oluştur
            _bgSR.sprite = TextureUtils.CreateRoundedRect(100, 100, 10, Constants.GridBgColor);
            bg.transform.localScale = new Vector3(gridWidth, gridHeight, 1f);
        }
    }

    private void BuildCells()
    {
        float cs = Constants.CELL_SIZE;
        float vScale = cs * Constants.CELL_VISUAL_RATIO;

        for (int c = 0; c < Constants.GRID_COLS; c++)
        {
            for (int r = 0; r < Constants.GRID_ROWS; r++)
            {
                Vector3 pos = CellWorldPos(c, r);

                // ── Main cell sprite ──────────────────────────────────────
                var cellGO = new GameObject($"Cell_{c}_{r}");
                cellGO.transform.SetParent(transform);
                cellGO.transform.position = pos + new Vector3(0, 0, 0.3f);
                cellGO.transform.localScale = Vector3.one * vScale;

                var csr = cellGO.AddComponent<SpriteRenderer>();
                csr.sprite = TextureUtils.WhiteCellSprite;
                csr.color = Constants.CellEmptyColor;
                csr.sortingOrder = 1;
                _cellSR[c, r] = csr;

                // ── Overlay sprite (highlight) ─────────────────────────────
                var ovGO = new GameObject($"Overlay_{c}_{r}");
                ovGO.transform.SetParent(transform);
                ovGO.transform.position = pos + new Vector3(0, 0, 0.1f);
                ovGO.transform.localScale = Vector3.one * vScale;

                var osr = ovGO.AddComponent<SpriteRenderer>();
                osr.sprite = TextureUtils.WhiteCellSprite;
                osr.color = Color.clear;
                osr.sortingOrder = 2;
                _overlaySR[c, r] = osr;
            }
        }
    }

    // ── Private: line clearing ──────────────────────────────────────────────────

    private void CheckAndClearLines(System.Action onDone)
    {
        int C = Constants.GRID_COLS, R = Constants.GRID_ROWS;
        var fullCols = new List<int>();
        var fullRows = new List<int>();

        for (int c = 0; c < C; c++)
        {
            bool full = true;
            for (int r = 0; r < R; r++) { if (!_occupied[c, r]) { full = false; break; } }
            if (full) fullCols.Add(c);
        }
        for (int r = 0; r < R; r++)
        {
            bool full = true;
            for (int c = 0; c < C; c++) { if (!_occupied[c, r]) { full = false; break; } }
            if (full) fullRows.Add(r);
        }

        if (fullCols.Count == 0 && fullRows.Count == 0)
        {
            _consecutiveClears = 0;
            onDone?.Invoke();
            return;
        }

        _consecutiveClears++;

        // Collect unique cells to clear
        var toClear = new HashSet<Vector2Int>();
        foreach (int c in fullCols) for (int r = 0; r < R; r++) toClear.Add(new Vector2Int(c, r));
        foreach (int r in fullRows) for (int c = 0; c < C; c++) toClear.Add(new Vector2Int(c, r));

        int lineCount = fullCols.Count + fullRows.Count;
        ScoreManager.Instance.AddScore(lineCount * Constants.POINTS_PER_LINE);
        OnLinesCleared?.Invoke(fullCols, fullRows, toClear.Count);

        if (lineCount >= 2)
        {
            StyleManager.Instance?.NextStyle();
        }

        // --- New Feedback Logic ---
        bool isCombo = _consecutiveClears > 1;
        AudioManager.Instance?.PlayFeedbackSound(lineCount, isCombo);

        string feedbackText = "";
        int feedbackType = 0;

        if (isCombo)
        {
            feedbackText = "COMBO";
            feedbackType = 3;
        }
        else
        {
            if (lineCount == 1) { feedbackText = "GOOD"; feedbackType = 0; }
            else if (lineCount == 2) { feedbackText = "AMAZING"; feedbackType = 1; }
            else if (lineCount >= 3) { feedbackText = "UNBELIEVABLE"; feedbackType = 2; }
        }

        if (!string.IsNullOrEmpty(feedbackText))
        {
            UIManager.Instance?.ShowFeedback(feedbackText, feedbackType);
        }

        AudioManager.Instance?.PlayLineClear(lineCount);

        // Animate cells out (staggered)
        float maxDelay = 0f;
        float vScale = Constants.CELL_SIZE * Constants.CELL_VISUAL_RATIO;
        int idx = 0;

        foreach (var cell in toClear)
        {
            int cc = cell.x, rr = cell.y;
            float delay = idx * Constants.ANIM_CLEAR_DELAY;
            if (delay > maxDelay) maxDelay = delay;

            var sr = _cellSR[cc, rr];
            DOTween.Kill(sr.transform);
            DOTween.Kill(sr);

            var seq = DOTween.Sequence();
            seq.AppendInterval(delay);
            seq.Append(sr.transform
                         .DOScale(0f, Constants.ANIM_CLEAR_FADE)
                         .SetEase(Ease.InBack));
            seq.Join(sr.DOFade(0f, Constants.ANIM_CLEAR_FADE));
            seq.AppendCallback(() =>
            {
                _occupied[cc, rr] = false;
                _cellColor[cc, rr] = default;
                sr.sprite = TextureUtils.WhiteCellSprite;
                var c2 = Constants.CellEmptyColor;
                sr.color = c2;
                sr.transform.localScale = Vector3.one * vScale;
            });

            idx++;
        }

        float waitTime = maxDelay + Constants.ANIM_CLEAR_FADE + 0.05f;

        DOVirtual.DelayedCall(waitTime, () =>
        {
            PulseRemaining(toClear);
            DOVirtual.DelayedCall(Constants.ANIM_PULSE * 0.55f, () => onDone?.Invoke());
        });
    }

    private void PulseRemaining(HashSet<Vector2Int> cleared)
    {
        for (int c = 0; c < Constants.GRID_COLS; c++)
        {
            for (int r = 0; r < Constants.GRID_ROWS; r++)
            {
                if (!_occupied[c, r]) continue;
                DOTween.Kill(_cellSR[c, r].transform);
                _cellSR[c, r].transform
                    .DOPunchScale(Vector3.one * 0.12f, Constants.ANIM_PULSE, 1, 0.3f);
            }
        }
    }

    private void RefreshAllCells()
    {
        for (int c = 0; c < Constants.GRID_COLS; c++)
        {
            for (int r = 0; r < Constants.GRID_ROWS; r++)
            {
                if (_occupied[c, r])
                {
                    var sr = _cellSR[c, r];
                    sr.sprite = TextureUtils.GetBlockSprite(_cellColor[c, r]);
                }
            }
        }
    }
}
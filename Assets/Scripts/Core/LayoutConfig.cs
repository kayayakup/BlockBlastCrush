// ── LayoutConfig.cs ───────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Core/LayoutConfig.cs
//
// Call LayoutConfig.Compute() ONCE in Bootstrap.Awake() before anything else.
// All other classes read from the static properties here instead of hard-coded
// Constants — so the game adapts to any screen resolution automatically.
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

public static class LayoutConfig
{
    // ── Camera ────────────────────────────────────────────────────────────────
    public static float OrthoSize     { get; private set; }  // half world-height
    public static float WorldWidth    { get; private set; }  // full world-width
    public static float WorldHeight   { get; private set; }  // full world-height

    // ── Grid ──────────────────────────────────────────────────────────────────
    public static float CellSize      { get; private set; }  // world-units per cell
    public static float GridCenterX   { get; private set; }
    public static float GridCenterY   { get; private set; }

    // ── Tray ──────────────────────────────────────────────────────────────────
    public static float TrayY         { get; private set; }
    public static float TraySlotSpacing { get; private set; }
    public static float TrayScale     { get; private set; }
    public static float DragOffsetY   { get; private set; }

    // ── UI (world-space) ─────────────────────────────────────────────────────
    public static float TopBarWorldY  { get; private set; }  // top of usable area
    public static float TopBarHeight  { get; private set; }  // in world units

    // ── Reference texture PPU ────────────────────────────────────────────────
    /// <summary>Pixels-Per-Unit used for all generated sprites.</summary>
    public static float PPU           { get; private set; }

    // ── Computed ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Call once in Bootstrap.Awake() before every other Initialize().
    /// Adapts layout to the actual runtime Screen.width / Screen.height.
    /// </summary>
    public static void Compute()
    {
        int sw = Screen.width;
        int sh = Screen.height;

        // Protect against 0 before the window is fully set up
        if (sw <= 0) sw = 1080;
        if (sh <= 0) sh = 2400;

        float aspect = (float)sw / sh;   // e.g. 0.45 for 1080×2400

        // ── World height is fixed at 26 units (ortho = 13).
        // All other sizes derive from that + the actual aspect ratio.
        OrthoSize   = 15.5f;
        WorldHeight = OrthoSize * 2f;            // 26
        WorldWidth  = WorldHeight * aspect;      // 11.7 @ 1080×2400

        // ── The grid should fill ~82% of the available width
        float gridWorldW = WorldWidth * 0.82f;
        CellSize = gridWorldW / Constants.GRID_COLS;   // ≈1.20 @ 1080×2400

        // ── Vertical layout:
        //    Top 9% → top bar (score/crown/gear)
        //    Bottom 16% → tray
        //    Middle → grid, vertically centred
        float topBarFrac  = 0.04f;
        float trayFrac    = 0.16f;

        float topBarWorldH = WorldHeight * topBarFrac;     // ≈2.34
        float trayWorldH   = WorldHeight * trayFrac;       // ≈4.16

        float usableTop    =  OrthoSize - topBarWorldH;    // top of grid area
        float usableBot    = -OrthoSize + trayWorldH;      // bottom of grid area

        float gridWorldH   = CellSize * Constants.GRID_ROWS;
        float midUsable    = (usableTop + usableBot) * 0.5f;
        GridCenterX = 0f;
        GridCenterY = midUsable;

        // ── Tray — sits in the bottom strip, centred vertically in that strip
        TrayY = -OrthoSize + trayWorldH * 1.1f;

        // Tray slot spacing: the three slots span ~80% of world width
        TraySlotSpacing = WorldWidth * 0.65f / (Constants.TRAY_COUNT - 1);
        // Clamp to sensible range
        TraySlotSpacing = Mathf.Clamp(TraySlotSpacing, CellSize * 2.5f, CellSize * 5f);

        // Tray blocks are drawn at a larger scale for easier touch interaction
        TrayScale = Mathf.Clamp(WorldWidth * 0.14f / (CellSize * 2.5f), 0.50f, 0.75f);

        // Lift block above finger during drag
        DragOffsetY = 5.0f;

        // Top bar info for UIManager
        TopBarHeight = topBarWorldH;
        TopBarWorldY = OrthoSize;   // top edge (canvas origin)

        // PPU: how many texture pixels map to 1 world unit.
        // Higher = sharper sprites on high-DPI screens.
        // We target ~180 px/unit to get crisp 512-px blocks at our cell sizes.
        PPU = Mathf.Clamp(sw / WorldWidth, 80f, 220f);

        Debug.Log($"[LayoutConfig] Screen={sw}×{sh}  aspect={aspect:F3}  " +
                  $"ortho={OrthoSize}  cellSize={CellSize:F3}  " +
                  $"gridCY={GridCenterY:F3}  trayY={TrayY:F3}  PPU={PPU:F1}");
    }
}

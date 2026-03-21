// ── Constants.cs ─────────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Core/Constants.cs
//
// LAYOUT values (cell size, tray position, etc.) are NOT stored here as
// hard-coded numbers.  They live in LayoutConfig and are computed at runtime
// so the game adapts to any screen resolution (including 1080×2400).
//
// This file holds ONLY values that never change: grid dimensions, scoring,
// animation timings, and colours.
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

public static class Constants
{
    // ── Grid dimensions (cell count only) ────────────────────────────────────
    public const int GRID_COLS = 8;
    public const int GRID_ROWS = 8;
    public const int TRAY_COUNT = 6;

    // ── Scoring ───────────────────────────────────────────────────────────────
    public const int POINTS_PER_CELL = 1;
    public const int POINTS_PER_LINE = 18;

    // ── Animation durations (seconds) ────────────────────────────────────────
    public const float ANIM_SNAP         = 0.05f;
    public const float ANIM_CLEAR_FADE   = 0.08f;
    public const float ANIM_CLEAR_DELAY  = 0.005f;
    public const float ANIM_SPAWN        = 0.12f;
    public const float ANIM_PULSE        = 0.10f;
    public const float ANIM_BATCH_DELAY  = 0.15f;
    public const float CELL_VISUAL_RATIO = 0.98f;  // sprite fills 98% of cell slot (very close)

    // ── Colours ───────────────────────────────────────────────────────────────
    public static readonly Color GridBgColor      = new Color(0.16f, 0.18f, 0.28f);
    public static readonly Color CellEmptyColor   = new Color(0.20f, 0.22f, 0.34f);
    public static readonly Color TrayBgColor      = new Color(0.12f, 0.14f, 0.24f);
    public static readonly Color HighlightValid   = new Color(1.00f, 1.00f, 1.00f, 0.55f);
    public static readonly Color HighlightInvalid = new Color(1.00f, 0.12f, 0.12f, 0.55f);
    public static readonly Color HighlightLine    = new Color(1.00f, 1.00f, 0.40f, 0.75f); // Bright yellow highlight for full lines
    public static readonly Color TopBarColor      = new Color(0.18f, 0.20f, 0.32f);

    // ── Layout shortcuts (redirect to LayoutConfig) ───────────────────────────
    // All geometry reads through here so existing code needs zero changes.
    public static float CELL_SIZE       => LayoutConfig.CellSize;
    public static float GRID_CENTER_X   => LayoutConfig.GridCenterX;
    public static float GRID_CENTER_Y   => LayoutConfig.GridCenterY;
    public static float TRAY_Y          => LayoutConfig.TrayY;
    public static float TRAY_SLOT_SPACING => LayoutConfig.TraySlotSpacing;
    public static float TRAY_ROW_SPACING  => LayoutConfig.TrayRowSpacing;
    public static float TRAY_SCALE      => LayoutConfig.TrayScale;
    public static float DRAG_OFFSET_Y   => LayoutConfig.DragOffsetY;
    public static float CAMERA_ORTHO_SIZE => LayoutConfig.OrthoSize;
}

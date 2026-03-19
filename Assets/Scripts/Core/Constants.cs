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
    public const int TRAY_COUNT = 3;

    // ── Scoring ───────────────────────────────────────────────────────────────
    public const int POINTS_PER_CELL = 1;
    public const int POINTS_PER_LINE = 18;

    // ── Animation durations (seconds) ────────────────────────────────────────
    public const float ANIM_SNAP         = 0.13f;
    public const float ANIM_CLEAR_FADE   = 0.20f;
    public const float ANIM_CLEAR_DELAY  = 0.022f;
    public const float ANIM_SPAWN        = 0.30f;
    public const float ANIM_PULSE        = 0.26f;
    public const float ANIM_BATCH_DELAY  = 0.40f;
    public const float CELL_VISUAL_RATIO = 0.87f;  // sprite fills 87% of cell slot

    // ── Colours ───────────────────────────────────────────────────────────────
    public static readonly Color BgColor          = new Color(0.318f, 0.767f, 0.508f);
    public static readonly Color GridBgColor      = new Color(0.055f, 0.239f, 0.490f);
    public static readonly Color CellEmptyColor   = new Color(0.071f, 0.180f, 0.365f);
    public static readonly Color TrayBgColor      = new Color(0.040f, 0.170f, 0.370f);
    public static readonly Color HighlightValid   = new Color(1.00f, 1.00f, 1.00f, 0.55f);
    public static readonly Color HighlightInvalid = new Color(1.00f, 0.12f, 0.12f, 0.55f);
    public static readonly Color TopBarColor      = new Color(0.060f, 0.290f, 0.580f);

    // ── Layout shortcuts (redirect to LayoutConfig) ───────────────────────────
    // All geometry reads through here so existing code needs zero changes.
    public static float CELL_SIZE       => LayoutConfig.CellSize;
    public static float GRID_CENTER_X   => LayoutConfig.GridCenterX;
    public static float GRID_CENTER_Y   => LayoutConfig.GridCenterY;
    public static float TRAY_Y          => LayoutConfig.TrayY;
    public static float TRAY_SLOT_SPACING => LayoutConfig.TraySlotSpacing;
    public static float TRAY_SCALE      => LayoutConfig.TrayScale;
    public static float DRAG_OFFSET_Y   => LayoutConfig.DragOffsetY;
    public static float CAMERA_ORTHO_SIZE => LayoutConfig.OrthoSize;
}

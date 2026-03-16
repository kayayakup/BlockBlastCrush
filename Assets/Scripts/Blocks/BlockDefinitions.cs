// ── BlockDefinitions.cs ───────────────────────────────────────────────────────
// Place in: Assets/Scripts/Blocks/BlockDefinitions.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Complete catalogue of all block shapes with every relevant rotation baked in.
/// Each shape is stored in ALL of its distinct orientations as separate entries
/// so no runtime rotation is needed (matching 1010!/Block Puzzle gameplay).
///
/// This version contains only the shapes requested:
/// - 2×2 square (4'lü küp)
/// - 3×3 square (9'lu küp)
/// - L shapes with 3, 4, 5 cells
/// - I shapes with 2, 3, 4, 5, 6 cells
/// - T shape (4 cells)
/// - Z shape (4 cells)
///
/// Shape coordinate system: col=x (right), row=y (up), origin at min corner.
/// </summary>
public static class BlockDefinitions
{
    // ── Colour palette ────────────────────────────────────────────────────────
    private static readonly Color[] Palette =
    {
        new Color(0.55f, 0.28f, 0.90f),   // purple
        new Color(0.15f, 0.76f, 0.32f),   // green
        new Color(0.96f, 0.45f, 0.08f),   // orange
        new Color(0.10f, 0.72f, 0.88f),   // cyan
        new Color(0.93f, 0.73f, 0.05f),   // yellow
        new Color(0.95f, 0.22f, 0.28f),   // red
        new Color(0.20f, 0.50f, 0.95f),   // royal blue
        new Color(0.95f, 0.30f, 0.70f),   // pink
    };

    // =========================================================================
    // ── Shape library ─────────────────────────────────────────────────────────
    // =========================================================================
    //  Visual diagrams use:   X = filled cell,  . = empty
    //  Columns go →  /  Rows go ↑  (row 0 = bottom)
    // =========================================================================

    private static readonly Vector2Int[][] AllShapes =
    {
        //──────────────────────────────────────────────────────────────────────
        // 2×2 SQUARE (4'lü küp)
        //──────────────────────────────────────────────────────────────────────
        /* 0 */  S(V(0,0), V(1,0), V(0,1), V(1,1)),                 // XX / XX

        //──────────────────────────────────────────────────────────────────────
        // 3×3 SQUARE (9'lu küp)
        //──────────────────────────────────────────────────────────────────────
        /* 1 */  S(V(0,0), V(1,0), V(2,0),
                   V(0,1), V(1,1), V(2,1),
                   V(0,2), V(1,2), V(2,2)),

        //──────────────────────────────────────────────────────────────────────
        // 3-CELL L-SHAPES (3'lü "L") – 4 rotations
        //──────────────────────────────────────────────────────────────────────
        /* 2 */  S(V(0,0), V(0,1), V(1,1)),   // ┘
        /* 3 */  S(V(0,0), V(1,0), V(0,1)),   // └
        /* 4 */  S(V(1,0), V(0,1), V(1,1)),   // ┌
        /* 5 */  S(V(0,0), V(1,0), V(1,1)),   // ┐

        //──────────────────────────────────────────────────────────────────────
        // 4-CELL L-SHAPES (4'lü "L") – 8 rotations (J and L variants)
        //──────────────────────────────────────────────────────────────────────
        /* 6 */  S(V(0,0), V(0,1), V(0,2), V(1,0)),   // │+─ (J)
        /* 7 */  S(V(0,0), V(1,0), V(1,1), V(1,2)),   // ─+│
        /* 8 */  S(V(0,2), V(1,0), V(1,1), V(1,2)),   // ─+│ top
        /* 9 */  S(V(0,0), V(0,1), V(0,2), V(1,2)),   // │+─ top
        /*10 */  S(V(0,0), V(1,0), V(2,0), V(2,1)),   // L
        /*11 */  S(V(0,0), V(0,1), V(1,0), V(2,0)),   // L rotated
        /*12 */  S(V(0,1), V(1,1), V(2,0), V(2,1)),   // L
        /*13 */  S(V(0,0), V(0,1), V(1,1), V(2,1)),   // L

        //──────────────────────────────────────────────────────────────────────
        // 5-CELL L-SHAPES (5'li "L") – 8 rotations (corner & extended)
        //──────────────────────────────────────────────────────────────────────
        /*14 */  S(V(0,0), V(1,0), V(2,0), V(0,1), V(0,2)),   // BL corner
        /*15 */  S(V(0,0), V(1,0), V(2,0), V(2,1), V(2,2)),   // BR corner
        /*16 */  S(V(0,0), V(0,1), V(0,2), V(1,2), V(2,2)),   // TL corner
        /*17 */  S(V(0,2), V(1,2), V(2,0), V(2,1), V(2,2)),   // TR corner
        /*18 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(0,1)),   // ───┘
        /*19 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(3,1)),   // └───
        /*20 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(1,0)),   // vertical + bottom foot right
        /*21 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(1,3)),   // vertical + top foot right

        //──────────────────────────────────────────────────────────────────────
        // 2-CELL I (2'li "I") – 2 rotations
        //──────────────────────────────────────────────────────────────────────
        /*22 */  S(V(0,0), V(0,1)),                               // vertical
        /*23 */  S(V(0,0), V(1,0)),                               // horizontal

        //──────────────────────────────────────────────────────────────────────
        // 3-CELL I (3'lü "I") – 2 rotations
        //──────────────────────────────────────────────────────────────────────
        /*24 */  S(V(0,0), V(0,1), V(0,2)),                       // vertical
        /*25 */  S(V(0,0), V(1,0), V(2,0)),                       // horizontal

        //──────────────────────────────────────────────────────────────────────
        // 4-CELL I (4'lü "I") – 2 rotations
        //──────────────────────────────────────────────────────────────────────
        /*26 */  S(V(0,0), V(0,1), V(0,2), V(0,3)),               // vertical
        /*27 */  S(V(0,0), V(1,0), V(2,0), V(3,0)),               // horizontal

        //──────────────────────────────────────────────────────────────────────
        // 5-CELL I (5'li "I") – 2 rotations
        //──────────────────────────────────────────────────────────────────────
        /*28 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(0,4)),       // vertical
        /*29 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(4,0)),       // horizontal

        //──────────────────────────────────────────────────────────────────────
        // 6-CELL I (6'lı "I") – 2 rotations (new)
        //──────────────────────────────────────────────────────────────────────
        /*30 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(0,4), V(0,5)), // vertical
        /*31 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(4,0), V(5,0)), // horizontal

        //──────────────────────────────────────────────────────────────────────
        // T-SHAPES (normal "T") – 4 rotations
        //──────────────────────────────────────────────────────────────────────
        /*32 */  S(V(0,0), V(1,0), V(2,0), V(1,1)),   // ⊥  T pointing up
        /*33 */  S(V(1,0), V(0,1), V(1,1), V(2,1)),   // ┬  T pointing down
        /*34 */  S(V(0,0), V(0,1), V(0,2), V(1,1)),   // ├  T pointing right
        /*35 */  S(V(0,1), V(1,0), V(1,1), V(1,2)),   // ┤  T pointing left

        //──────────────────────────────────────────────────────────────────────
        // Z-SHAPES (normal "Z") – 2 rotations (only Z, not S)
        //──────────────────────────────────────────────────────────────────────
        /*36 */  S(V(0,0), V(1,0), V(1,1), V(2,1)),   // Z horizontal
        /*37 */  S(V(0,1), V(1,1), V(1,2), V(2,2)),   // Z vertical (corrected from original Z vertical)
    };

    // =========================================================================
    // ── Weight pools (shape indices + probability) ────────────────────────────
    // =========================================================================
    //  Tiny  (2 cells)
    //  Small (3 cells)
    //  Medium(4 cells)
    //  Large (5+ cells: 5,6,9)

    private static readonly int[] TinyPool = { 22, 23 };                 // 2-cell I
    private static readonly int[] SmallPool = { 2, 3, 4, 5, 24, 25 };        // 3-cell L and I
    private static readonly int[] MediumPool = { 0,                       // 2×2 square
                                                  6,7,8,9,10,11,12,13,    // 4-cell L
                                                  26,27,                  // 4-cell I
                                                  32,33,34,35,            // T
                                                  36,37 };                // Z
    private static readonly int[] LargePool = { 1,                       // 3×3 square
                                                  14,15,16,17,            // 5-cell corner L
                                                  18,19,20,21,            // 5-cell extended L
                                                  28,29,                  // 5-cell I
                                                  30,31 };                // 6-cell I

    // =========================================================================
    // ── Public API ────────────────────────────────────────────────────────────
    // =========================================================================

    /// <summary>
    /// Returns a random BlockData, adapting the difficulty to the current grid state.
    ///
    /// When the grid is crowded (>55 % filled), the method:
    ///  1. Tries up to MAX_RETRIES times to find a shape that fits somewhere.
    ///  2. If all retries fail, calls GetGuaranteedFit() to find any fitting shape.
    ///  3. If still none fits, returns null (game over condition).
    /// </summary>
    public static BlockData GetRandom(float occupancyRatio = 0f)
    {
        // Pick a pool based on how full the grid is
        int[] pool = ChoosePool(occupancyRatio);
        Color color = RandomColor();

        // On a tight grid: retry up to 12 times to find a piece that fits
        bool gridTight = occupancyRatio > 0.55f;
        int maxTries = gridTight ? 12 : 1;

        for (int attempt = 0; attempt < maxTries; attempt++)
        {
            var shape = AllShapes[pool[Random.Range(0, pool.Length)]];
            var data = new BlockData(shape, color);

            if (!gridTight || GridManager.Instance == null ||
                GridManager.Instance.CanPlaceAnywhere(data))
                return data;
        }

        // Fallback: try to find any shape that fits (escalating from smallest)
        return GetGuaranteedFit();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Selects a pool weighted toward smaller pieces as the grid fills up.
    ///
    ///  occupancy < 35 %   →  30 % tiny / 35 % small / 25 % medium / 10 % large
    ///  occupancy 35–55 % →  20 % tiny / 40 % small / 30 % medium / 10 % large
    ///  occupancy 55–70 % →  35 % tiny / 40 % small / 20 % medium /  5 % large
    ///  occupancy > 70 %  →  50 % tiny / 40 % small / 10 % medium /  0 % large
    /// </summary>
    private static int[] ChoosePool(float occ)
    {
        float roll = Random.value;

        if (occ < 0.35f)
        {
            if (roll < 0.30f) return TinyPool;
            if (roll < 0.65f) return SmallPool;
            if (roll < 0.90f) return MediumPool;
            return LargePool;
        }
        else if (occ < 0.55f)
        {
            if (roll < 0.20f) return TinyPool;
            if (roll < 0.60f) return SmallPool;
            if (roll < 0.90f) return MediumPool;
            return LargePool;
        }
        else if (occ < 0.70f)
        {
            if (roll < 0.35f) return TinyPool;
            if (roll < 0.75f) return SmallPool;
            if (roll < 0.95f) return MediumPool;
            return LargePool;
        }
        else  // > 70 % — grid very full
        {
            if (roll < 0.50f) return TinyPool;
            if (roll < 0.90f) return SmallPool;
            return MediumPool;   // large pieces never appear on a packed grid
        }
    }

    /// <summary>
    /// Returns the smallest shape that can still be placed on the current grid.
    /// Tries tiny, then small, then medium, then large pools in order.
    /// If no shape fits, returns null.
    /// </summary>
    public static BlockData GetGuaranteedFit()
    {
        Color color = RandomColor();
        var gm = GridManager.Instance;
        if (gm == null) return null;

        // Escalate through pools in increasing size order
        int[][] escalate = { TinyPool, SmallPool, MediumPool, LargePool };
        foreach (var pool in escalate)
        {
            // Shuffle pool order so we don't always pick the same shape
            var indices = new List<int>(pool);
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = indices[i]; indices[i] = indices[j]; indices[j] = tmp;
            }
            foreach (int idx in indices)
            {
                var data = new BlockData(AllShapes[idx], color);
                if (gm.CanPlaceAnywhere(data)) return data;
            }
        }

        // No shape fits at all → game over
        return null;
    }

    private static Color RandomColor() => Palette[Random.Range(0, Palette.Length)];

    // ── Shape construction helpers ────────────────────────────────────────────

    private static Vector2Int V(int c, int r) => new Vector2Int(c, r);
    private static Vector2Int[] S(params Vector2Int[] cells) => cells;
}
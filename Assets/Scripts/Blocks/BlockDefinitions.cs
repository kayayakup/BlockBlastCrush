// ── BlockDefinitions.cs ───────────────────────────────────────────────────────
// Place in: Assets/Scripts/Blocks/BlockDefinitions.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Complete catalogue of all block shapes with every relevant rotation baked in.
/// Each shape is stored in ALL of its distinct orientations as separate entries
/// so no runtime rotation is needed (matching 1010!/Block Puzzle gameplay).
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

    public static readonly Vector2Int[][] AllShapes =
    {
        //──────────────────────────────────────────────────────────────────────
        // 2-CELL LINES (I)
        //──────────────────────────────────────────────────────────────────────
        /*  0 */  S(V(0,0), V(0,1)),                               // vertical domino
        /*  1 */  S(V(0,0), V(1,0)),                               // horizontal domino

        //──────────────────────────────────────────────────────────────────────
        // 3-CELL LINES (I)
        //──────────────────────────────────────────────────────────────────────
        /*  2 */  S(V(0,0), V(0,1), V(0,2)),                       // │││
        /*  3 */  S(V(0,0), V(1,0), V(2,0)),                       // ───

        //──────────────────────────────────────────────────────────────────────
        // 4-CELL LINES (I)
        //──────────────────────────────────────────────────────────────────────
        /*  4 */  S(V(0,0), V(0,1), V(0,2), V(0,3)),               // ││││
        /*  5 */  S(V(0,0), V(1,0), V(2,0), V(3,0)),               // ────

        //──────────────────────────────────────────────────────────────────────
        // 5-CELL LINES (I)
        //──────────────────────────────────────────────────────────────────────
        /*  6 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(0,4)),       // │││││
        /*  7 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(4,0)),       // ─────

        //──────────────────────────────────────────────────────────────────────
        // 2×2 SQUARE (4'lü küp)
        //──────────────────────────────────────────────────────────────────────
        /*  8 */  S(V(0,0), V(1,0), V(0,1), V(1,1)),               // XX / XX

        //──────────────────────────────────────────────────────────────────────
        // 3×3 SQUARE (9'lu küp)
        //──────────────────────────────────────────────────────────────────────
        /*  9 */  S(V(0,0), V(1,0), V(2,0),
                    V(0,1), V(1,1), V(2,1),
                    V(0,2), V(1,2), V(2,2)),                       // full 3x3 block

        //──────────────────────────────────────────────────────────────────────
        // SMALL L-SHAPES (3'lü "L", 4 rotations)
        //  rot0: XX    rot1: X.    rot2: .X    rot3: XX
        //        X.          XX          .X          .X
        //──────────────────────────────────────────────────────────────────────
        /* 10 */  S(V(0,0), V(0,1), V(1,1)),   // ┘
        /* 11 */  S(V(0,0), V(1,0), V(0,1)),   // └
        /* 12 */  S(V(1,0), V(0,1), V(1,1)),   // ┌
        /* 13 */  S(V(0,0), V(1,0), V(1,1)),   // ┐

        //──────────────────────────────────────────────────────────────────────
        // LARGE L-SHAPES (4'lü "L", 8 rotations)
        //  arm of 3 + single corner cell
        //──────────────────────────────────────────────────────────────────────
        // Foot goes right
        /* 14 */  S(V(0,0), V(0,1), V(0,2), V(1,0)),   // │+─
        /* 15 */  S(V(0,0), V(1,0), V(1,1), V(1,2)),   // ─+│
        /* 16 */  S(V(0,2), V(1,0), V(1,1), V(1,2)),   // ─+│ (top)
        /* 17 */  S(V(0,0), V(0,1), V(0,2), V(1,2)),   // │+─ (top)
        // Foot goes left
        /* 18 */  S(V(0,0), V(1,0), V(2,0), V(2,1)),
        /* 19 */  S(V(0,0), V(0,1), V(1,0), V(2,0)),
        /* 20 */  S(V(0,1), V(1,1), V(2,0), V(2,1)),
        /* 21 */  S(V(0,0), V(0,1), V(1,1), V(2,1)),

        //──────────────────────────────────────────────────────────────────────
        // T-SHAPES (normal "T", 4 rotations)
        //──────────────────────────────────────────────────────────────────────
        /* 22 */  S(V(0,0), V(1,0), V(2,0), V(1,1)),   // ⊥  (pointing up)
        /* 23 */  S(V(1,0), V(0,1), V(1,1), V(2,1)),   // ┬  (pointing down)
        /* 24 */  S(V(0,0), V(0,1), V(0,2), V(1,1)),   // ├  (pointing right)
        /* 25 */  S(V(0,1), V(1,0), V(1,1), V(1,2)),   // ┤  (pointing left)

        //──────────────────────────────────────────────────────────────────────
        // S / Z SHAPES (normal "Z", 4 orientations)
        //──────────────────────────────────────────────────────────────────────
        /* 26 */  S(V(0,0), V(1,0), V(1,1), V(2,1)),   // S horizontal
        /* 27 */  S(V(0,1), V(1,0), V(1,1), V(2,0)),   // Z horizontal
        /* 28 */  S(V(0,1), V(0,2), V(1,0), V(1,1)),   // S vertical
        /* 29 */  S(V(0,0), V(0,1), V(1,1), V(1,2)),   // Z vertical

        //──────────────────────────────────────────────────────────────────────
        // 2×3 / 3×2 RECTANGLES (6'lı dikdörtgen)
        //──────────────────────────────────────────────────────────────────────
        /* 30 */  S(V(0,0), V(1,0),
                    V(0,1), V(1,1),
                    V(0,2), V(1,2)),                   // 2 wide × 3 tall
        /* 31 */  S(V(0,0), V(1,0), V(2,0),
                    V(0,1), V(1,1), V(2,1)),           // 3 wide × 2 tall

        //──────────────────────────────────────────────────────────────────────
        // 5-CELL EXTENDED L-SHAPES (5'li "L", 4 rotations)
        //  arm of 4 + perpendicular foot
        //──────────────────────────────────────────────────────────────────────
        /* 32 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(0,1)),   // ───┘
        /* 33 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(3,1)),   // └───
        /* 34 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(1,0)),   // vertical + bottom foot right
        /* 35 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(1,3)),   // vertical + top foot right
    };

    // =========================================================================
    // ── Weight pools (shape indices + probability) ────────────────────────────
    // =========================================================================
    //  Tiny  (≤2 cells) – appear often; easy to squeeze into gaps
    //  Small (3–4 cells) – standard frequency
    //  Medium(4–5 cells) – somewhat common
    //  Large (5–9 cells) – rarer; punishing on tight grids

    private static readonly int[] TinyPool = { 0, 1 };                     // 2‑cell lines
    private static readonly int[] SmallPool = { 2, 3, 8, 10, 11, 12, 13, 26, 27, 28, 29 }; // 3‑cell & simple 4‑cell
    private static readonly int[] MediumPool = { 4, 5, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 }; // 4‑cell complex
    private static readonly int[] LargePool = { 6, 7, 9, 30, 31, 32, 33, 34, 35 }; // 5‑cell, 6‑cell, 9‑cell

    // =========================================================================
    // ── Public API ────────────────────────────────────────────────────────────
    // =========================================================================

    /// <summary>
    /// Returns a random BlockData, adapting the difficulty to the current grid state.
    ///
    /// When the grid is crowded (>55 % filled), the method:
    ///  1. Tries up to MAX_RETRIES times to find a shape that fits somewhere.
    ///  2. If all retries fail, falls back to GetGuaranteedFit().
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

        // Fallback — try every shape from smallest to largest
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
    /// Tries tiny, small, medium, then large pools in order, shuffling each.
    /// If nothing fits, returns a 2‑cell piece (the smallest available).
    /// </summary>
    public static BlockData GetGuaranteedFit()
    {
        Color color = RandomColor();
        var gm = GridManager.Instance;

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
                if (gm == null || gm.CanPlaceAnywhere(data))
                    return data;
            }
        }

        // Ultimate fallback: return a 2‑cell piece (smallest available)
        return new BlockData(AllShapes[TinyPool[0]], color);
    }

    private static Color RandomColor() => Palette[Random.Range(0, Palette.Length)];

    // ── Shape construction helpers ────────────────────────────────────────────

    private static Vector2Int V(int c, int r) => new Vector2Int(c, r);
    private static Vector2Int[] S(params Vector2Int[] cells) => cells;
}
// ── BlockDefinitions.cs ───────────────────────────────────────────────────────
// Place in: Assets/Scripts/Blocks/BlockDefinitions.cs
using System.Collections.Generic;
using UnityEngine;

public static class BlockDefinitions
{
    private static readonly Color[] Palette =
    {
        new Color(0.96f, 0.45f, 0.85f),   // magenta-pink
        new Color(0.40f, 0.85f, 0.55f),   // mint green
        new Color(1.00f, 0.60f, 0.28f),   // tangerine orange
        new Color(0.30f, 0.82f, 0.95f),   // sky cyan
        new Color(1.00f, 0.88f, 0.30f),   // sunny yellow
        new Color(1.00f, 0.40f, 0.45f),   // coral red
        new Color(0.38f, 0.58f, 0.98f),   // soft blue
        new Color(0.95f, 0.50f, 0.78f),   // bubblegum pink
    };

    // =========================================================================
    // ── Shape library ─────────────────────────────────────────────────────────
    // =========================================================================
    public static readonly Vector2Int[][] AllShapes =
    {
        // 2-cell I
        /*  0 */  S(V(0,0), V(0,1)),                               // vertical domino
        /*  1 */  S(V(0,0), V(1,0)),                               // horizontal domino
        // 3-cell I
        /*  2 */  S(V(0,0), V(0,1), V(0,2)),                       // │││
        /*  3 */  S(V(0,0), V(1,0), V(2,0)),                       // ───
        // 4-cell I
        /*  4 */  S(V(0,0), V(0,1), V(0,2), V(0,3)),               // ││││
        /*  5 */  S(V(0,0), V(1,0), V(2,0), V(3,0)),               // ────
        // 5-cell I
        /*  6 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(0,4)),       // │││││
        /*  7 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(4,0)),       // ─────
        // 2×2 square (4'lü küp)
        /*  8 */  S(V(0,0), V(1,0), V(0,1), V(1,1)),               // XX / XX
        // 3×3 square (9'lu küp)
        /*  9 */  S(V(0,0), V(1,0), V(2,0),
                    V(0,1), V(1,1), V(2,1),
                    V(0,2), V(1,2), V(2,2)),
        // 3-cell L (3'lü "L") – 4 rotations
        /* 10 */  S(V(0,0), V(0,1), V(1,1)),   // ┘
        /* 11 */  S(V(0,0), V(1,0), V(0,1)),   // └
        /* 12 */  S(V(1,0), V(0,1), V(1,1)),   // ┌
        /* 13 */  S(V(0,0), V(1,0), V(1,1)),   // ┐
        // 4-cell L (4'lü "L") – 8 rotations
        /* 14 */  S(V(0,0), V(0,1), V(0,2), V(1,0)),   // │+─
        /* 15 */  S(V(0,0), V(1,0), V(1,1), V(1,2)),   // ─+│
        /* 16 */  S(V(0,2), V(1,0), V(1,1), V(1,2)),   // ─+│ (top)
        /* 17 */  S(V(0,0), V(0,1), V(0,2), V(1,2)),   // │+─ (top)
        /* 18 */  S(V(0,0), V(1,0), V(2,0), V(2,1)),
        /* 19 */  S(V(0,0), V(0,1), V(1,0), V(2,0)),
        /* 20 */  S(V(0,1), V(1,1), V(2,0), V(2,1)),
        /* 21 */  S(V(0,0), V(0,1), V(1,1), V(2,1)),
        // T (normal "T") – 4 rotations
        /* 22 */  S(V(0,0), V(1,0), V(2,0), V(1,1)),   // ⊥
        /* 23 */  S(V(1,0), V(0,1), V(1,1), V(2,1)),   // ┬
        /* 24 */  S(V(0,0), V(0,1), V(0,2), V(1,1)),   // ├
        /* 25 */  S(V(0,1), V(1,0), V(1,1), V(1,2)),   // ┤
        // Z (normal "Z") – 4 orientations
        /* 26 */  S(V(0,0), V(1,0), V(1,1), V(2,1)),   // S horizontal
        /* 27 */  S(V(0,1), V(1,0), V(1,1), V(2,0)),   // Z horizontal
        /* 28 */  S(V(0,1), V(0,2), V(1,0), V(1,1)),   // S vertical
        /* 29 */  S(V(0,0), V(0,1), V(1,1), V(1,2)),   // Z vertical
        // 2×3 / 3×2 rectangles (6'lı dikdörtgen)
        /* 30 */  S(V(0,0), V(1,0), V(0,1), V(1,1), V(0,2), V(1,2)), // 2x3
        /* 31 */  S(V(0,0), V(1,0), V(2,0), V(0,1), V(1,1), V(2,1)), // 3x2
        // 5-cell extended L (5'li "L") – 4 rotations
        /* 32 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(0,1)),   // ───┘
        /* 33 */  S(V(0,0), V(1,0), V(2,0), V(3,0), V(3,1)),   // └───
        /* 34 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(1,0)),   // vertical + bottom foot right
        /* 35 */  S(V(0,0), V(0,1), V(0,2), V(0,3), V(1,3)),   // vertical + top foot right
    };

    // =========================================================================
    // ── Weight pools (shape indices) – Frekans ayarlamaları burada yapıldı ───
    // =========================================================================
    // Sık istenen şekiller (8,9,30,31) tüm havuzlarda çoklu kopyalar halinde.
    // Seyrek istenen şekiller (10-13,14-21,22-25,26-29) yalnızca MediumPool ve LargePool'da,
    // hem de tek birer kopya ile yer alır.
    private static readonly int[] TinyPool   = { 0,1, 8,8,8 };                     // 2‑cell I + 2x2 (3 kopya)
    private static readonly int[] SmallPool  = { 2,3, 8,8,8, 9,9, 30,31,30,31 }; // 3‑cell I + 2x2 (3), 3x3 (2), 6‑rect (4)
    private static readonly int[] MediumPool = {
        4,5,                                         // 4‑cell I
        8,8, 9,9, 30,31,30,31,                      // sık şekiller (2x2,3x3,6‑rect)
        32,33,34,35,                                 // 5‑cell extended L
        10,11,12,13,                                 // 3L (seyrek)
        26,27,28,29,                                 // Z (seyrek)
        14,15,16,17,18,19,20,21,                     // 4L (seyrek)
        22,23,24,25                                   // T (seyrek)
    };
    private static readonly int[] LargePool  = {
        6,7,                                         // 5‑cell I
        8,8, 9,9, 30,31,30,31,                      // sık şekiller (tekrar)
        10,11,12,13,                                 // 3L
        26,27,28,29,                                 // Z
        14,15,16,17,18,19,20,21,                     // 4L
        22,23,24,25,                                 // T
        32,33,34,35                                   // 5‑cell extended L (isteğe bağlı)
    };

    // =========================================================================
    // ── Public API (değişmedi) ────────────────────────────────────────────────
    // =========================================================================
    public static BlockData GetRandom(float occupancyRatio = 0f)
    {
        int[] pool = ChoosePool(occupancyRatio);
        Color color = RandomColor();
        bool gridTight = occupancyRatio > 0.55f;
        int  maxTries  = gridTight ? 12 : 1;

        for (int attempt = 0; attempt < maxTries; attempt++)
        {
            var shape = AllShapes[pool[Random.Range(0, pool.Length)]];
            var data  = new BlockData(shape, color);
            if (!gridTight || GridManager.Instance == null ||
                GridManager.Instance.CanPlaceAnywhere(data))
                return data;
        }
        return GetGuaranteedFit();
    }

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
        else
        {
            if (roll < 0.50f) return TinyPool;
            if (roll < 0.90f) return SmallPool;
            return MediumPool;
        }
    }

    public static BlockData GetGuaranteedFit()
    {
        Color color = RandomColor();
        var gm = GridManager.Instance;
        int[][] escalate = { TinyPool, SmallPool, MediumPool, LargePool };
        foreach (var pool in escalate)
        {
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
        return new BlockData(AllShapes[TinyPool[0]], color);
    }

    private static Color RandomColor() => Palette[Random.Range(0, Palette.Length)];
    private static Vector2Int   V(int c, int r) => new Vector2Int(c, r);
    private static Vector2Int[] S(params Vector2Int[] cells) => cells;
}
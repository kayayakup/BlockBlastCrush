// ── BlockData.cs ──────────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Blocks/BlockData.cs
using UnityEngine;

/// <summary>
/// Immutable descriptor for a block piece: its cell offsets and colour.
/// Pre-computes bounding-box metadata once on construction.
/// </summary>
public sealed class BlockData
{
    /// <summary>(col, row) offsets relative to the bounding-box minimum corner.</summary>
    public readonly Vector2Int[] Cells;
    public readonly Color        Color;

    // Bounding-box extremes (in cell units)
    public readonly int   MinCol, MaxCol, MinRow, MaxRow;

    /// <summary>Floating-point centre of the bounding box (for positioning).</summary>
    public readonly float CenterCol, CenterRow;

    public BlockData(Vector2Int[] cells, Color color)
    {
        Cells = cells;
        Color = color;

        MinCol = MinRow = int.MaxValue;
        MaxCol = MaxRow = int.MinValue;

        foreach (var c in cells)
        {
            if (c.x < MinCol) MinCol = c.x;
            if (c.x > MaxCol) MaxCol = c.x;
            if (c.y < MinRow) MinRow = c.y;
            if (c.y > MaxRow) MaxRow = c.y;
        }

        CenterCol = (MinCol + MaxCol) * 0.5f;
        CenterRow = (MinRow + MaxRow) * 0.5f;
    }

    /// <summary>Width of bounding box in cells.</summary>
    public int BBoxWidth  => MaxCol - MinCol + 1;

    /// <summary>Height of bounding box in cells.</summary>
    public int BBoxHeight => MaxRow - MinRow + 1;
}

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

    private Block[] _trayBlocks;
    private BlockData[] _trayData;
    private Vector3[] _slotPositions;
    private bool[] _slotFilled;
    private int _filledCount;
    private Tween _batchDelayTween;

    // Rare shapes: normal T, normal Z, 3'lü L, 4'lü L
    // In BlockDefinitions these correspond to indices 10–29.
    private static HashSet<Vector2Int[]> _rareShapes;
    private const float RARE_ACCEPT_PROBABILITY = 0.2f; // 20% chance to keep a rare shape

    // Lazy initialisation of the rare‑shape set
    private static void EnsureRareShapesInitialized()
    {
        if (_rareShapes != null) return;
        _rareShapes = new HashSet<Vector2Int[]>();
        // Indices 10 through 29 are the rare ones (see BlockDefinitions)
        for (int i = 10; i <= 29; i++)
        {
            _rareShapes.Add(BlockDefinitions.AllShapes[i]);
        }
    }

    void Awake() => Instance = this;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        int n = Constants.TRAY_COUNT;
        _trayBlocks = new Block[n];
        _trayData = new BlockData[n];
        _slotPositions = new Vector3[n];
        _slotFilled = new bool[n];

        // Compute 2x3 grid positions (3 columns, 2 rows)
        float colSpacing = Constants.TRAY_SLOT_SPACING;
        float rowSpacing = LayoutConfig.TrayRowSpacing;
        float startX = -1.0f * colSpacing; // (3-1) * 0.5 * spacing = 1.0 * spacing
        
        for (int i = 0; i < n; i++)
        {
            int row = i / 3; // 0 for first 3, 1 for next 3
            int col = i % 3; // 0, 1, 2
            
            float x = startX + col * colSpacing;
            // First row (i=0,1,2) at Y, second row (i=3,4,5) at Y - rowSpacing
            float y = Constants.TRAY_Y - row * rowSpacing;
            
            _slotPositions[i] = new Vector3(x, y, 0f);
        }

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

    public Block GetBlock(int slot) => _trayBlocks[slot];
    public int FilledCount => _filledCount;

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

    /// <summary>
    /// Returns a random BlockData, but with reduced probability for rare shapes.
    /// Uses rejection sampling: when a rare shape is drawn, it is kept only with probability RARE_ACCEPT_PROBABILITY.
    /// </summary>
    private BlockData GetRandomWithRarity(float occ, int maxAttempts = 15)
    {
        EnsureRareShapesInitialized();

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var data = BlockDefinitions.GetRandom(occ);
            if (_rareShapes.Contains(data.Cells))
            {
                // Rare shape: accept only with low probability
                if (Random.value < RARE_ACCEPT_PROBABILITY)
                    return data;
                // Otherwise retry
            }
            else
            {
                // Common shape: always accept
                return data;
            }
        }
        // Fallback – just return whatever the standard method gives
        return BlockDefinitions.GetRandom(occ);
    }

    private void SpawnBatch()
    {
        _filledCount = 0;

        // Sample occupancy once for the whole batch so all three pieces
        // are chosen with the same difficulty context.
        float occ = GridManager.Instance != null
                    ? GridManager.Instance.GetOccupancyRatio()
                    : 0f;

        // Generate candidates with rarity control
        var candidates = new BlockData[Constants.TRAY_COUNT];
        for (int i = 0; i < Constants.TRAY_COUNT; i++)
            candidates[i] = GetRandomWithRarity(occ);

        // Safety pass: if none of the three fit anywhere, replace one with
        // a guaranteed-fit piece (avoids deadlocks on very crowded grids).
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
            var data = candidates[i];
            _trayData[i] = data;
            _slotFilled[i] = true;
            _filledCount++;

            int capturedI = i;
            var capturedD = data;
            float delay = i * 0.06f;
            DOVirtual.DelayedCall(delay, () => {
                _trayBlocks[capturedI].Setup(capturedD, capturedI, _slotPositions[capturedI]);
                if (capturedI == 0) AudioManager.Instance?.PlaySpawn();
            });
        }
    }
}
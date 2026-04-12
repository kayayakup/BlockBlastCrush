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

    private Block[]     _trayBlocks;
    private BlockData[] _trayData;
    private Vector3[]   _slotPositions;
    private bool[]      _slotFilled;
    private int         _filledCount;
    private Tween       _batchDelayTween;

    // Rarity configuration: shapes that should appear less often
    private static readonly HashSet<Vector2Int[]> _rareShapes;
    private const float RARE_ACCEPT_PROBABILITY = 0.2f; // 20% chance to keep a rare shape

    static BlockSpawner()
    {
        // Mark shapes with indices 10–29 as rare:
        // 10–13 : 3'lü "L"
        // 14–21 : 4'lü "L"
        // 22–25 : normal "T"
        // 26–29 : normal "Z"
        _rareShapes = new HashSet<Vector2Int[]>();
        for (int i = 10; i <= 29; i++)
        {
            _rareShapes.Add(BlockDefinitions.AllShapes[i]);
        }
    }

    void Awake()
    {
        Instance = this;
        if (StyleManager.Instance != null)
            StyleManager.Instance.OnStyleChanged += RefreshTray;
    }

    void OnDestroy()
    {
        if (StyleManager.Instance != null)
            StyleManager.Instance.OnStyleChanged -= RefreshTray;
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        int n = Constants.TRAY_COUNT;
        _trayBlocks    = new Block[n];
        _trayData      = new BlockData[n];
        _slotPositions = new Vector3[n];
        _slotFilled    = new bool[n];

        // Compute 2x3 grid slot positions
        float spacingX = Constants.TRAY_SLOT_SPACING;
        float spacingY = Constants.TRAY_ROW_SPACING;
        for (int i = 0; i < n; i++)
        {
            int row = i / 3; // 0 for first row, 1 for second row
            int col = i % 3; // 0, 1, 2 for columns
            
            float x = (col - 1) * spacingX;
            float y = Constants.TRAY_Y - row * spacingY;
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

    public Block     GetBlock(int slot)     => _trayBlocks[slot];
    public int       FilledCount            => _filledCount;

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

        // Sample occupancy once for the whole batch so all pieces
        // are chosen with the same difficulty context.
        float occ = GridManager.Instance != null
                    ? GridManager.Instance.GetOccupancyRatio()
                    : 0f;

        var candidates = new BlockData[Constants.TRAY_COUNT];

        // ── Gap-aware slot ───────────────────────────────────────────────
        // Pick one random slot that will receive a shape matching a grid gap.
        int gapSlot = -1;
        if (occ > 0.10f && GridManager.Instance != null)
        {
            var fittingIndices = GridManager.Instance.FindFittingShapeIndices();
            if (fittingIndices.Count > 0)
            {
                gapSlot = Random.Range(0, Constants.TRAY_COUNT);
                int chosenIdx = fittingIndices[Random.Range(0, fittingIndices.Count)];
                candidates[gapSlot] = BlockDefinitions.GetFromShapeIndex(chosenIdx);
            }
        }

        // ── Fill remaining slots with normal random shapes ───────────────
        for (int i = 0; i < Constants.TRAY_COUNT; i++)
        {
            if (i == gapSlot) continue; // already assigned
            candidates[i] = GetRandomWithRarity(occ);
        }

        // Safety pass: if none of the pieces fit anywhere, replace one with
        // a guaranteed-fit piece (avoids deadlocks on very crowded grids).
        if (occ > 0.55f && GridManager.Instance != null)
        {
            bool anyFits = false;
            foreach (var d in candidates)
                if (GridManager.Instance.CanPlaceAnywhere(d)) { anyFits = true; break; }

            if (!anyFits)
            {
                // Force a slot to a guaranteed-fit tiny piece
                int safeSlot = (gapSlot >= 0 && gapSlot != 1) ? 1 : 0;
                candidates[safeSlot] = BlockDefinitions.GetGuaranteedFit();
            }
        }

        for (int i = 0; i < Constants.TRAY_COUNT; i++)
        {
            var data       = candidates[i];
            _trayData[i]   = data;
            _slotFilled[i] = true;
            _filledCount++;

            int   capturedI = i;
            var   capturedD = data;
            float delay     = i * 0.06f;
            DOVirtual.DelayedCall(delay, () => {
                _trayBlocks[capturedI].Setup(capturedD, capturedI, _slotPositions[capturedI]);
                if (capturedI == 0) AudioManager.Instance?.PlaySpawn();
            });
        }

        GoogleAdMobController.Instance.CheckAndShowBanner();
    }

    private void RefreshTray()
    {
        if (_trayBlocks == null) return;
        foreach (var b in _trayBlocks)
        {
            if (b != null)
                b.RefreshStyle();
        }
    }
}
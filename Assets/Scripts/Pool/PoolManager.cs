// ── PoolManager.cs ────────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Pool/PoolManager.cs
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Simple object pool for block-cell GameObjects.
/// Cells are reused across the lifetime of the session to minimise GC pressure.
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private const int PREWARM_COUNT = 48;  // enough for ~4 large pieces simultaneously

    void Awake() => Instance = this;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        for (int i = 0; i < PREWARM_COUNT; i++)
            _pool.Enqueue(CreateCell());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Retrieves an active cell from the pool (creates one if pool is empty).</summary>
    public GameObject GetCell()
    {
        var cell = (_pool.Count > 0) ? _pool.Dequeue() : CreateCell();
        cell.SetActive(true);
        return cell;
    }

    /// <summary>
    /// Returns a cell to the pool.
    /// Kills any active tweens, resets sprite/color/scale, and deactivates it.
    /// </summary>
    public void ReturnCell(GameObject cell)
    {
        if (cell == null) return;

        DOTween.Kill(cell.transform);

        var sr = cell.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            DOTween.Kill(sr);
            sr.sprite       = TextureUtils.WhiteCellSprite;
            sr.color        = Color.white;
            sr.sortingOrder = 0;
        }

        cell.SetActive(false);
        cell.transform.SetParent(transform);
        cell.transform.localScale = Vector3.one;
        _pool.Enqueue(cell);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private GameObject CreateCell()
    {
        var go = new GameObject("PoolCell");
        go.transform.SetParent(transform);

        var sr    = go.AddComponent<SpriteRenderer>();
        sr.sprite = TextureUtils.WhiteCellSprite;
        sr.color  = Color.white;

        go.SetActive(false);
        return go;
    }
}

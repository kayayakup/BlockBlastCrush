// ── EffectManager.cs ──────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Core/EffectManager.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns particle effects (fireworks) when lines are cleared.
/// Loads firework prefabs from Resources folder.
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    private GameObject[] fireworkPrefabs;

    void Awake()
    {
        Instance = this;
        LoadFireworkPrefabs();
    }

    private void LoadFireworkPrefabs()
    {
        // Prefab'ların "Assets/Resources/Particles/" klasöründe olduğunu varsayıyoruz.
        // İsimler: SF_1, SF_Basic, SF_Rainbow
        List<GameObject> prefabs = new List<GameObject>();

        // Tek tek yükle
        GameObject sf1 = Resources.Load<GameObject>("Particles/SF_1");
        GameObject sfBasic = Resources.Load<GameObject>("Particles/SF_Basic");
        GameObject sfRainbow = Resources.Load<GameObject>("Particles/SF_Rainbow");

        if (sf1 != null) prefabs.Add(sf1);
        if (sfBasic != null) prefabs.Add(sfBasic);
        if (sfRainbow != null) prefabs.Add(sfRainbow);

        // Alternatif: Tümünü tek seferde yüklemek için:
        // fireworkPrefabs = Resources.LoadAll<GameObject>("Particles");

        fireworkPrefabs = prefabs.ToArray();

        if (fireworkPrefabs.Length == 0)
        {
            Debug.LogError("EffectManager: No firework prefabs found in Resources/Particles folder!");
        }
    }

    public void Initialize()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.OnLinesCleared += HandleLinesCleared;
        }
    }

    private void OnDestroy()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.OnLinesCleared -= HandleLinesCleared;
        }
    }

    private void HandleLinesCleared(List<int> fullCols, List<int> fullRows, int cellCount)
    {
        if (fireworkPrefabs == null || fireworkPrefabs.Length == 0) return;

        // Spawn effects for cleared columns
        foreach (int col in fullCols)
        {
            Vector3 spawnPos = GridManager.Instance.CellWorldPos(col, Constants.GRID_ROWS / 2);
            SpawnRandomFirework(spawnPos);
        }

        // Spawn effects for cleared rows
        foreach (int row in fullRows)
        {
            Vector3 spawnPos = GridManager.Instance.CellWorldPos(Constants.GRID_COLS / 2, row);
            SpawnRandomFirework(spawnPos);
        }
    }

    private void SpawnRandomFirework(Vector3 position)
    {
        int index = Random.Range(0, fireworkPrefabs.Length);
        GameObject prefab = fireworkPrefabs[index];

        if (prefab != null)
        {
            GameObject effect = Instantiate(prefab, position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
}
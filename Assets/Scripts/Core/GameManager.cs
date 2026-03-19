// ── GameManager.cs ────────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Core/GameManager.cs
using UnityEngine;

/// <summary>
/// Central game-state machine.
/// States:  Playing  →  GameOver  →  (Restart)  →  Playing
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsGameOver   { get; private set; }
    public int  TotalPlaced  { get; private set; }   // total blocks placed this session

    void Awake() => Instance = this;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        IsGameOver  = false;
        TotalPlaced = 0;

        GoogleAdMobController.Instance.RestartScene();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by BlockSpawner.NotifyBlockPlaced after each placement.</summary>
    public void CheckGameOver()
    {
        if (IsGameOver) return;

        var available = BlockSpawner.Instance.GetAvailableData();
        foreach (var data in available)
            if (GridManager.Instance.CanPlaceAnywhere(data))
                return;   // at least one valid move → no game over

        TriggerGameOver();
    }

    /// <summary>Resets the entire game to its initial state.</summary>
    public void RestartGame()
    {
        IsGameOver  = false;
        TotalPlaced = 0;

        ScoreManager.Instance.ResetScore();
        GridManager.Instance.ResetGrid();
        BlockSpawner.Instance.ResetTray();
        UIManager.Instance.HideGameOver();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void TriggerGameOver()
    {
        IsGameOver = true;
        AudioManager.Instance?.PlayGameOver();
        UIManager.Instance.ShowGameOver(ScoreManager.Instance.CurrentScore);

        GoogleAdMobController.Instance.ShowInterstitialAd();
    }
}

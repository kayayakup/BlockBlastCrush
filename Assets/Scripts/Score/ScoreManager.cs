// ── ScoreManager.cs ───────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Score/ScoreManager.cs
using UnityEngine;

/// <summary>
/// Manages current and best score.
/// Best score is persisted via PlayerPrefs.
/// Fires OnScoreChanged(currentScore, bestScore, delta) whenever the score updates.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; }
    public int BestScore    { get; private set; }

    private const string BEST_SCORE_KEY = "BlockPuzzle_Best";

    /// <summary>Invoked after every score change. Args: (current, best, delta).</summary>
    public event System.Action<int, int, int> OnScoreChanged;

    void Awake() => Instance = this;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        BestScore    = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
        CurrentScore = 0;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void AddScore(int delta)
    {
        if (delta <= 0) return;

        CurrentScore += delta;

        if (CurrentScore > BestScore)
        {
            BestScore = CurrentScore;
            PlayerPrefs.SetInt(BEST_SCORE_KEY, BestScore);
            PlayerPrefs.Save();
        }

        if (delta >= Constants.POINTS_PER_LINE) AudioManager.Instance?.PlayScore();
        OnScoreChanged?.Invoke(CurrentScore, BestScore, delta);
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(CurrentScore, BestScore, 0);
    }
}

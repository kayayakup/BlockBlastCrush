// ── GameManager.cs ────────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Core/GameManager.cs
using System;
using GoogleMobileAds.Api;
using UnityEngine;

/// <summary>
/// Central game-state machine.
/// States:  Playing  →  GameOver  →  (Restart)  →  Playing
/// </summary>
public class GameManager : MonoBehaviour
{
    private const float TimedRewardedOfferInterval = 60f;
    private const float TimedInterstitialInterval = 180f;
    private const int TimedRewardedBonusPoints = 25;

    public static GameManager Instance { get; private set; }

    public bool IsGameOver { get; private set; }
    public int TotalPlaced { get; private set; }   // total blocks placed this session
    private float _timeSinceRewardedOffer;
    private float _timeSinceInterstitial;
    private bool _interstitialDue;
    private Action<Reward> _pendingRewardedAction;
    private bool _pendingRewardedIsTimedOffer;

    void Awake() => Instance = this;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        IsGameOver = false;
        TotalPlaced = 0;
        _timeSinceRewardedOffer = 0f;
        _timeSinceInterstitial = 0f;
        _interstitialDue = false;
        _pendingRewardedAction = null;
        _pendingRewardedIsTimedOffer = false;
    }

    private void Update()
    {
        GoogleAdMobController adMob = GoogleAdMobController.Instance;
        if (_pendingRewardedAction != null && adMob != null)
            ProcessPendingRewardedAction(adMob);

        if (IsGameOver)
            return;

        float deltaTime = Time.deltaTime;
        _timeSinceRewardedOffer += deltaTime;
        _timeSinceInterstitial += deltaTime;

        if (_timeSinceInterstitial >= TimedInterstitialInterval)
        {
            _timeSinceInterstitial = 0f;
            _interstitialDue = true;
        }

        if (adMob == null)
            return;

        if (_interstitialDue && adMob.IsInterstitialReady())
        {
            UIManager.Instance.HideTimedRewardedOffer();
            if (adMob.ShowInterstitial())
            {
                _interstitialDue = false;
                _timeSinceRewardedOffer = 0f;
                return;
            }
        }

        if (!_interstitialDue &&
            _timeSinceRewardedOffer >= TimedRewardedOfferInterval)
        {
            _timeSinceRewardedOffer = 0f;
            if (!adMob.IsRewardedReady())
                adMob.LoadRewarded();

            UIManager.Instance.ShowTimedRewardedOffer(
                adMob.IsRewardedReady(),
                adMob.IsRewardedRequestPending());
        }
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
        IsGameOver = false;
        TotalPlaced = 0;
        _timeSinceRewardedOffer = 0f;
        _timeSinceInterstitial = 0f;
        _interstitialDue = false;
        _pendingRewardedAction = null;
        _pendingRewardedIsTimedOffer = false;

        ScoreManager.Instance.ResetScore();
        GridManager.Instance.ResetGrid();
        BlockSpawner.Instance.ResetTray();
        UIManager.Instance.HideGameOver();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void TriggerGameOver()
    {
        IsGameOver = true;
        if (_pendingRewardedIsTimedOffer)
        {
            _pendingRewardedAction = null;
            _pendingRewardedIsTimedOffer = false;
        }

        AudioManager.Instance?.PlayGameOver();
        UIManager.Instance.HideTimedRewardedOffer();
        UIManager.Instance.ShowGameOver(
            ScoreManager.Instance.CurrentScore,
            GoogleAdMobController.Instance != null);
    }

    public void WatchRewardedForBonus()
    {
        if (IsGameOver)
            return;

        GoogleAdMobController adMob = GoogleAdMobController.Instance;
        if (adMob == null)
        {
            Debug.LogWarning("[GameManager] AdMob controller is unavailable.");
            UIManager.Instance.HideTimedRewardedOffer();
            return;
        }

        RequestRewardedAd(
            _ => ScoreManager.Instance.AddScore(TimedRewardedBonusPoints),
            isTimedOffer: true);
    }

    public void WatchRewardedToContinue()
    {
        if (!IsGameOver)
            return;

        GoogleAdMobController adMob = GoogleAdMobController.Instance;
        if (adMob == null)
        {
            Debug.LogWarning("[GameManager] AdMob controller is unavailable.");
            return;
        }

        RequestRewardedAd(_ => ContinueAfterReward(), isTimedOffer: false);
    }

    private void RequestRewardedAd(Action<Reward> onReward, bool isTimedOffer)
    {
        GoogleAdMobController adMob = GoogleAdMobController.Instance;
        if (adMob == null)
        {
            Debug.LogWarning("[GameManager] AdMob controller is unavailable.");
            return;
        }

        if (_pendingRewardedAction != null)
            return;

        _pendingRewardedAction = onReward;
        _pendingRewardedIsTimedOffer = isTimedOffer;
        adMob.LoadRewarded();
        ProcessPendingRewardedAction(adMob);
    }

    private void ProcessPendingRewardedAction(GoogleAdMobController adMob)
    {
        if (_pendingRewardedAction == null)
            return;

        if (adMob.IsRewardedReady())
        {
            Action<Reward> onReward = _pendingRewardedAction;
            bool isTimedOffer = _pendingRewardedIsTimedOffer;
            _pendingRewardedAction = null;
            _pendingRewardedIsTimedOffer = false;

            if (adMob.ShowRewarded(onReward))
            {
                if (isTimedOffer)
                    UIManager.Instance.HideTimedRewardedOffer();
                else
                    UIManager.Instance.SetRewardedContinueLoading(false);
                return;
            }

            _pendingRewardedAction = onReward;
            _pendingRewardedIsTimedOffer = isTimedOffer;
        }

        if (adMob.IsRewardedRequestPending())
        {
            if (_pendingRewardedIsTimedOffer)
                UIManager.Instance.SetTimedRewardedOfferStatus(false, true);
            else
                UIManager.Instance.SetRewardedContinueLoading(true);
            return;
        }

        bool wasTimedOffer = _pendingRewardedIsTimedOffer;
        _pendingRewardedAction = null;
        _pendingRewardedIsTimedOffer = false;
        if (wasTimedOffer)
            UIManager.Instance.SetTimedRewardedOfferStatus(false, false);
        else
            UIManager.Instance.SetRewardedContinueUnavailable();
    }

    private void ContinueAfterReward()
    {
        if (!IsGameOver)
            return;

        IsGameOver = false;
        _timeSinceRewardedOffer = 0f;
        _timeSinceInterstitial = 0f;
        _interstitialDue = false;
        _pendingRewardedAction = null;
        _pendingRewardedIsTimedOffer = false;
        GridManager.Instance.ResetGrid();
        BlockSpawner.Instance.ResetTray();
        UIManager.Instance.HideGameOver();
    }

    public void OpenAppStoreURL(string capturedUrl)
    {
        Application.OpenURL(capturedUrl);
    }
}

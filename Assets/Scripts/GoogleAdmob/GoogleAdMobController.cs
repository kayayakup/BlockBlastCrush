using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GoogleAdMobController : MonoBehaviour
{
    public static GoogleAdMobController Instance { get; private set; }

    [Header("Android Ad Unit IDs")]
    // Üretimde kendi Android reklam birimi ID'lerinizi kullanın.
    [SerializeField] private string androidBannerAdUnitId =
        "ca-app-pub-5398339005079750/2479007067";
    [SerializeField] private string androidInterstitialAdUnitId =
        "ca-app-pub-5398339005079750/1297787331";
    [SerializeField, Tooltip("AdMob Rewarded ad unit ID, e.g. ca-app-pub-.../... (not the App ID containing '~').")]
    private string androidRewardedAdUnitId = "";

    [Header("iOS Ad Unit IDs")]
    // iOS için AdMob'da ayrıca oluşturduğunuz ID'leri girin.
    [SerializeField] private string iosBannerAdUnitId = "";
    [SerializeField] private string iosInterstitialAdUnitId = "";
    [SerializeField] private string iosRewardedAdUnitId = "";

    [Header("Banner")]
    [SerializeField] private AdPosition bannerPosition = AdPosition.Bottom;
    [SerializeField] private bool autoLoadBannerOnStart = true;

    [Header("Fullscreen ad gameplay behavior")]
    [SerializeField] private bool pauseGameDuringInterstitial = true;
    [SerializeField] private bool pauseUnityAudioDuringInterstitial = false;

    [Header("Retry policy")]
    [SerializeField, Min(0)] private int maxRetryAttempts = 4;
    [SerializeField, Min(1f)] private float baseRetryDelaySeconds = 5f;
    [SerializeField, Min(1f)] private float maxRetryDelaySeconds = 60f;
    [SerializeField, Min(5f)] private float connectivityCheckIntervalSeconds = 5f;
    [SerializeField, Min(10f)] private float recoveryProbeIntervalSeconds = 30f;

    private BannerView _bannerView;
    private Action _bannerLoadedHandler;
    private Action<LoadAdError> _bannerFailedHandler;
    private Action _bannerImpressionHandler;
    private Action _bannerClickedHandler;

    private bool _bannerRequested;
    private bool _isBannerLoaded;
    private bool _isBannerLoading;
    private int _bannerRetryAttempts;
    private int _bannerGeneration;
    private int _bannerRetryToken;
    private Coroutine _bannerRetryRoutine;

    private InterstitialAd _interstitialAd;
    private Action _interstitialOpenedHandler;
    private Action _interstitialClosedHandler;
    private Action<AdError> _interstitialFailedToShowHandler;
    private Action _interstitialImpressionHandler;
    private Action _interstitialClickedHandler;

    private bool _isInterstitialLoading;
    private bool _isShowingInterstitial;
    private int _interstitialRetryAttempts;
    private int _interstitialGeneration;
    private int _interstitialRetryToken;
    private Coroutine _interstitialRetryRoutine;

    private RewardedAd _rewardedAd;
    private Action _rewardedOpenedHandler;
    private Action _rewardedClosedHandler;
    private Action<AdError> _rewardedFailedToShowHandler;
    private Action _rewardedImpressionHandler;
    private Action _rewardedClickedHandler;

    private bool _isRewardedLoading;
    private bool _isShowingRewarded;
    private int _rewardedRetryAttempts;
    private int _rewardedGeneration;
    private int _rewardedRetryToken;
    private Coroutine _rewardedRetryRoutine;

    private bool _consentFlowInProgress;
    private bool _mobileAdsInitializing;
    private bool _isAdMobInitialized;
    private bool _disposed;
    private bool _gameStateCaptured;
    private float _previousTimeScale;
    private bool _previousAudioListenerPause;
    private float _lastRecoveryProbeTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _bannerRequested = autoLoadBannerOnStart;
        StartCoroutine(ConnectivityMonitor());
        StartConsentFlow();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (!isPaused && !_disposed)
            RefreshAds();
    }

    private void OnDestroy()
    {
        _disposed = true;

        if (Instance == this)
            Instance = null;

        StopAllCoroutines();
        _bannerRetryRoutine = null;
        _interstitialRetryRoutine = null;
        _rewardedRetryRoutine = null;

        DestroyBannerInternal();
        RestoreGameStateAfterInterstitial();
        ReleaseInterstitial();
        ReleaseRewarded();
    }

    private void OnValidate()
    {
        maxRetryAttempts = Mathf.Max(0, maxRetryAttempts);
        baseRetryDelaySeconds = Mathf.Max(1f, baseRetryDelaySeconds);
        maxRetryDelaySeconds = Mathf.Max(baseRetryDelaySeconds, maxRetryDelaySeconds);
        connectivityCheckIntervalSeconds = Mathf.Max(5f, connectivityCheckIntervalSeconds);
        recoveryProbeIntervalSeconds = Mathf.Max(10f, recoveryProbeIntervalSeconds);
    }

    // -------------------- Consent / SDK initialization --------------------

    private void StartConsentFlow()
    {
        if (_disposed || _isAdMobInitialized || _mobileAdsInitializing ||
            _consentFlowInProgress)
            return;

        if (Application.internetReachability == NetworkReachability.NotReachable)
            return;

        _consentFlowInProgress = true;

        var parameters = new ConsentRequestParameters
        {
            // Bu değer uygulamanın gerçek hedef kitlesine göre ayarlanmalıdır.
            TagForUnderAgeOfConsent = false
        };

        ConsentInformation.Update(parameters, OnConsentInfoUpdated);
    }

    private void OnConsentInfoUpdated(FormError consentError)
    {
        if (_disposed)
            return;

        if (consentError != null)
        {
            Debug.LogWarning($"[AdMob] Consent update failed: {consentError.Message}");
            _consentFlowInProgress = false;
            TryInitializeAdsIfConsentAllows();
            return;
        }

        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
        {
            if (_disposed)
                return;

            if (formError != null)
                Debug.LogWarning($"[AdMob] Consent form: {formError.Message}");

            _consentFlowInProgress = false;
            TryInitializeAdsIfConsentAllows();
        });
    }

    private void TryInitializeAdsIfConsentAllows()
    {
        if (_disposed || _isAdMobInitialized || _mobileAdsInitializing)
            return;

        if (!ConsentInformation.CanRequestAds())
            return;

        _mobileAdsInitializing = true;

        // Gerekliyse RequestConfiguration burada, SDK initialize edilmeden önce
        // uygulamanın hedef kitlesine uygun şekilde ayarlanmalıdır.
        MobileAds.Initialize(_ =>
        {
            _mobileAdsInitializing = false;

            if (_disposed)
                return;

            _isAdMobInitialized = true;
            Debug.Log("[AdMob] SDK initialized.");

            if (_bannerRequested)
                TryLoadBanner();

            TryLoadInterstitial();
            TryLoadRewarded();
        });
    }

    /// Uygulamanızda gizlilik ayarları menüsü varsa buradan çağırabilirsiniz.
    public void ShowPrivacyOptionsForm()
    {
        if (_disposed)
            return;

        ConsentForm.ShowPrivacyOptionsForm(error =>
        {
            if (error != null)
                Debug.LogWarning($"[AdMob] Privacy options form: {error.Message}");

            if (ConsentInformation.CanRequestAds())
                TryInitializeAdsIfConsentAllows();
        });
    }

    private bool CanRequestAds()
    {
        return !_disposed &&
               _isAdMobInitialized &&
               ConsentInformation.CanRequestAds() &&
               Application.internetReachability != NetworkReachability.NotReachable;
    }

    // -------------------- Platform IDs --------------------

    private string GetBannerAdUnitId()
    {
#if UNITY_EDITOR
        return "ca-app-pub-3940256099942544/6300978111"; // Google test banner
#elif UNITY_ANDROID
        return androidBannerAdUnitId;
#elif UNITY_IOS
        return iosBannerAdUnitId;
#else
        return "";
#endif
    }

    private string GetInterstitialAdUnitId()
    {
#if UNITY_EDITOR
        return "ca-app-pub-3940256099942544/1033173712"; // Google test interstitial
#elif UNITY_ANDROID
        return androidInterstitialAdUnitId;
#elif UNITY_IOS
        return iosInterstitialAdUnitId;
#else
        return "";
#endif
    }

    private string GetRewardedAdUnitId()
    {
#if UNITY_EDITOR
        return "ca-app-pub-3940256099942544/5224354917"; // Google test rewarded
#elif UNITY_ANDROID
        return androidRewardedAdUnitId;
#elif UNITY_IOS
        return iosRewardedAdUnitId;
#else
        return "";
#endif
    }

    private bool ValidateAdUnitId(string adUnitId, string format)
    {
        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogError(
                $"[AdMob] {format} ad unit ID is not configured. Copy the ad unit ID from AdMob; do not use the app ID.");
            return false;
        }

        int separatorIndex = adUnitId.IndexOf('/');
        if (adUnitId.Contains("~") || separatorIndex <= 0 ||
            separatorIndex == adUnitId.Length - 1 ||
            !adUnitId.StartsWith("ca-app-pub-", StringComparison.Ordinal))
        {
            Debug.LogError(
                $"[AdMob] Invalid {format} ad unit ID '{adUnitId}'. Ad unit IDs use 'ca-app-pub-.../...'; values containing '~' are app IDs.");
            return false;
        }

        return true;
    }

    // -------------------- Banner --------------------

    public void LoadBanner()
    {
        _bannerRequested = true;
        TryLoadBanner();
    }

    public void ShowBanner()
    {
        _bannerRequested = true;

        if (_isBannerLoaded)
            _bannerView?.Show();
        else
            TryLoadBanner();
    }

    public void HideBanner()
    {
        _bannerRequested = false;
        _bannerView?.Hide();
    }

    public void DestroyBanner()
    {
        _bannerRequested = false;
        _bannerRetryAttempts = 0;
        CancelBannerRetry();
        DestroyBannerInternal();
    }

    private void TryLoadBanner()
    {
        if (_disposed || !_bannerRequested || _isBannerLoaded || _isBannerLoading)
            return;

        if (!_isAdMobInitialized)
        {
            StartConsentFlow();
            return;
        }

        if (!CanRequestAds())
        {
            ScheduleBannerRetry();
            return;
        }

        string adUnitId = GetBannerAdUnitId();
        if (!ValidateAdUnitId(adUnitId, "Banner"))
            return;

        CancelBannerRetry();
        DestroyBannerInternal();

        _isBannerLoading = true;

        // Adaptive banner, mevcut ekran genişliğine uyum sağlar.
        AdSize adSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        var view = new BannerView(adUnitId, adSize, bannerPosition);
        _bannerView = view;
        int generation = ++_bannerGeneration;

        _bannerLoadedHandler = () =>
        {
            if (_disposed || generation != _bannerGeneration || view != _bannerView)
                return;

            _isBannerLoading = false;
            _isBannerLoaded = true;
            _bannerRetryAttempts = 0;
            CancelBannerRetry();

            Debug.Log("[AdMob] Banner loaded.");
            view.Show();
        };

        _bannerFailedHandler = error =>
        {
            if (_disposed || generation != _bannerGeneration || view != _bannerView)
                return;

            _isBannerLoading = false;
            _isBannerLoaded = false;

            Debug.LogWarning(
                $"[AdMob] Banner load failed: {error?.GetCode()} - {error?.GetMessage()}");

            ScheduleBannerRetry();
        };

        _bannerImpressionHandler = () =>
            Debug.Log("[AdMob] Banner impression.");

        _bannerClickedHandler = () =>
            Debug.Log("[AdMob] Banner clicked.");

        view.OnBannerAdLoaded += _bannerLoadedHandler;
        view.OnBannerAdLoadFailed += _bannerFailedHandler;
        view.OnAdImpressionRecorded += _bannerImpressionHandler;
        view.OnAdClicked += _bannerClickedHandler;

        view.LoadAd(new AdRequest());
    }

    private void DestroyBannerInternal()
    {
        // Eski view'den gelebilecek callback'leri geçersiz kıl.
        _bannerGeneration++;

        if (_bannerView != null)
        {
            if (_bannerLoadedHandler != null)
                _bannerView.OnBannerAdLoaded -= _bannerLoadedHandler;

            if (_bannerFailedHandler != null)
                _bannerView.OnBannerAdLoadFailed -= _bannerFailedHandler;

            if (_bannerImpressionHandler != null)
                _bannerView.OnAdImpressionRecorded -= _bannerImpressionHandler;

            if (_bannerClickedHandler != null)
                _bannerView.OnAdClicked -= _bannerClickedHandler;

            _bannerView.Destroy();
        }

        _bannerView = null;
        _bannerLoadedHandler = null;
        _bannerFailedHandler = null;
        _bannerImpressionHandler = null;
        _bannerClickedHandler = null;
        _isBannerLoaded = false;
        _isBannerLoading = false;
    }

    // -------------------- Interstitial --------------------

    public bool IsInterstitialReady()
    {
        return !_disposed &&
               !_isShowingInterstitial &&
               !_isShowingRewarded &&
               _interstitialAd != null &&
               _interstitialAd.CanShowAd();
    }

    /// true dönerse Show çağrılmıştır; hazır değilse yükleme başlatılır ve false döner.
    public bool ShowInterstitial()
    {
        if (_disposed || _isShowingInterstitial || _isShowingRewarded)
            return false;

        if (!IsInterstitialReady())
        {
            TryLoadInterstitial();
            return false;
        }

        _isShowingInterstitial = true;

        try
        {
            _interstitialAd.Show();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[AdMob] Interstitial Show failed: {exception.Message}");
            _isShowingInterstitial = false;
            RestoreGameStateAfterInterstitial();
            ReleaseInterstitial();
            ScheduleInterstitialRetry();
            return false;
        }
    }

    public void LoadInterstitial()
    {
        TryLoadInterstitial();
    }

    private void TryLoadInterstitial()
    {
        if (_disposed || _isInterstitialLoading ||
            (_interstitialAd != null && _interstitialAd.CanShowAd()))
            return;

        if (!_isAdMobInitialized)
        {
            StartConsentFlow();
            return;
        }

        if (!CanRequestAds())
        {
            ScheduleInterstitialRetry();
            return;
        }

        string adUnitId = GetInterstitialAdUnitId();
        if (!ValidateAdUnitId(adUnitId, "Interstitial"))
            return;

        CancelInterstitialRetry();
        ReleaseInterstitial();

        _isInterstitialLoading = true;
        int requestGeneration = ++_interstitialGeneration;

        InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            // Controller yok edilmiş veya daha yeni bir istek başlamış olabilir.
            if (_disposed || requestGeneration != _interstitialGeneration)
            {
                ad?.Destroy();
                return;
            }

            _isInterstitialLoading = false;

            if (error != null || ad == null)
            {
                ad?.Destroy();
                Debug.LogWarning(
                    $"[AdMob] Interstitial load failed: {error?.GetCode()} - {error?.GetMessage()}");
                ScheduleInterstitialRetry();
                return;
            }

            _interstitialAd = ad;
            _interstitialRetryAttempts = 0;
            AttachInterstitialEvents(ad);

            Debug.Log("[AdMob] Interstitial loaded.");
        });
    }

    private void AttachInterstitialEvents(InterstitialAd ad)
    {
        _interstitialOpenedHandler = () =>
        {
            if (_disposed || ad != _interstitialAd)
                return;

            _isShowingInterstitial = true;
            CaptureAndPauseGameState();
            Debug.Log("[AdMob] Interstitial opened.");
        };

        _interstitialClosedHandler = () =>
        {
            if (_disposed || ad != _interstitialAd)
                return;

            Debug.Log("[AdMob] Interstitial closed.");
            _isShowingInterstitial = false;
            RestoreGameStateAfterInterstitial();
            ReleaseInterstitial();

            // Interstitial tek kullanımlıktır; sonraki gösterim için yeniden yükle.
            TryLoadInterstitial();
        };

        _interstitialFailedToShowHandler = error =>
        {
            if (_disposed || ad != _interstitialAd)
                return;

            Debug.LogWarning(
                $"[AdMob] Interstitial failed to show: {error?.GetMessage()}");

            _isShowingInterstitial = false;
            RestoreGameStateAfterInterstitial();
            ReleaseInterstitial();
            ScheduleInterstitialRetry();
        };

        _interstitialImpressionHandler = () =>
            Debug.Log("[AdMob] Interstitial impression.");

        _interstitialClickedHandler = () =>
            Debug.Log("[AdMob] Interstitial clicked.");

        ad.OnAdFullScreenContentOpened += _interstitialOpenedHandler;
        ad.OnAdFullScreenContentClosed += _interstitialClosedHandler;
        ad.OnAdFullScreenContentFailed += _interstitialFailedToShowHandler;
        ad.OnAdImpressionRecorded += _interstitialImpressionHandler;
        ad.OnAdClicked += _interstitialClickedHandler;
    }

    private void ReleaseInterstitial()
    {
        // Önce generation artırılır: eski yükleme callback'i artık geçersizdir.
        _interstitialGeneration++;

        if (_interstitialAd != null)
        {
            if (_interstitialOpenedHandler != null)
                _interstitialAd.OnAdFullScreenContentOpened -= _interstitialOpenedHandler;

            if (_interstitialClosedHandler != null)
                _interstitialAd.OnAdFullScreenContentClosed -= _interstitialClosedHandler;

            if (_interstitialFailedToShowHandler != null)
                _interstitialAd.OnAdFullScreenContentFailed -= _interstitialFailedToShowHandler;

            if (_interstitialImpressionHandler != null)
                _interstitialAd.OnAdImpressionRecorded -= _interstitialImpressionHandler;

            if (_interstitialClickedHandler != null)
                _interstitialAd.OnAdClicked -= _interstitialClickedHandler;

            _interstitialAd.Destroy();
        }

        _interstitialAd = null;
        _interstitialOpenedHandler = null;
        _interstitialClosedHandler = null;
        _interstitialFailedToShowHandler = null;
        _interstitialImpressionHandler = null;
        _interstitialClickedHandler = null;
        _isInterstitialLoading = false;
    }

    // -------------------- Rewarded --------------------

    public bool IsRewardedReady()
    {
        return !_disposed &&
               !_isShowingRewarded &&
               !_isShowingInterstitial &&
               _rewardedAd != null &&
               _rewardedAd.CanShowAd();
    }

    public bool IsRewardedLoading()
    {
        return !_disposed && _isRewardedLoading;
    }

    public bool IsRewardedRequestPending()
    {
        return !_disposed &&
               (_isRewardedLoading ||
                _mobileAdsInitializing ||
                _consentFlowInProgress ||
                _rewardedRetryRoutine != null);
    }

    /// <summary>
    /// Reklam gösterildiyse true döner. Ödül yalnızca kullanıcı reklamı tamamladığında verilir.
    /// </summary>
    public bool ShowRewarded(Action<Reward> onUserEarnedReward)
    {
        if (_disposed || _isShowingRewarded || _isShowingInterstitial)
            return false;

        if (onUserEarnedReward == null)
        {
            Debug.LogError("[AdMob] Rewarded ad requires a reward callback.");
            return false;
        }

        if (!IsRewardedReady())
        {
            TryLoadRewarded();
            return false;
        }

        RewardedAd ad = _rewardedAd;
        _isShowingRewarded = true;
        bool rewardGranted = false;

        try
        {
            ad.Show(reward =>
            {
                if (_disposed || ad != _rewardedAd || rewardGranted)
                    return;

                rewardGranted = true;
                onUserEarnedReward?.Invoke(reward);
            });
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[AdMob] Rewarded Show failed: {exception.Message}");
            _isShowingRewarded = false;
            RestoreGameStateAfterInterstitial();
            ReleaseRewarded();
            ScheduleRewardedRetry();
            return false;
        }
    }

    public void LoadRewarded()
    {
        TryLoadRewarded();
    }

    private void TryLoadRewarded()
    {
        if (_disposed || _isRewardedLoading ||
            (_rewardedAd != null && _rewardedAd.CanShowAd()))
            return;

        if (!_isAdMobInitialized)
        {
            StartConsentFlow();
            return;
        }

        if (!CanRequestAds())
        {
            ScheduleRewardedRetry();
            return;
        }

        string adUnitId = GetRewardedAdUnitId();
        if (!ValidateAdUnitId(adUnitId, "Rewarded"))
            return;

        CancelRewardedRetry();
        ReleaseRewarded();

        _isRewardedLoading = true;
        int requestGeneration = ++_rewardedGeneration;

        RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            if (_disposed || requestGeneration != _rewardedGeneration)
            {
                ad?.Destroy();
                return;
            }

            _isRewardedLoading = false;

            if (error != null || ad == null)
            {
                ad?.Destroy();
                Debug.LogWarning(
                    $"[AdMob] Rewarded load failed: {error?.GetCode()} - {error?.GetMessage()}");
                ScheduleRewardedRetry();
                return;
            }

            _rewardedAd = ad;
            _rewardedRetryAttempts = 0;
            AttachRewardedEvents(ad);

            Debug.Log("[AdMob] Rewarded loaded.");
        });
    }

    private void AttachRewardedEvents(RewardedAd ad)
    {
        _rewardedOpenedHandler = () =>
        {
            if (_disposed || ad != _rewardedAd)
                return;

            _isShowingRewarded = true;
            CaptureAndPauseGameState();
            Debug.Log("[AdMob] Rewarded opened.");
        };

        _rewardedClosedHandler = () =>
        {
            if (_disposed || ad != _rewardedAd)
                return;

            Debug.Log("[AdMob] Rewarded closed.");
            _isShowingRewarded = false;
            RestoreGameStateAfterInterstitial();
            ReleaseRewarded();
            TryLoadRewarded();
        };

        _rewardedFailedToShowHandler = error =>
        {
            if (_disposed || ad != _rewardedAd)
                return;

            Debug.LogWarning(
                $"[AdMob] Rewarded failed to show: {error?.GetMessage()}");

            _isShowingRewarded = false;
            RestoreGameStateAfterInterstitial();
            ReleaseRewarded();
            ScheduleRewardedRetry();
        };

        _rewardedImpressionHandler = () =>
            Debug.Log("[AdMob] Rewarded impression.");

        _rewardedClickedHandler = () =>
            Debug.Log("[AdMob] Rewarded clicked.");

        ad.OnAdFullScreenContentOpened += _rewardedOpenedHandler;
        ad.OnAdFullScreenContentClosed += _rewardedClosedHandler;
        ad.OnAdFullScreenContentFailed += _rewardedFailedToShowHandler;
        ad.OnAdImpressionRecorded += _rewardedImpressionHandler;
        ad.OnAdClicked += _rewardedClickedHandler;
    }

    private void ReleaseRewarded()
    {
        _rewardedGeneration++;

        if (_rewardedAd != null)
        {
            if (_rewardedOpenedHandler != null)
                _rewardedAd.OnAdFullScreenContentOpened -= _rewardedOpenedHandler;

            if (_rewardedClosedHandler != null)
                _rewardedAd.OnAdFullScreenContentClosed -= _rewardedClosedHandler;

            if (_rewardedFailedToShowHandler != null)
                _rewardedAd.OnAdFullScreenContentFailed -= _rewardedFailedToShowHandler;

            if (_rewardedImpressionHandler != null)
                _rewardedAd.OnAdImpressionRecorded -= _rewardedImpressionHandler;

            if (_rewardedClickedHandler != null)
                _rewardedAd.OnAdClicked -= _rewardedClickedHandler;

            _rewardedAd.Destroy();
        }

        _rewardedAd = null;
        _rewardedOpenedHandler = null;
        _rewardedClosedHandler = null;
        _rewardedFailedToShowHandler = null;
        _rewardedImpressionHandler = null;
        _rewardedClickedHandler = null;
        _isRewardedLoading = false;
    }

    // -------------------- Game state --------------------

    private void CaptureAndPauseGameState()
    {
        if (_gameStateCaptured)
            return;

        _gameStateCaptured = true;
        _previousTimeScale = Time.timeScale;
        _previousAudioListenerPause = AudioListener.pause;

        if (pauseGameDuringInterstitial)
            Time.timeScale = 0f;

        if (pauseUnityAudioDuringInterstitial)
            AudioListener.pause = true;
    }

    private void RestoreGameStateAfterInterstitial()
    {
        if (!_gameStateCaptured)
            return;

        if (pauseGameDuringInterstitial)
            Time.timeScale = _previousTimeScale;

        if (pauseUnityAudioDuringInterstitial)
            AudioListener.pause = _previousAudioListenerPause;

        _gameStateCaptured = false;
    }

    // -------------------- Retry / connectivity --------------------

    private void ScheduleBannerRetry()
    {
        if (_disposed || !_bannerRequested || _bannerRetryRoutine != null ||
            _bannerRetryAttempts >= maxRetryAttempts)
            return;

        float delay = CalculateBackoff(_bannerRetryAttempts++);
        int token = ++_bannerRetryToken;
        _bannerRetryRoutine = StartCoroutine(BannerRetryRoutine(delay, token));
    }

    private IEnumerator BannerRetryRoutine(float delay, int token)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (_disposed || token != _bannerRetryToken)
            yield break;

        _bannerRetryRoutine = null;
        TryLoadBanner();
    }

    private void ScheduleInterstitialRetry()
    {
        if (_disposed || _interstitialRetryRoutine != null ||
            _interstitialRetryAttempts >= maxRetryAttempts)
            return;

        float delay = CalculateBackoff(_interstitialRetryAttempts++);
        int token = ++_interstitialRetryToken;
        _interstitialRetryRoutine = StartCoroutine(
            InterstitialRetryRoutine(delay, token));
    }

    private void ScheduleRewardedRetry()
    {
        if (_disposed || _rewardedRetryRoutine != null ||
            _rewardedRetryAttempts >= maxRetryAttempts)
            return;

        float delay = CalculateBackoff(_rewardedRetryAttempts++);
        int token = ++_rewardedRetryToken;
        _rewardedRetryRoutine = StartCoroutine(
            RewardedRetryRoutine(delay, token));
    }

    private IEnumerator InterstitialRetryRoutine(float delay, int token)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (_disposed || token != _interstitialRetryToken)
            yield break;

        _interstitialRetryRoutine = null;
        TryLoadInterstitial();
    }

    private IEnumerator RewardedRetryRoutine(float delay, int token)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (_disposed || token != _rewardedRetryToken)
            yield break;

        _rewardedRetryRoutine = null;
        TryLoadRewarded();
    }

    private float CalculateBackoff(int attempt)
    {
        return Mathf.Min(
            baseRetryDelaySeconds * Mathf.Pow(2f, attempt),
            maxRetryDelaySeconds);
    }

    private void CancelBannerRetry()
    {
        _bannerRetryToken++;

        if (_bannerRetryRoutine != null)
        {
            StopCoroutine(_bannerRetryRoutine);
            _bannerRetryRoutine = null;
        }
    }

    private void CancelInterstitialRetry()
    {
        _interstitialRetryToken++;

        if (_interstitialRetryRoutine != null)
        {
            StopCoroutine(_interstitialRetryRoutine);
            _interstitialRetryRoutine = null;
        }
    }

    private void CancelRewardedRetry()
    {
        _rewardedRetryToken++;

        if (_rewardedRetryRoutine != null)
        {
            StopCoroutine(_rewardedRetryRoutine);
            _rewardedRetryRoutine = null;
        }
    }

    private IEnumerator ConnectivityMonitor()
    {
        NetworkReachability previous = Application.internetReachability;

        while (!_disposed)
        {
            yield return new WaitForSecondsRealtime(connectivityCheckIntervalSeconds);

            if (_disposed)
                yield break;

            NetworkReachability current = Application.internetReachability;
            bool reachable = current != NetworkReachability.NotReachable;
            bool justBecameReachable =
                reachable && previous == NetworkReachability.NotReachable;
            bool recoveryProbeDue =
                Time.realtimeSinceStartup - _lastRecoveryProbeTime >=
                recoveryProbeIntervalSeconds;

            if (reachable && (justBecameReachable || recoveryProbeDue))
            {
                _lastRecoveryProbeTime = Time.realtimeSinceStartup;

                if (!_isAdMobInitialized)
                {
                    StartConsentFlow();
                    TryInitializeAdsIfConsentAllows();
                }
                else
                {
                    bool bannerNeedsRetry =
                        _bannerRequested && !_isBannerLoaded && !_isBannerLoading &&
                        _bannerRetryRoutine == null;

                    bool interstitialNeedsRetry =
                        !IsInterstitialReady() && !_isInterstitialLoading &&
                        !_isShowingInterstitial && _interstitialRetryRoutine == null;

                    bool rewardedNeedsRetry =
                        !IsRewardedReady() && !_isRewardedLoading &&
                        !_isShowingRewarded && _rewardedRetryRoutine == null;

                    if (justBecameReachable || bannerNeedsRetry ||
                        interstitialNeedsRetry || rewardedNeedsRetry)
                        RefreshAds();
                }
            }

            previous = current;
        }
    }

    /// Ağ geri geldiğinde veya uygulama ön plana döndüğünde çağrılabilir.
    public void RefreshAds()
    {
        if (_disposed)
            return;

        if (!_isAdMobInitialized)
        {
            StartConsentFlow();
            TryInitializeAdsIfConsentAllows();
            return;
        }

        _bannerRetryAttempts = 0;
        _interstitialRetryAttempts = 0;
        _rewardedRetryAttempts = 0;

        if (_bannerRequested && !_isBannerLoaded && !_isBannerLoading)
            TryLoadBanner();

        if (!_isShowingInterstitial && !IsInterstitialReady() &&
            !_isInterstitialLoading)
            TryLoadInterstitial();

        if (!_isShowingRewarded && !IsRewardedReady() && !_isRewardedLoading)
            TryLoadRewarded();
    }
}
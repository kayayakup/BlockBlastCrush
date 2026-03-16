// ── UIManager.cs ──────────────────────────────────────────────────────────────
// Place in: Assets/Scripts/UI/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Builds and manages ALL UI elements in code — no prefabs or scene assets needed.
///
/// Layout (matching reference screenshot):
///   Top-left:  Crown icon + best-score number (small)
///   Top-right: Gear / settings icon
///   Centre-top: Large current-score number
///   Overlay:   Game-over panel (dark fade + bounce panel + restart button)
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── Component references ───────────────────────────────────────────────────
    private Canvas        _canvas;
    private TMP_Text      _scoreTMP;
    private TMP_Text      _bestTMP;
    private Image         _crownImg;
    private Image         _gearImg;

    // Score pop-up
    private RectTransform _popupRT;
    private TMP_Text      _popupTMP;
    private CanvasGroup   _popupCG;

    // Game-over panel
    private GameObject  _gameOverPanel;
    private TMP_Text    _gameOverScoreTMP;

    // World-space background
    private GameObject _bgObj;

    // Safe area
    private RectTransform _safeAreaRT;
    private Rect          _lastSafeArea;

    void Awake() => Instance = this;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        BuildWorldBackground();
        BuildCanvas();
        BuildSafeArea();
        BuildTopBar();
        BuildScorePopup();
        BuildGameOverPanel();

        ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
        RefreshScoreTexts(0, ScoreManager.Instance.BestScore);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ShowGameOver(int finalScore)
    {
        _gameOverScoreTMP.text = $"SCORE   {finalScore}\nBEST   {ScoreManager.Instance.BestScore}";
        _gameOverPanel.SetActive(true);

        var cg = _gameOverPanel.GetComponent<CanvasGroup>();
        DOTween.Kill(cg);
        cg.alpha = 0f;
        cg.DOFade(1f, 0.40f).SetEase(Ease.OutQuad);

        var rt = _gameOverPanel.GetComponent<RectTransform>();
        DOTween.Kill(rt);
        rt.localScale = Vector3.one * 0.7f;
        rt.DOScale(1f, 0.42f).SetEase(Ease.OutBack);
    }

    public void HideGameOver()
    {
        _gameOverPanel.SetActive(false);
    }

    // ── Score event ───────────────────────────────────────────────────────────

    private void HandleScoreChanged(int current, int best, int delta)
    {
        RefreshScoreTexts(current, best);
        if (delta >= Constants.POINTS_PER_LINE)
        {
            ShowScorePopup($"+{delta}");
            AudioManager.Instance?.PlayScore();
        }
    }

    private void RefreshScoreTexts(int score, int best)
    {
        _scoreTMP.text = score.ToString();
        _bestTMP.text  = best.ToString();

        DOTween.Kill(_scoreTMP.rectTransform);
        _scoreTMP.rectTransform
            .DOPunchScale(Vector3.one * 0.20f, 0.28f, 2, 0.5f)
            .SetEase(Ease.OutQuad);
    }

    private void ShowScorePopup(string text)
    {
        DOTween.Kill(_popupCG);
        DOTween.Kill(_popupRT);

        _popupTMP.text = text;
        _popupCG.alpha = 1f;

        // Anchor popup in the middle of the canvas
        _popupRT.anchoredPosition = new Vector2(0f, 80f);
        _popupRT.localScale = Vector3.one * 0.4f;

        DOTween.Sequence()
               .Append(_popupRT.DOScale(1.5f, 0.18f).SetEase(Ease.OutBack))
               .Append(_popupRT.DOScale(1.1f, 0.10f))
               .AppendInterval(0.50f)
               .Append(_popupCG.DOFade(0f, 0.22f))
               .Join(_popupRT.DOAnchorPosY(160f, 0.22f));
    }

    // ── World-space background ────────────────────────────────────────────────

    private void BuildWorldBackground()
    {
        _bgObj = new GameObject("Background");
        _bgObj.transform.SetParent(transform);
        _bgObj.transform.position = new Vector3(0f, 0f, 1f);

        // Tall sprite that fills the camera view completely
        float camH  = Constants.CAMERA_ORTHO_SIZE * 2f;
        float camW  = camH * Screen.width / Mathf.Max(Screen.height, 1);
        _bgObj.transform.localScale = new Vector3(camW + 1f, camH + 1f, 1f);

        var sr          = _bgObj.AddComponent<SpriteRenderer>();
        sr.sprite       = TextureUtils.CreateRoundedRect(4, 4, 0, Constants.BgColor);
        sr.sortingOrder = -10;

        // Tray-area strip (slightly darker at bottom)
        var trayBg = new GameObject("TrayBG");
        trayBg.transform.SetParent(_bgObj.transform);
        trayBg.transform.localPosition = new Vector3(0f,
            (Constants.TRAY_Y - Constants.CAMERA_ORTHO_SIZE) / camH * 2f + 0.5f - 0.08f, -0.05f);

        float stripH = (Constants.CAMERA_ORTHO_SIZE + Constants.TRAY_Y - 1.1f) / camH * (-1f);
        var   trayGO = new GameObject("TrayStrip");
        trayGO.transform.SetParent(transform);
        trayGO.transform.position = new Vector3(0f, Constants.TRAY_Y - 0.9f, 0.8f);
        trayGO.transform.localScale = new Vector3(camW + 1f, Mathf.Max(0.1f, (-Constants.TRAY_Y + Constants.CAMERA_ORTHO_SIZE - 0.5f) * 2f), 1f);

        var tsr = trayGO.AddComponent<SpriteRenderer>();
        tsr.sprite       = TextureUtils.CreateRoundedRect(4, 4, 0, Constants.TrayBgColor);
        tsr.sortingOrder = -9;
    }

    // ── Canvas construction ───────────────────────────────────────────────────

    private void BuildCanvas()
    {
        _canvas              = gameObject.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler                 = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2400f);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
    }

    // ── Safe-area helper ──────────────────────────────────────────────────────

    private void BuildSafeArea()
    {
        var safeGO = NewUIGO("SafeArea", _canvas.transform);
        _safeAreaRT = safeGO.GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        var safeArea = Screen.safeArea;
        if (safeArea == _lastSafeArea) return;
        _lastSafeArea = safeArea;

        var canvasRT = _canvas.GetComponent<RectTransform>();
        var canvasSize = canvasRT.rect.size;
        if (canvasSize.x <= 0 || canvasSize.y <= 0) return;

        var anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
        var anchorMax = new Vector2((safeArea.x + safeArea.width) / Screen.width,
                                    (safeArea.y + safeArea.height) / Screen.height);

        _safeAreaRT.anchorMin = anchorMin;
        _safeAreaRT.anchorMax = anchorMax;
        _safeAreaRT.offsetMin = Vector2.zero;
        _safeAreaRT.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (_safeAreaRT != null) ApplySafeArea();
    }

    // ── Top bar (crown + best + gear + score) ─────────────────────────────────

    private void BuildTopBar()
    {
        // ── Invisible top-bar container (anchored to top of safe area) ────────
        var barGO = NewUIGO("TopBar", _safeAreaRT);
        var barRT = barGO.GetComponent<RectTransform>();
        AnchorStretchTop(barRT, 0f, 104f);

        // Subtle background
        var barImg   = barGO.AddComponent<Image>();
        barImg.color = Constants.TopBarColor;
        barImg.sprite = TextureUtils.CreateRoundedRect(4, 4, 0, Color.white);

        // ── Crown icon ────────────────────────────────────────────────────────
        var crownGO = NewUIGO("CrownIcon", barGO.transform);
        var crownRT = crownGO.GetComponent<RectTransform>();
        crownRT.anchorMin = crownRT.anchorMax = crownRT.pivot = new Vector2(0f, 0.5f);
        crownRT.anchoredPosition = new Vector2(22f, 0f);
        crownRT.sizeDelta        = new Vector2(52f, 40f);
        _crownImg         = crownGO.AddComponent<Image>();
        _crownImg.sprite  = TextureUtils.CrownSprite;
        _crownImg.preserveAspect = true;

        // ── Best-score label ──────────────────────────────────────────────────
        var bestGO = NewUIGO("BestScore", barGO.transform);
        var bestRT = bestGO.GetComponent<RectTransform>();
        bestRT.anchorMin = bestRT.anchorMax = bestRT.pivot = new Vector2(0f, 0.5f);
        bestRT.anchoredPosition = new Vector2(80f, 0f);
        bestRT.sizeDelta        = new Vector2(280f, 80f);
        _bestTMP = bestGO.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(_bestTMP, "0", 44f, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);

        // ── Gear icon (top-right) ─────────────────────────────────────────────
        var gearGO = NewUIGO("GearIcon", barGO.transform);
        var gearRT = gearGO.GetComponent<RectTransform>();
        gearRT.anchorMin = gearRT.anchorMax = gearRT.pivot = new Vector2(1f, 0.5f);
        gearRT.anchoredPosition = new Vector2(-22f, 0f);
        gearRT.sizeDelta        = new Vector2(44f, 44f);
        _gearImg         = gearGO.AddComponent<Image>();
        _gearImg.sprite  = TextureUtils.GearSprite;
        _gearImg.color   = new Color(0.85f, 0.90f, 1f, 0.90f);
        _gearImg.preserveAspect = true;

        // Gear ikonuna Button ekle
        var gearBtn = gearGO.AddComponent<Button>();
        gearBtn.targetGraphic = _gearImg;
        var gearColors = gearBtn.colors;
        gearColors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        gearColors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        gearBtn.colors = gearColors;
        gearBtn.onClick.AddListener(OnGearClicked);

        // ── Current score (large, centred, just below top bar) ─────────────────
        var scoreGO = NewUIGO("ScoreLabel", _safeAreaRT);
        var scoreRT = scoreGO.GetComponent<RectTransform>();
        scoreRT.anchorMin = scoreRT.anchorMax = new Vector2(0.5f, 1f);
        scoreRT.pivot     = new Vector2(0.5f, 1f);
        scoreRT.anchoredPosition = new Vector2(0f, -130f);
        scoreRT.sizeDelta        = new Vector2(600f, 130f);
        _scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(_scoreTMP, "0", 108f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
    }

    private void OnGearClicked()
    {
        var audio = AudioManager.Instance;
        if (audio == null) return;
        audio.ToggleMute();
        // Sessizse ikonu soluklaştır, aktifse parlak göster
        _gearImg.color = audio.IsMuted
            ? new Color(0.5f, 0.5f, 0.5f, 0.55f)
            : new Color(0.85f, 0.90f, 1f, 0.90f);
    }

    // ── Score pop-up ──────────────────────────────────────────────────────────

    private void BuildScorePopup()
    {
        var go   = NewUIGO("ScorePopup", _canvas.transform);
        _popupRT = go.GetComponent<RectTransform>();
        _popupRT.anchorMin = _popupRT.anchorMax = new Vector2(0.5f, 0.5f);
        _popupRT.pivot     = new Vector2(0.5f, 0.5f);
        _popupRT.sizeDelta = new Vector2(340f, 90f);
        _popupRT.anchoredPosition = new Vector2(0f, 80f);

        _popupTMP = go.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(_popupTMP, "", 58f, new Color(1f, 0.92f, 0.15f), FontStyles.Bold, TextAlignmentOptions.Center);

        _popupCG       = go.AddComponent<CanvasGroup>();
        _popupCG.alpha = 0f;
    }

    // ── Game-over panel ───────────────────────────────────────────────────────

    private void BuildGameOverPanel()
    {
        // Full-screen dark overlay
        _gameOverPanel = NewUIGO("GameOverPanel", _canvas.transform);
        var panelRT    = _gameOverPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        var panelImg  = _gameOverPanel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0.02f, 0.08f, 0.88f);

        var cg = _gameOverPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Inner white card
        var cardGO = NewUIGO("Card", _gameOverPanel.transform);
        var cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(560f, 420f);
        cardRT.anchoredPosition = Vector2.zero;

        var cardImg    = cardGO.AddComponent<Image>();
        cardImg.sprite = TextureUtils.CreateRoundedRect(100, 100, 18, Color.white);
        cardImg.color  = new Color(0.055f, 0.200f, 0.420f, 1f);
        cardImg.type   = Image.Type.Sliced;

        // "GAME OVER" title
        var titleGO = NewUIGO("Title", cardGO.transform);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot     = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -40f);
        titleRT.sizeDelta        = new Vector2(500f, 90f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(titleTMP, "GAME OVER", 62f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);

        // Score summary
        var fGO = NewUIGO("FinalScore", cardGO.transform);
        var fRT = fGO.GetComponent<RectTransform>();
        fRT.anchorMin = fRT.anchorMax = new Vector2(0.5f, 0.5f);
        fRT.pivot     = new Vector2(0.5f, 0.5f);
        fRT.anchoredPosition = new Vector2(0f, 20f);
        fRT.sizeDelta        = new Vector2(480f, 100f);
        _gameOverScoreTMP = fGO.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(_gameOverScoreTMP, "SCORE   0\nBEST   0", 36f,
            new Color(0.8f, 0.9f, 1f), FontStyles.Normal, TextAlignmentOptions.Center);

        // Restart button
        var btnGO = NewUIGO("RestartBtn", cardGO.transform);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot     = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0f, 40f);
        btnRT.sizeDelta        = new Vector2(280f, 72f);

        var btnImg    = btnGO.AddComponent<Image>();
        btnImg.sprite = TextureUtils.CreateRoundedRect(100, 100, 22, Color.white);
        btnImg.color  = new Color(0.18f, 0.72f, 0.30f);
        btnImg.type   = Image.Type.Sliced;

        var btn    = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var cc = btn.colors;
        cc.highlightedColor = new Color(0.26f, 0.86f, 0.40f);
        cc.pressedColor     = new Color(0.10f, 0.52f, 0.20f);
        btn.colors = cc;
        btn.onClick.AddListener(() => GameManager.Instance.RestartGame());

        var btnTxtGO = NewUIGO("BtnText", btnGO.transform);
        var btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;
        var btnTMP = btnTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(btnTMP, "PLAY AGAIN", 36f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);

        _gameOverPanel.SetActive(false);
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static GameObject NewUIGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void AnchorStretchTop(RectTransform rt, float y, float height)
    {
        rt.anchorMin       = new Vector2(0f, 1f);
        rt.anchorMax       = new Vector2(1f, 1f);
        rt.pivot           = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta       = new Vector2(0f, height);
    }

    private static void ApplyTextStyle(TMP_Text t, string text, float size, Color color,
                                       FontStyles style, TextAlignmentOptions align)
    {
        t.text      = text;
        t.fontSize  = size;
        t.color     = color;
        t.fontStyle = style;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
    }
}

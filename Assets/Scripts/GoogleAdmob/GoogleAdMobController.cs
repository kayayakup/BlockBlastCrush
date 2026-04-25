using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class GoogleAdMobController : MonoBehaviour
{
    public static GoogleAdMobController Instance;

    // Banner & Interstitial
    private BannerView bannerView;
    private InterstitialAd interstitial;

    // Reklamýn yüklenip yüklenmediðini takip ettiðimiz deðiþken
    private bool isBannerLoaded = false;

    // DÝKKAT: Geliþtirme aþamasýnda kendi gerçek ID'lerin yerine Google Test ID'lerini kullanmalýsýn!
    private string bannerID = "ca-app-pub-5398339005079750/7421254312";
    private string interstitialID = "ca-app-pub-5398339005079750/6108172640";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 1. ÇÖZÜM: AdMob'un baþlatýlmasý zaman alýr. Yükleme komutlarýný 
        // SDK'nýn "ben hazýrlandým" dediði bu callback bloðunun içine alýyoruz.
        MobileAds.Initialize(initStatus =>
        {
            LoadBanner();
            LoadInterstitial();
        });
    }

    // -----------------------------------------------------------
    // BANNER
    // -----------------------------------------------------------
    public void LoadBanner()
    {
        // Destroy old banner
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        bannerView = new BannerView(bannerID, AdSize.Banner, AdPosition.Bottom);

        // Banner baþarýyla yüklendiðinde tetiklenir
        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner baþarýyla yüklendi.");
            isBannerLoaded = true;
        };

        // Banner yüklenemediðinde tetiklenir
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner yüklenemedi: " + error.GetMessage());
            isBannerLoaded = false;

            // 2. ÇÖZÜM: Eðer reklam gelmezse sistemi kendi haline býrakmýyoruz.
            // 5 saniye sonra CheckAndShowBanner metodunu çaðýrarak tekrar denemesini saðlýyoruz.
            Invoke(nameof(CheckAndShowBanner), 5f);
        };

        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);
    }

    // Ýstediðin kontrol metodu: Gelmiþ mi kontrol et, gelmediyse yükle/göster
    public void CheckAndShowBanner()
    {
        if (bannerView == null || !isBannerLoaded)
        {
            Debug.Log("Banner henüz gelmemiþ veya null. Yeniden yükleniyor...");
            LoadBanner(); // Yükleme isteði atar, yüklendiðinde otomatik görünür.
        }
        else
        {
            Debug.Log("Banner zaten yüklü, gösteriliyor.");
            bannerView.Show();
        }
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }

    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
            isBannerLoaded = false; // Yýkýldýðýnda durumu sýfýrla
        }
    }

    // -----------------------------------------------------------
    // INTERSTITIAL
    // -----------------------------------------------------------
    public void LoadInterstitial()
    {
        // Destroy old ad
        if (interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }

        InterstitialAd.Load(interstitialID, new AdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    Debug.Log("Interstitial failed to load: " + error.GetMessage());

                    // Interstitial için de garantiye almak istersen 10 saniye sonra tekrar deneyebilirsin:
                    Invoke(nameof(LoadInterstitial), 10f);
                    return;
                }

                interstitial = ad;
            });
    }

    public void ShowInterstitialAd()
    {
        if (interstitial != null && interstitial.CanShowAd())
        {
            interstitial.Show();
            interstitial = null; // must reload after showing
            LoadInterstitial();
        }
    }
}
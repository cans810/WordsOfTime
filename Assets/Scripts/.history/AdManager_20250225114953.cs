using UnityEngine;
using UnityEngine.Advertisements;
using System;
using UnityEngine.SceneManagement;

public class AdManager : MonoBehaviour
{
    private static AdManager instance;
    public static AdManager Instance => instance;

    private InterstitialAdExample interstitialAd;
    private BannerAdExample bannerAd;
    private const int REWARDED_AD_COOLDOWN = 300; // Assuming a default cooldown period of 5 minutes

    private long lastAdTime;
    private bool isBannerShowing = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Get the existing InterstitialAdExample component
            interstitialAd = GetComponent<InterstitialAdExample>();
            if (interstitialAd == null)
            {
                Debug.LogError("InterstitialAdExample component not found on the same GameObject!");
            }
            else
            {
                interstitialAd.Initialize();
                interstitialAd.LoadAd();
            }

            // Get or add the BannerAdExample component
            bannerAd = GetComponent<BannerAdExample>();
            if (bannerAd == null)
            {
                bannerAd = gameObject.AddComponent<BannerAdExample>();
            }

            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene" && !SaveManager.Instance.Data.noAdsBought)
        {
            ShowBanner();
        }
        else
        {
            HideBanner();
        }
    }

    public void ShowBanner()
    {
        if (!SaveManager.Instance.Data.noAdsBought && !isBannerShowing && bannerAd != null)
        {
            bannerAd.LoadBanner();
            isBannerShowing = true;
        }
    }

    public void HideBanner()
    {
        if (isBannerShowing && bannerAd != null)
        {
            bannerAd.HideBannerAd();
            isBannerShowing = false;
        }
    }

    public void ShowInterstitialAd()
    {
        if (SaveManager.Instance.Data.noAdsBought)
        {
            Debug.Log("No Ads purchased - skipping ad");
            return;
        }
        
        if (interstitialAd != null)
        {
            Debug.Log("AdManager: Delegating to InterstitialAdExample to show ad");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogError("InterstitialAdExample component is null!");
        }
    }

    public bool IsRewardedAdReady
    {
        get
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return currentTime - lastAdTime >= REWARDED_AD_COOLDOWN;
        }
    }

    public void OnRewardedAdWatched()
    {
        lastAdTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveManager.Instance.Data.lastRewardedAdTimestamp = lastAdTime;
        SaveManager.Instance.SaveGame();
    }

    public void UnloadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            Debug.Log("Unloading interstitial ad");
            // Implement any necessary cleanup for the ad
            interstitialAd.UnloadAd();
        }
    }

    public void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            Debug.Log("Loading interstitial ad");
            interstitialAd.LoadAd();
        }
    }
}
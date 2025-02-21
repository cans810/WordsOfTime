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
    private const int WORDS_BETWEEN_ADS = 3;  // Show ad every 3 words guessed
    private const int REWARDED_AD_COOLDOWN = 300; // Assuming a default cooldown period of 5 minutes

    private long lastAdTime;
    private bool isBannerShowing = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Only initialize ads if no-ads hasn't been purchased
            if (!SaveManager.Instance.Data.hasRemovedAds)
            {
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
        // Only show banner if ads aren't removed
        if (!SaveManager.Instance.Data.hasRemovedAds && scene.name == "GameScene")
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
        // Check for no-ads purchase before showing banner
        if (!SaveManager.Instance.Data.hasRemovedAds && !isBannerShowing && bannerAd != null)
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
        // Check for no-ads purchase before showing interstitial
        if (!SaveManager.Instance.Data.hasRemovedAds && interstitialAd != null && interstitialAd.IsAdLoaded())
        {
            interstitialAd.ShowAd();
        }
        else if (!SaveManager.Instance.Data.hasRemovedAds)
        {
            interstitialAd?.LoadAd();
        }
    }

    private void ShowAd()
    {
        // Check for no-ads purchase before showing ad
        if (!SaveManager.Instance.Data.hasRemovedAds)
        {
            Debug.Log("Showing ad through ShowAd method");
            interstitialAd.ShowAd();
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

    // Add method to handle when ads are removed through purchase
    public void OnAdsRemoved()
    {
        HideBanner();
        if (SceneManager.sceneLoaded != null)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        // Clean up ad components if they exist
        if (bannerAd != null)
        {
            Destroy(bannerAd);
            bannerAd = null;
        }
        if (interstitialAd != null)
        {
            Destroy(interstitialAd);
            interstitialAd = null;
        }
    }
}
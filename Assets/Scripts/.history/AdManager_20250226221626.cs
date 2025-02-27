using UnityEngine;
using UnityEngine.Advertisements;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

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
        // Don't show banner ads if ads are disabled
        if (scene.name == "GameScene" && !AreAdsDisabled())
        {
            ShowBanner();
        }
        else
        {
            HideBanner();
        }
    }

    // Helper method to check if ads are disabled
    private bool AreAdsDisabled()
    {
        return GameManager.Instance != null && GameManager.Instance.NoAdsBought;
    }

    public void ShowBanner()
    {
        // Don't show ads if they've been disabled through purchase
        if (AreAdsDisabled())
        {
            HideBanner();
            return;
        }

        if (!isBannerShowing && bannerAd != null)
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
        // Skip if no ads purchased
        if (AreAdsDisabled())
        {
            Debug.Log("AdManager: No ads purchased, skipping interstitial ad");
            return;
        }
        
        // Check if interstitial ad component exists
        if (interstitialAd == null)
        {
            Debug.LogError("AdManager: InterstitialAdExample component is null");
            return;
        }
        
        // Check if ad is loaded
        if (interstitialAd.IsAdLoaded())
        {
            Debug.Log("AdManager: Showing interstitial ad");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("AdManager: Ad not loaded, loading and attempting to show");
            
            // Initialize if needed
            if (!Advertisement.isInitialized)
            {
                Debug.Log("AdManager: Unity Ads not initialized, initializing...");
                interstitialAd.Initialize();
            }
            
            // Load the ad
            interstitialAd.LoadAd();
            
            // Try to show after a short delay if it loads quickly
            StartCoroutine(TryShowAdAfterDelay());
        }
    }
    
    private System.Collections.IEnumerator TryShowAdAfterDelay()
    {
        // Wait a short time for the ad to load
        yield return new WaitForSeconds(1.5f);
        
        // Try to show the ad if it's loaded
        if (interstitialAd != null && interstitialAd.IsAdLoaded())
        {
            Debug.Log("AdManager: Ad loaded after delay, showing now");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("AdManager: Ad still not loaded after delay");
        }
    }

    private void ShowAd()
    {
        if (AreAdsDisabled())
        {
            Debug.Log("AdManager: No ads purchased, not showing ad");
            return;
        }

        Debug.Log("Showing ad through ShowAd method");
        interstitialAd.ShowAd();
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
}
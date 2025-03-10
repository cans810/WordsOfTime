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
    private RewardedAdExample rewardedAd;
    
    // Increased the number of words between ads to reduce ad frequency
    private const int WORDS_BETWEEN_ADS = 5;  // Show ad every 5 words guessed (was 3)
    private const int REWARDED_AD_COOLDOWN = 300; // 5 minutes cooldown period
    
    // Track last ad shown time
    private float lastInterstitialAdTime;
    private float minTimeBetweenInterstitialAds = 180f; // 3 minutes between interstitial ads

    private long lastAdTime;
    private bool isBannerShowing = false;
    
    // For child-directed treatment
    private bool isChildDirected = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Set child-directed treatment flag for COPPA compliance
            SetChildDirectedTreatment(isChildDirected);
            
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
            
            // Get or add the RewardedAdExample component
            rewardedAd = GetComponent<RewardedAdExample>();
            if (rewardedAd == null && gameObject.GetComponent<RewardedAdExample>() == null)
            {
                rewardedAd = gameObject.AddComponent<RewardedAdExample>();
            }
            else
            {
                rewardedAd = GetComponent<RewardedAdExample>();
            }

            // Initialize last ad time
            lastInterstitialAdTime = Time.time;
            
            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Set child-directed treatment for COPPA compliance
    private void SetChildDirectedTreatment(bool childDirected)
    {
        // Unity Ads doesn't directly expose child-directed settings through the SDK
        // This is usually configured in the Unity Ads dashboard
        // You might need to check Unity Ads documentation for the latest approach
        Debug.Log("Setting child-directed treatment: " + childDirected);
        
        // For some ad networks, you might need additional configuration here
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
        // Skip if no ads purchased
        if (SaveManager.Instance.Data.noAdsBought)
        {
            Debug.Log("AdManager: No ads purchased, skipping interstitial ad");
            return;
        }
        
        // Check if enough time has passed since the last interstitial
        if (Time.time - lastInterstitialAdTime < minTimeBetweenInterstitialAds)
        {
            Debug.Log("AdManager: Not enough time has passed since last interstitial ad");
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
            lastInterstitialAdTime = Time.time;
        }
        else
        {
            Debug.Log("AdManager: Ad not loaded");
            
            // Initialize if needed
            if (!Advertisement.isInitialized)
            {
                Debug.Log("AdManager: Unity Ads not initialized, initializing...");
                interstitialAd.Initialize();
            }
            
            // Just load the ad for next time, but don't show it now
            interstitialAd.LoadAd();
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
}
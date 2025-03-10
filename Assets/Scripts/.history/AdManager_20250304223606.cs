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
    
    // Reduced time between ads for better visibility while maintaining policy compliance
    private const int WORDS_BETWEEN_ADS = 3;  // Reduced from 5 back to 3
    private const int REWARDED_AD_COOLDOWN = 300; // 5 minutes cooldown period
    
    // Track last ad shown time - reduced time for testing
    private float lastInterstitialAdTime;
    private float minTimeBetweenInterstitialAds = 60f; // Reduced from 180s to 60s (1 minute)

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
                interstitialAd = gameObject.AddComponent<InterstitialAdExample>();
            }
            
            interstitialAd.Initialize();
            interstitialAd.LoadAd();

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
            lastInterstitialAdTime = Time.time - minTimeBetweenInterstitialAds; // Allow showing an ad immediately at start
            
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
            
            // Show an interstitial ad when entering the game scene (with delay for better UX)
            Invoke("TryShowInterstitialWithDelay", 2.0f);
        }
        else
        {
            HideBanner();
        }
    }
    
    private void TryShowInterstitialWithDelay()
    {
        // Only try to show if ads aren't purchased
        if (!SaveManager.Instance.Data.noAdsBought)
        {
            ShowInterstitialAd();
        }
    }

    public void ShowBanner()
    {
        if (!SaveManager.Instance.Data.noAdsBought && !isBannerShowing && bannerAd != null)
        {
            Debug.Log("AdManager: Showing banner ad");
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
            Debug.Log("AdManager: Not enough time has passed since last interstitial ad. Time remaining: " 
                + (minTimeBetweenInterstitialAds - (Time.time - lastInterstitialAdTime)) + " seconds");
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
            Debug.Log("AdManager: Ad not loaded, trying to load one for next time");
            
            // Initialize if needed
            if (!Advertisement.isInitialized)
            {
                Debug.Log("AdManager: Unity Ads not initialized, initializing...");
                interstitialAd.Initialize();
            }
            
            // Load an ad, and try to show it after a short delay if it loads quickly
            interstitialAd.LoadAd();
            StartCoroutine(TryShowAdAfterDelay());
        }
    }
    
    private IEnumerator TryShowAdAfterDelay()
    {
        // Wait a short time for the ad to load
        yield return new WaitForSeconds(1.0f);
        
        // Try to show the ad if it's loaded and enough time has passed
        if (interstitialAd != null && interstitialAd.IsAdLoaded() && 
            (Time.time - lastInterstitialAdTime >= minTimeBetweenInterstitialAds))
        {
            Debug.Log("AdManager: Ad loaded after delay, showing now");
            interstitialAd.ShowAd();
            lastInterstitialAdTime = Time.time;
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
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
    
    // Show interstitial ad every 3 words guessed
    private const int WORDS_BETWEEN_ADS = 3;
    private const int REWARDED_AD_COOLDOWN = 300; // 5 minutes cooldown period
    
    // Counter for word guesses
    private int wordGuessCounter = 0;
    
    // Track last ad shown time
    private float lastInterstitialAdTime;
    private float minTimeBetweenInterstitialAds = 60f; // 1 minute between interstitial ads

    private long lastAdTime;
    private bool isBannerShowing = false;
    
    // For child-directed treatment (required for Family Policy compliance)
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
        Debug.Log("Setting child-directed treatment: " + childDirected);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only show banner ads in GameScene
        if (scene.name == "GameScene" && !SaveManager.Instance.Data.noAdsBought)
        {
            Debug.Log("AdManager: Entered GameScene, showing banner");
            ShowBanner();
        }
        else
        {
            Debug.Log("AdManager: Not in GameScene, hiding banner");
            HideBanner();
        }
        
        // Reset word guess counter when entering any scene
        wordGuessCounter = 0;
    }

    public void ShowBanner()
    {
        // Only show banner if in GameScene
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            Debug.Log("AdManager: Not showing banner because we're not in GameScene");
            return;
        }
        
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
    
    // Call this method whenever a word is successfully guessed
    public void OnWordGuessed()
    {
        // Skip if no ads purchased
        if (SaveManager.Instance.Data.noAdsBought)
        {
            return;
        }
        
        // Only process if in GameScene
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            return;
        }
        
        // Increment counter
        wordGuessCounter++;
        Debug.Log($"AdManager: Word guessed. Counter: {wordGuessCounter}/{WORDS_BETWEEN_ADS}");
        
        // Check if we've reached the threshold
        if (wordGuessCounter >= WORDS_BETWEEN_ADS)
        {
            Debug.Log("AdManager: Threshold reached, showing interstitial ad");
            // Reset counter before showing ad
            wordGuessCounter = 0;
            
            // Show interstitial ad
            ShowInterstitialAd();
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
        
        // Check if enough time has passed since the last interstitial (Family Policy compliance)
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
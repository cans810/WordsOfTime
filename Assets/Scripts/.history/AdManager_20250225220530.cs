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
    private const int REWARDED_AD_COOLDOWN = 300; // Assuming a default cooldown period of 5 minutes

    private long lastAdTime;
    private bool isBannerShowing = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            try
            {
                Debug.Log("AdManager initializing...");
                
                // Get the existing InterstitialAdExample component
                interstitialAd = GetComponent<InterstitialAdExample>();
                if (interstitialAd == null)
                {
                    Debug.LogWarning("InterstitialAdExample component not found, adding one");
                    interstitialAd = gameObject.AddComponent<InterstitialAdExample>();
                }
                
                // Initialize Unity Ads through the InterstitialAdExample component
                // This will handle loading the first ad after initialization is complete
                interstitialAd.Initialize();
                
                // Get or add the BannerAdExample component
                bannerAd = GetComponent<BannerAdExample>();
                if (bannerAd == null)
                {
                    bannerAd = gameObject.AddComponent<BannerAdExample>();
                }

                // Subscribe to scene loading events
                SceneManager.sceneLoaded += OnSceneLoaded;
                
                Debug.Log("AdManager initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error initializing AdManager: {e.Message}");
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
        Debug.Log("AdManager.ShowInterstitialAd called");
        
        if (SaveManager.Instance.Data.noAdsBought)
        {
            Debug.Log("No Ads purchased - skipping ad");
            return;
        }
        
        // Check if Unity Ads is initialized
        if (!Advertisement.isInitialized)
        {
            Debug.LogError("Unity Ads is not initialized! Cannot show interstitial ad.");
            
            // Try to initialize again
            if (interstitialAd != null)
            {
                Debug.Log("Attempting to re-initialize Unity Ads...");
                interstitialAd.Initialize();
            }
            return;
        }
        
        if (interstitialAd != null)
        {
            // Check if ad is loaded before showing
            if (interstitialAd.IsAdLoaded())
            {
                Debug.Log("AdManager: Ad is loaded, delegating to InterstitialAdExample to show ad");
                interstitialAd.ShowAd();
            }
            else
            {
                Debug.LogWarning("Ad not loaded yet, loading a new ad");
                interstitialAd.LoadAd();
                
                // Try to show the ad after a short delay to give it time to load
                StartCoroutine(ShowAdAfterDelay(1.0f));
            }
        }
        else
        {
            Debug.LogError("InterstitialAdExample component is null!");
        }
    }
    
    private IEnumerator ShowAdAfterDelay(float delay)
    {
        Debug.Log($"Waiting {delay} seconds before trying to show ad again");
        yield return new WaitForSeconds(delay);
        
        if (interstitialAd != null && interstitialAd.IsAdLoaded())
        {
            Debug.Log("Ad loaded after delay, showing now");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("Ad still not loaded after delay");
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

    public void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            Debug.Log("Loading interstitial ad");
            interstitialAd.LoadAd();
        }
    }

    private System.Collections.IEnumerator LoadAdWithDelay(float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        
        if (interstitialAd != null)
        {
            Debug.Log("Loading initial interstitial ad after delay");
            interstitialAd.LoadAd();
        }
    }

    // Add a public method to check if Unity Ads is initialized
    public bool IsUnityAdsInitialized()
    {
        bool isInitialized = Advertisement.isInitialized;
        Debug.Log($"Unity Ads initialized status: {isInitialized}");
        return isInitialized;
    }
}
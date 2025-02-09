using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class AdManager : MonoBehaviour
{
    private static AdManager instance;
    public static AdManager Instance => instance;

    private InterstitialAdExample interstitialAd;
    private const int WORDS_BETWEEN_ADS = 3;  // Show ad every 3 words guessed
    private const int REWARDED_AD_COOLDOWN = 300; // Assuming a default cooldown period of 5 minutes

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
                interstitialAd.LoadAd(); // Load the first ad
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowInterstitialAd()
    {
        Debug.Log("AdManager: Showing interstitial ad");
        if (interstitialAd != null && interstitialAd.IsAdLoaded())
        {
            Debug.Log("AdManager: Interstitial ad is loaded, showing now");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("AdManager: Interstitial ad is not ready or component is null!");
            // Try to load a new ad
            interstitialAd?.LoadAd();
        }
    }

    private void ShowAd()
    {
        Debug.Log("Showing ad through ShowAd method");
        interstitialAd.ShowAd();
    }

    public bool IsRewardedAdReady
    {
        get
        {
            // Check if the cooldown period has passed
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long lastAdTime = SaveManager.Instance.Data.lastRewardedAdTimestamp;
            return currentTime - lastAdTime >= REWARDED_AD_COOLDOWN;
        }
    }

    public void OnRewardedAdWatched()
    {
        // Update the last watched timestamp
        SaveManager.Instance.Data.lastRewardedAdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveManager.Instance.SaveGame();
        
        // existing reward logic...
    }
}
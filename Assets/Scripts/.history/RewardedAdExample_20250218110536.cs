using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class RewardedAdExample : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] string _iOSAdUnitId = "Rewarded_iOS";
    string _adUnitId = null;
    private bool isAdLoaded = false;

    void Awake()
    {
        // Get the Ad Unit ID for the current platform:
        _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOSAdUnitId
            : _androidAdUnitId;

        // Load the ad immediately on awake
        LoadAd();
    }

    public void LoadAd()
    {
        Debug.Log("Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void ShowAd(Action onRewardGranted)
    {
        if (isAdLoaded)
        {
            OnRewardGranted = onRewardGranted;
            Advertisement.Show(_adUnitId, this);
            isAdLoaded = false; // Reset the loaded state
        }
        else
        {
            Debug.Log("Rewarded ad not ready, loading now...");
            LoadAd();
            // Optionally, show a message to the user that the ad is loading
        }
    }

    private Action OnRewardGranted;

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("Ad Loaded: " + adUnitId);
        isAdLoaded = true;

        // Notify MainMenuManager that the ad is ready
        MainMenuManager mainMenuManager = FindObjectOfType<MainMenuManager>();
        if (mainMenuManager != null)
        {
            mainMenuManager.OnAdReady();
        }
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            Debug.Log("Unity Ads Rewarded Ad Completed");
            OnRewardGranted?.Invoke();
            
            // Save the current time
            string currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            PlayerPrefs.SetString("LastAdWatchTime", currentTime);
            PlayerPrefs.Save();
        }
        // Reload the ad after showing
        LoadAd();
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Failed to load ad: {message}");
        // Retry loading after a delay
        Invoke("LoadAd", 2f); // Retry after 2 seconds
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Failed to show ad: {message}");
        // Reload the ad after failure
        LoadAd();
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }
} 
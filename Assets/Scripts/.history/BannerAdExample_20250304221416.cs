using UnityEngine;
using UnityEngine.Advertisements;

public class BannerAdExample : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    // Using a less intrusive position for family apps
    [SerializeField] BannerPosition _bannerPosition = BannerPosition.BOTTOM_CENTER;
    [SerializeField] string _androidAdUnitId = "Banner_Android";
    [SerializeField] string _iOSAdUnitId = "Banner_iOS";
    string _adUnitId = null;
    private bool isLoading = false;

    void Awake()
    {
        // Get the Ad Unit ID for the current platform:
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID
        _adUnitId = _androidAdUnitId;
#endif

        // Set the banner position immediately when the script initializes
        Advertisement.Banner.SetPosition(_bannerPosition);
    }

    public void LoadBanner()
    {
        // Prevent multiple simultaneous load attempts
        if (isLoading) return;
        
        isLoading = true;
        
        // Set up options to notify the SDK of load events:
        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        // Load the Ad Unit with banner content:
        Advertisement.Banner.Load(_adUnitId, options);
    }

    void OnBannerLoaded()
    {
        Debug.Log("Banner loaded");
        isLoading = false;
        ShowBannerAd();
    }

    void OnBannerError(string message)
    {
        Debug.Log($"Banner Error: {message}");
        isLoading = false;
        
        // Retry after a delay rather than immediately to avoid hammering the ad network
        Invoke("LoadBanner", 60.0f);
    }

    public void ShowBannerAd()
    {
        // Set up options to notify the SDK of show events:
        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        };

        // Show the loaded Banner Ad Unit:
        Advertisement.Banner.Show(_adUnitId, options);
    }

    public void HideBannerAd()
    {
        // Hide the banner:
        Advertisement.Banner.Hide();
    }

    void OnBannerClicked() 
    { 
        Debug.Log("Banner clicked");
    }
    
    void OnBannerShown() 
    { 
        Debug.Log("Banner shown");
    }
    
    void OnBannerHidden() 
    { 
        Debug.Log("Banner hidden");
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"Banner Ad Loaded: {placementId}");
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Error loading Banner Ad Unit: {placementId} - {error.ToString()} - {message}");
        isLoading = false;
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.Log($"Error showing Banner Ad Unit {placementId}: {error.ToString()} - {message}");
        isLoading = false;
    }

    public void OnUnityAdsShowStart(string placementId) { }
    public void OnUnityAdsShowClick(string placementId) { }
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState) { }
} 
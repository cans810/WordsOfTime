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
    
    void Start()
    {
        // Try to load a banner on start
        if (!isLoading)
        {
            LoadBanner();
        }
    }

    public void LoadBanner()
    {
        // Prevent multiple simultaneous load attempts
        if (isLoading)
        {
            Debug.Log("Already loading banner ad, skipping duplicate request");
            return;
        }
        
        if (string.IsNullOrEmpty(_adUnitId))
        {
            Debug.LogError("Banner ad unit ID is null or empty!");
#if UNITY_ANDROID
            _adUnitId = "Banner_Android"; // Fallback value
#else
            _adUnitId = "Banner_iOS"; // Fallback value
#endif
        }
        
        Debug.Log("Loading banner ad with ID: " + _adUnitId);
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
        Debug.Log("Banner loaded successfully");
        isLoading = false;
        ShowBannerAd();
    }

    void OnBannerError(string message)
    {
        Debug.LogError($"Banner Error: {message}");
        isLoading = false;
        
        // Retry after a delay rather than immediately to avoid hammering the ad network
        Invoke("LoadBanner", 30.0f);
    }

    public void ShowBannerAd()
    {
        Debug.Log("Showing banner ad");
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
        Debug.Log("Hiding banner ad");
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
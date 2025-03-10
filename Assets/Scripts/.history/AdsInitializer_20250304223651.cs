using UnityEngine;
using UnityEngine.Advertisements;
 
public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] string _androidGameId = "96d7b277-044d-4abd-9e30-b74bb188c564";
    [SerializeField] string _iOSGameId = "96d7b277-044d-4abd-9e30-b74bb188c564";
#if UNITY_EDITOR
    [SerializeField] bool _testMode = true; // Keep test mode true in editor
#else
    [SerializeField] bool _testMode = false; // Set to false for production builds
#endif
    private string _gameId;
    
    // Flag to indicate if this app is for children
    [SerializeField] bool _childDirected = true;
    
    private static bool _initialized = false;
 
    void Awake()
    {
        InitializeAds();
    }
 
    public void InitializeAds()
    {
        // Skip if already initialized
        if (_initialized) return;
        
    #if UNITY_IOS
            _gameId = _iOSGameId;
    #elif UNITY_ANDROID
            _gameId = _androidGameId;
    #elif UNITY_EDITOR
            _gameId = _androidGameId; //Only for testing the functionality in the Editor
            _testMode = true; // Force test mode in editor
    #endif
    
        Debug.Log("Initializing Unity Ads with game ID: " + _gameId + ", Test Mode: " + _testMode);
        
        // Only initialize if not already initialized
        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            // Initialize with appropriate mode
            Advertisement.Initialize(_gameId, _testMode, this);
            
            // Unity Ads doesn't have direct API for child-directed treatment
            // You must ensure appropriate ad content through the Unity Dashboard settings
            // and by using appropriate ad formats
            Debug.Log("Initializing ads with child-directed settings: " + _childDirected);
        }
        else if (Advertisement.isInitialized)
        {
            Debug.Log("Unity Ads already initialized");
            _initialized = true;
        }
    }
 
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
        _initialized = true;
        
        // Try to load ads once initialized
        BannerAdExample banner = GetComponent<BannerAdExample>();
        if (banner != null)
        {
            banner.LoadBanner();
        }
        
        InterstitialAdExample interstitial = GetComponent<InterstitialAdExample>();
        if (interstitial != null)
        {
            interstitial.LoadAd();
        }
    }
 
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
        // Try again after a delay
        Invoke("InitializeAds", 5.0f);
    }
    
    // Public method to check if ads are initialized
    public static bool AreAdsInitialized()
    {
        return _initialized || Advertisement.isInitialized;
    }
}
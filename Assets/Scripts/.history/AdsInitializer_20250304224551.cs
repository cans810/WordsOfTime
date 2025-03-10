using UnityEngine;
using UnityEngine.Advertisements;
 
public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] string _androidGameId = "96d7b277-044d-4abd-9e30-b74bb188c564";
    [SerializeField] string _iOSGameId = "96d7b277-044d-4abd-9e30-b74bb188c564";
    [SerializeField] bool _testMode = false; // Set to false for production builds
    private string _gameId;
    
    // Flag to indicate if this app is for children
    [SerializeField] bool _childDirected = true;
    
    // Component reference for rewarded ads
    private RewardedAdExample rewardedAdExample;
 
    void Awake()
    {
        InitializeAds();
    }
 
    public void InitializeAds()
    {
    #if UNITY_IOS
            _gameId = _iOSGameId;
    #elif UNITY_ANDROID
            _gameId = _androidGameId;
    #elif UNITY_EDITOR
            _gameId = _androidGameId; //Only for testing the functionality in the Editor
            _testMode = true; // Force test mode in editor
    #endif
    
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
    }
 
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
        
        // Get or add RewardedAdExample component
        rewardedAdExample = GetComponent<RewardedAdExample>();
        if (rewardedAdExample == null)
        {
            rewardedAdExample = gameObject.AddComponent<RewardedAdExample>();
        }
        
        // Pre-load a rewarded ad
        if (rewardedAdExample != null)
        {
            rewardedAdExample.LoadAd();
        }
    }
 
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }
}
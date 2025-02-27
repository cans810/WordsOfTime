using UnityEngine;
using UnityEngine.Advertisements;
 
public class InterstitialAdExample : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnitId = "Interstitial_Android";
    [SerializeField] string _iOsAdUnitId = "Interstitial_iOS";
    private string adUnitId;
    private bool isAdLoaded = false;
 
    void Awake()
    {
        // Get the Ad Unit ID for the current platform:
        adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOsAdUnitId
            : _androidAdUnitId;
    }
 
    public void Initialize()
    {
        Debug.Log("Initializing interstitial ads");
        if (!Advertisement.isInitialized)
        {
            try
            {
                Debug.Log("Unity Ads not initialized, initializing now");
                Advertisement.Initialize("96d7b277-044d-4abd-9e30-b74bb188c564", true); // Replace with your game ID
                
                // Load an ad after initialization
                LoadAd();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error initializing Unity Ads: {e.Message}");
            }
        }
        else
        {
            Debug.Log("Unity Ads already initialized");
            
            // Make sure we have an ad loaded
            if (!isAdLoaded)
            {
                LoadAd();
            }
        }
    }
 
    // Load content to the Ad Unit:
    public void LoadAd()
    {
        Debug.Log("Loading interstitial ad");
        Advertisement.Load(adUnitId, this);
    }
 
    // Show the loaded content in the Ad Unit:
    public void ShowAd()
    {
        Debug.Log("Attempting to show interstitial ad");
        if (isAdLoaded)
        {
            try
            {
                Debug.Log("InterstitialAdExample: Ad is loaded, showing now");
                Advertisement.Show(adUnitId, this);
                isAdLoaded = false; // Reset the loaded state
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing ad: {e.Message}");
                isAdLoaded = false; // Reset in case of error
                LoadAd(); // Try to load a new ad
            }
        }
        else
        {
            Debug.LogWarning("Interstitial ad not ready - loading new ad");
            
            // Make sure ads are initialized
            if (!Advertisement.isInitialized)
            {
                Debug.Log("Ads not initialized, initializing now");
                Initialize();
            }
            
            // Load a new ad
            LoadAd();
        }
    }
 
    // Implement Load Listener and Show Listener interface methods: 
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"Interstitial ad loaded: {placementId}");
        isAdLoaded = true;
    }
 
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Failed to load interstitial ad: {placementId}, Error: {error}, Message: {message}");
        isAdLoaded = false;
        LoadAd(); // Try to load another ad
    }
 
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Failed to show interstitial ad: {placementId}, Error: {error}, Message: {message}");
        LoadAd(); // Try to load another ad
    }
 
    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log($"Interstitial ad started: {placementId}");
    }
 
    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log($"Interstitial ad clicked: {placementId}");
    }
 
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"Interstitial ad completed: {placementId}, completion state: {showCompletionState}");
        isAdLoaded = false; // Reset the loaded state
        LoadAd(); // Load the next ad
        
        // Log the completion for debugging
        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("User watched the entire ad");
        }
        else if (showCompletionState == UnityAdsShowCompletionState.SKIPPED)
        {
            Debug.Log("User skipped the ad");
        }
        else
        {
            Debug.Log("Ad may have been interrupted");
        }
    }

    public bool IsAdLoaded()
    {
        return isAdLoaded;
    }
}
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
            Advertisement.Initialize("96d7b277-044d-4abd-9e30-b74bb188c564", true); // Replace with your game ID
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
            Advertisement.Show(adUnitId, this);
            isAdLoaded = false; // Reset the loaded state
        }
        else
        {
            Debug.LogWarning("Interstitial ad not ready - loading new ad");
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
        Debug.Log($"Interstitial ad completed: {placementId}");
        LoadAd(); // Load the next ad
    }

    public bool IsAdLoaded()
    {
        return isAdLoaded;
    }

    public void UnloadAd()
    {
        Debug.Log("Unloading interstitial ad");
        isAdLoaded = false;
        // Add any platform-specific ad unloading logic here
    }
}
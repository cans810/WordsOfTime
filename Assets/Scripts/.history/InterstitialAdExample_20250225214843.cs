using UnityEngine;
using UnityEngine.Advertisements;
 
public class InterstitialAdExample : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener, IUnityAdsInitializationListener
{
    [SerializeField] string _androidAdUnitId = "Interstitial_Android";
    [SerializeField] string _iOsAdUnitId = "Interstitial_iOS";
    private string adUnitId;
    private bool isAdLoaded = false;
    private string gameId = "96d7b277-044d-4abd-9e30-b74bb188c564";
    private bool testMode = true;
 
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
            Debug.Log("Unity Ads not initialized, initializing now with listener...");
            Advertisement.Initialize(gameId, testMode, this);
            Debug.Log("Unity Ads initialization requested with proper listener");
        }
        else
        {
            Debug.Log("Unity Ads already initialized");
            // Only load ad if already initialized
            LoadAd();
        }
    }
    
    // Implement initialization listener interface methods
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete!");
        // Now it's safe to load an ad
        LoadAd();
    }
    
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Unity Ads initialization failed: {error.ToString()} - {message}");
    }
 
    // Load content to the Ad Unit:
    public void LoadAd()
    {
        Debug.Log("Loading interstitial ad");
        
        // Check if Unity Ads is initialized
        if (!Advertisement.isInitialized)
        {
            Debug.LogError("Attempted to load ad but Unity Ads is not initialized!");
            // Don't try to initialize here, as it would create a loop
            return;
        }
        
        // Check if we're in test mode
        Debug.Log($"Unity Ads test mode: {Advertisement.debugMode}");
        
        // Log the ad unit ID being used
        Debug.Log($"Loading ad with unit ID: {adUnitId}");
        
        Advertisement.Load(adUnitId, this);
    }
 
    // Show the loaded content in the Ad Unit:
    public void ShowAd()
    {
        Debug.Log("ShowAd method called in InterstitialAdExample");
        
        if (GameManager.Instance.NoAdsBought)
        {
            Debug.Log("No Ads purchased - skipping ad");
            return;
        }

        // Note: Don't call AdManager.ShowInterstitialAd() here as it creates an infinite loop
        Debug.Log("Showing interstitial ad...");
        
        // Check if Unity Ads is initialized
        if (!Advertisement.isInitialized)
        {
            Debug.LogError("Attempted to show ad but Unity Ads is not initialized!");
            return;
        }
        
        // Check if ad is loaded before showing
        if (isAdLoaded)
        {
            Debug.Log($"Ad is loaded, showing ad unit: {adUnitId}");
            Advertisement.Show(adUnitId, this);
            isAdLoaded = false;  // Mark as not loaded after showing
        }
        else
        {
            Debug.LogWarning("Attempted to show ad before it was loaded. Loading a new ad instead.");
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
        
        // Notify WordGameManager that ad is completed if it exists
        if (WordGameManager.Instance != null)
        {
            Debug.Log($"WordGameManager instance found, current wordsGuessedCount: {WordGameManager.Instance.wordsGuessedCount}");
            
            // Only consider the ad truly completed if it was shown to completion
            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log("Ad was shown to completion, notifying WordGameManager");
                WordGameManager.Instance.OnAdCompleted();
                Debug.Log($"After OnAdCompleted, wordsGuessedCount: {WordGameManager.Instance.wordsGuessedCount}");
            }
            else
            {
                Debug.Log($"Ad was not shown to completion (state: {showCompletionState}), not resetting counter");
                // Just reset the isAdShowing flag without resetting the counter
                WordGameManager.Instance.ResetAdShowingFlag();
            }
        }
        else
        {
            Debug.LogError("WordGameManager instance is null in OnUnityAdsShowComplete!");
        }
        
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
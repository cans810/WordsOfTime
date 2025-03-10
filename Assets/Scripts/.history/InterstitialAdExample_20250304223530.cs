using UnityEngine;
using UnityEngine.Advertisements;
using System.Collections;
 
public class InterstitialAdExample : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnitId = "Interstitial_Android";
    [SerializeField] string _iOsAdUnitId = "Interstitial_iOS";
    private string adUnitId;
    private bool isAdLoaded = false;
    private float adCloseTimeout = 5.0f; // 5 seconds timeout for closing ads
    private bool isShowingAd = false;
    private Coroutine adTimeoutCoroutine;
 
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
                #if UNITY_ANDROID
                Advertisement.Initialize("96d7b277-044d-4abd-9e30-b74bb188c564", false); // Disable test mode for production
                #elif UNITY_IOS
                Advertisement.Initialize("96d7b277-044d-4abd-9e30-b74bb188c564", false); // Disable test mode for production
                #else
                Advertisement.Initialize("96d7b277-044d-4abd-9e30-b74bb188c564", true);
                #endif
                
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
        // We'll still check isShowingAd, but remove the isAdLoaded check to make sure we always attempt to load
        if (!isShowingAd)
        {
            Advertisement.Load(adUnitId, this);
        }
        else
        {
            Debug.Log("Not loading interstitial ad because one is currently showing");
        }
    }
 
    // Show the loaded content in the Ad Unit:
    public void ShowAd()
    {
        Debug.Log("Attempting to show interstitial ad");
        if (isAdLoaded && !isShowingAd)
        {
            try
            {
                Debug.Log("InterstitialAdExample: Ad is loaded, showing now");
                isShowingAd = true;
                Advertisement.Show(adUnitId, this);
                isAdLoaded = false; // Reset the loaded state
                
                // Start timeout to ensure ad can be closed
                adTimeoutCoroutine = StartCoroutine(EnsureAdCanBeClosed());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing ad: {e.Message}");
                isAdLoaded = false; // Reset in case of error
                isShowingAd = false;
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
            
            // Load a new ad if not showing
            if (!isShowingAd)
            {
                LoadAd();
            }
        }
    }
    
    // Ensure ad can be closed after timeout
    private IEnumerator EnsureAdCanBeClosed()
    {
        yield return new WaitForSeconds(adCloseTimeout);
        
        // If ad is still showing after timeout, try to close it
        if (isShowingAd)
        {
            Debug.Log("Ad timeout reached - ensuring ad can be closed");
            // Unity Ads should handle this automatically, but we're marking it as no longer showing
            isShowingAd = false;
            
            // Try to load a new ad after timeout
            Invoke("LoadAd", 1.0f);
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
        
        // Reduce retry delay to improve ad availability
        Invoke("LoadAd", 10.0f);
    }
 
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Failed to show interstitial ad: {placementId}, Error: {error}, Message: {message}");
        isShowingAd = false;
        
        // Reduce retry delay to improve ad availability
        Invoke("LoadAd", 10.0f);
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
        isShowingAd = false;
        
        // Cancel the timeout coroutine if it's still running
        if (adTimeoutCoroutine != null)
        {
            StopCoroutine(adTimeoutCoroutine);
            adTimeoutCoroutine = null;
        }
        
        // Reduce delay before loading the next ad
        Invoke("LoadAd", 1.0f);
        
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
        return isAdLoaded && !isShowingAd;
    }
}
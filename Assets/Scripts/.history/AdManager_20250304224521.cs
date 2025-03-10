using UnityEngine;
using UnityEngine.Advertisements;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

public class AdManager : MonoBehaviour
{
    private static AdManager instance;
    public static AdManager Instance => instance;

    private RewardedAdExample rewardedAd;
    
    // Rewarded ad cooldown
    private const int REWARDED_AD_COOLDOWN = 300; // 5 minutes cooldown period
    private long lastAdTime;
    
    // For child-directed treatment
    private bool isChildDirected = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Set child-directed treatment flag for COPPA compliance
            SetChildDirectedTreatment(isChildDirected);
            
            // Get or add the RewardedAdExample component
            rewardedAd = GetComponent<RewardedAdExample>();
            if (rewardedAd == null && gameObject.GetComponent<RewardedAdExample>() == null)
            {
                rewardedAd = gameObject.AddComponent<RewardedAdExample>();
            }
            else
            {
                rewardedAd = GetComponent<RewardedAdExample>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Set child-directed treatment for COPPA compliance
    private void SetChildDirectedTreatment(bool childDirected)
    {
        // Unity Ads doesn't directly expose child-directed settings through the SDK
        // This is usually configured in the Unity Ads dashboard
        Debug.Log("Setting child-directed treatment: " + childDirected);
    }

    public bool IsRewardedAdReady
    {
        get
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return currentTime - lastAdTime >= REWARDED_AD_COOLDOWN;
        }
    }

    public void OnRewardedAdWatched()
    {
        lastAdTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveManager.Instance.Data.lastRewardedAdTimestamp = lastAdTime;
        SaveManager.Instance.SaveGame();
    }
}
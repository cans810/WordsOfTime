using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class MainMenuManager : MonoBehaviour
{
    public Image BackgroundImage;
    public TextMeshProUGUI pointText;
    private SettingsController settingsController;
    private EraSelectionManager eraSelectionManager;
    private MarketManager marketManager;
    public TextMeshProUGUI eraText;

    private string currentLanguage;

    private const float MUSIC_FADE_DURATION = 2.0f; // Duration of fade in seconds
    private bool isFirstStart = true;

    [SerializeField] private Button watchAdButton;
    [SerializeField] private TextMeshProUGUI watchAdCooldownText;
    private RewardedAdExample rewardedAd;
    private const string LAST_AD_TIME_KEY = "LastAdWatchTime";
    private const float REWARDED_AD_COOLDOWN = 7200f; // 2 hours in seconds
    private float remainingCooldown = 0f;
    private bool isCountingDown = false;

    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.5f;

    // Start is called before the first frame update
    private void Start()
    {
        // Check if this is the first start
        isFirstStart = PlayerPrefs.GetInt("HasStartedBefore", 0) == 0;
        
        if (isFirstStart)
        {
            PlayerPrefs.SetInt("HasStartedBefore", 1);
            PlayerPrefs.Save();
        }

        currentLanguage = PlayerPrefs.GetString("Language", "en");
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);

        // Find point text in main menu
        Transform pointPanel = GameObject.Find("PointPanel")?.transform;
        if (pointPanel != null)
        {
            pointText = pointPanel.Find("point")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Find managers without activating them
        settingsController = FindInactiveObjectByType<SettingsController>();
        eraSelectionManager = FindInactiveObjectByType<EraSelectionManager>();
        marketManager = FindInactiveObjectByType<MarketManager>();
        
        UpdatePointsDisplay();
        UpdateEraDisplay();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged += UpdateEraDisplay;
        }

        // Play music with delay
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusicWithDelay();
        }

        // Initialize rewarded ads by finding the existing component
        GameObject adsGameObject = GameObject.Find("Ads");
        if (adsGameObject != null)
        {
            rewardedAd = adsGameObject.GetComponent<RewardedAdExample>();
            if (rewardedAd != null)
            {
                rewardedAd.LoadAd();
            }
            else
            {
                Debug.LogError("RewardedAdExample component not found on Ads GameObject!");
            }
        }
        else
        {
            Debug.LogError("Ads GameObject not found!");
        }

        // Get the last ad time from PlayerPrefs
        long lastAdTime = long.Parse(PlayerPrefs.GetString(LAST_AD_TIME_KEY, "0"));
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Calculate initial remaining time
        remainingCooldown = Mathf.Max(0, REWARDED_AD_COOLDOWN - (currentTime - lastAdTime));
        
        if (remainingCooldown > 0)
        {
            isCountingDown = true;
            UpdateWatchAdCooldown();
        }
    }

    private IEnumerator DelayedMusicStart()
    {
        // Wait for 1 second before starting the fade
        yield return new WaitForSeconds(1f);
        StartCoroutine(FadeMusicIn());
    }

    private IEnumerator FadeMusicIn()
    {
        float elapsedTime = 0;
        float startVolume = 0f;
        float targetVolume = 1f; // Or whatever your default music volume is

        while (elapsedTime < MUSIC_FADE_DURATION)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / MUSIC_FADE_DURATION;
            
            // Use smooth step for more natural fading
            float currentVolume = Mathf.Lerp(startVolume, targetVolume, normalizedTime * normalizedTime * (3f - 2f * normalizedTime));
            
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetMusicVolume(currentVolume);
            }
            
            yield return null;
        }

        // Ensure we end at exactly the target volume
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(targetVolume);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged -= UpdateEraDisplay;
        }
    }

    // Helper method to find inactive objects
    private T FindInactiveObjectByType<T>() where T : MonoBehaviour
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        if (objects.Length > 0)
        {
            return objects[0];
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        if (isCountingDown && remainingCooldown > 0)
        {
            remainingCooldown -= Time.deltaTime;
            UpdateWatchAdCooldown();
        }

        if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
        {
            UpdatePointsDisplay();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdatePointsDisplay()
    {
        if (pointText != null)
        {
            pointText.text = GameManager.Instance.CurrentPoints.ToString();
        }

        UpdateEraDisplay();
    }

    private void UpdateEraDisplay()
    {
        if (eraText != null && GameManager.Instance != null)
        {
            string currentEra = GameManager.Instance.CurrentEra;
            string translationKey = currentEra.ToLower().Replace(" ", "_"); // Convert era name to key format
            string translatedEra = TranslationManager.Instance.GetTranslation(translationKey);
            
            if (string.IsNullOrEmpty(translatedEra))
            {
                eraText.text = "Select Era";
            }
            else
            {
                eraText.text = translatedEra;
            }

            // Update background image
            if (BackgroundImage != null)
            {
                BackgroundImage.sprite = GameManager.Instance.getEraImage(currentEra);
            }
        }
    }

    public void PlayButton(){
        SceneManager.LoadScene("GameScene");
    }

    public void SelectEraButton(){
        if (eraSelectionManager != null)
        {
            eraSelectionManager.ShowEraSelectionScreen();
        }
        else
        {
            Debug.LogError("Era Selection Manager not found! Make sure it exists in the scene.");
        }
    }

    public void SettingsButton()
    {
        if (settingsController != null)
        {
            settingsController.ShowSettings();
        }
        else
        {
            Debug.LogError("Settings Controller not found! Make sure it exists in the scene.");
        }
    }

    public void MarketButton(){
        if (marketManager != null)
        {
            marketManager.ShowMarketScreen();
        }
        else
        {
            Debug.LogError("Era Selection Manager not found! Make sure it exists in the scene.");
        }
    }

    public void OnLanguageChanged(string newLanguage)
    {
        currentLanguage = newLanguage;
        PlayerPrefs.SetString("Language", newLanguage);
        PlayerPrefs.Save();
        UpdateEraDisplay();
    }

    private void UpdateWatchAdCooldown()
    {
        if (remainingCooldown <= 0)
        {
            watchAdButton.interactable = true;
            watchAdCooldownText.text = "Watch Ad";
            isCountingDown = false;
        }
        else
        {
            watchAdButton.interactable = false;
            int hours = Mathf.FloorToInt(remainingCooldown / 3600f);
            int minutes = Mathf.FloorToInt((remainingCooldown % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(remainingCooldown % 60f);
            watchAdCooldownText.text = $"{hours}h {minutes}m {seconds}s";
        }
    }

    public void OnWatchAdButtonClicked()
    {
        if (rewardedAd != null && remainingCooldown <= 0)
        {
            rewardedAd.ShowAd(() => {
                // Reward the player
                GameManager.Instance.AddPoints(150);
                
                // Set new cooldown
                remainingCooldown = REWARDED_AD_COOLDOWN;
                isCountingDown = true;
                
                // Save the current time
                string currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                PlayerPrefs.SetString(LAST_AD_TIME_KEY, currentTime);
                PlayerPrefs.Save();
                
                UpdateWatchAdCooldown();
            });
        }
    }

    private void OnApplicationQuit()
    {
        // Save state through GameManager
        GameManager.Instance.SaveGameState();
        SaveManager.Instance.Data.lastClosedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveManager.Instance.SaveGame();
    }
}
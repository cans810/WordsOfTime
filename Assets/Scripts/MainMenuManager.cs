using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public SpriteRenderer BackgroundImage;
    private TextMeshProUGUI pointText;
    private SettingsController settingsController;
    private EraSelectionManager eraSelectionManager;
    private MarketManager marketManager;
    public TextMeshProUGUI eraText;

    private string currentLanguage;

    // Start is called before the first frame update
    private void Start()
    {
        currentLanguage = PlayerPrefs.GetString("Language", "en");
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);

        // Find point text in main menu
        Transform pointPanel = GameObject.Find("PointPanel")?.transform;
        if (pointPanel != null)
        {
            pointText = pointPanel.Find("point")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Find settings controller including inactive objects
        settingsController = FindInactiveObjectByType<SettingsController>();
        eraSelectionManager = FindInactiveObjectByType<EraSelectionManager>();
        marketManager = FindInactiveObjectByType<MarketManager>();

        StartCoroutine(InitializeEraSelectionManager());
        
        UpdatePointsDisplay();
        UpdateEraDisplay();

        // Subscribe to era change events if GameManager has them
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged += UpdateEraDisplay;
        }
    }

    private IEnumerator InitializeEraSelectionManager()
    {
        if (eraSelectionManager != null)
        {
            // Activate the manager
            eraSelectionManager.gameObject.SetActive(true);
            
            // Wait for end of frame to ensure all components are initialized
            yield return new WaitForEndOfFrame();
            
            // Update prices
            eraSelectionManager.UpdateEraPrices();
            
            // Wait another frame to ensure UI is updated
            yield return new WaitForEndOfFrame();
            
            // Deactivate the manager after initialization
            eraSelectionManager.gameObject.SetActive(false);
            
            Debug.Log("EraSelectionManager initialized and deactivated");
        }
        else
        {
            Debug.LogWarning("EraSelectionManager not found");
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
        UpdatePointsDisplay();
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
}

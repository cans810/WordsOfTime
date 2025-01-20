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

    [Header("UI Text Components")]
    [SerializeField] private TextMeshProUGUI playButtonText;
    [SerializeField] private TextMeshProUGUI selectEraButtonText;
    [SerializeField] private TextMeshProUGUI settingsButtonText;
    [SerializeField] private TextMeshProUGUI pointsPanelText;
    [SerializeField] private TextMeshProUGUI musicButtonText;
    [SerializeField] private TextMeshProUGUI soundButtonText;
    [SerializeField] private TextMeshProUGUI notificationsButtonText;
    [SerializeField] private TextMeshProUGUI languageButtonText;
    [SerializeField] private TextMeshProUGUI saveButtonText;
    [SerializeField] private TextMeshProUGUI helpButtonText;
    [SerializeField] private TextMeshProUGUI returnButtonText;

    // Start is called before the first frame update
    void Start()
    {
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);

        // Find point text in main menu
        Transform pointPanel = GameObject.Find("PointPanel")?.transform;
        if (pointPanel != null)
        {
            pointText = pointPanel.Find("point")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Find settings controller including inactive objects
        settingsController = FindInactiveObjectByType<SettingsController>();
        
        UpdatePointsDisplay();
        UpdateUITexts();
    }

    private void OnEnable()
    {
        // Subscribe to language change event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged += UpdateUITexts;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from language change event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged -= UpdateUITexts;
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
    }

    public void PlayButton(){
        SceneManager.LoadScene("GameScene");
    }

    public void SelectEraButton(){
        SceneManager.LoadScene("EraSelectionScene");
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

    public void UpdateUITexts()
    {
        string currentLanguage = GameManager.Instance.CurrentLanguage;

        if (playButtonText != null)
            playButtonText.text = currentLanguage == "en" ? "Play" : "Oyna";

        if (selectEraButtonText != null)
            selectEraButtonText.text = currentLanguage == "en" ? "Select Era" : "Dönem Seç";

        if (settingsButtonText != null)
            settingsButtonText.text = currentLanguage == "en" ? "Settings" : "Ayarlar";

        if (pointsPanelText != null)
            pointsPanelText.text = currentLanguage == "en" ? 
                $"Points: {GameManager.Instance.CurrentPoints}" : 
                $"Puan: {GameManager.Instance.CurrentPoints}";

        if (musicButtonText != null)
            musicButtonText.text = currentLanguage == "en" ? "Music" : "Müzik";

        if (soundButtonText != null)
            soundButtonText.text = currentLanguage == "en" ? "Sound" : "Ses";

        if (notificationsButtonText != null)
            notificationsButtonText.text = currentLanguage == "en" ? "Notifications" : "Bildirimler";

        if (languageButtonText != null)
            languageButtonText.text = currentLanguage == "en" ? "Language" : "Dil";

        if (saveButtonText != null)
            saveButtonText.text = currentLanguage == "en" ? "Save" : "Kaydet";

        if (helpButtonText != null)
            helpButtonText.text = currentLanguage == "en" ? "Help" : "Yardım";

        if (returnButtonText != null)
            returnButtonText.text = currentLanguage == "en" ? "Return" : "Geri";
    }
}

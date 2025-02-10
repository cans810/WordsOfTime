using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsController : MonoBehaviour
{
    private static SettingsController _instance;
    public static SettingsController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SettingsController>();
            }
            return _instance;
        }
    }

    [Header("Language Settings")]
    [SerializeField] private TextMeshProUGUI languageText;
    [SerializeField] private GameObject languageSelectionPanel;

    [Header("Sound Settings")]
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle notificationsToggle;
    [SerializeField] private Button saveButton;
    [SerializeField] private GameObject settingsPanel;  // Reference to the main settings panel

    [SerializeField] private Animator animator;

    public GameObject LanguageSelectionPanel;
    public Image BackgroundImage;

    public Toggle NotificationsToggle => notificationsToggle;  // Public getter for the toggle

    private List<LanguageOption> languages = new List<LanguageOption>()
    {
        new LanguageOption("en", "English"),
        new LanguageOption("tr", "Türkçe")
    };

    private int currentLanguageIndex = 0;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);

        Debug.Log("SettingsController Start");
        if (languageSelectionPanel != null)
        {
            languageSelectionPanel.SetActive(false);
        }

        InitializeLanguageSettings();
        InitializeSoundSettings();

        // Load settings when the scene starts
        LoadSettings();

        // Add listeners to the toggles
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        notificationsToggle.onValueChanged.AddListener(OnNotificationsToggleChanged);
    }

    private void InitializeLanguageSettings()
    {
        string currentLang = GameManager.Instance.CurrentLanguage;
        currentLanguageIndex = languages.FindIndex(l => l.code == currentLang);
        if (currentLanguageIndex == -1) currentLanguageIndex = 0;
        UpdateLanguageDisplay();
    }

    private void InitializeSoundSettings()
    {
        if (soundToggle != null && SoundManager.Instance != null)
        {
            soundToggle.isOn = SoundManager.Instance.IsSoundOn;
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }

        if (musicToggle != null && SoundManager.Instance != null)
        {
            musicToggle.isOn = SoundManager.Instance.IsMusicOn;
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        if (notificationsToggle != null)
        {
            notificationsToggle.onValueChanged.AddListener(OnNotificationsToggleChanged);
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(HandleSaveButtonClick);
        }
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        GameManager.Instance.SetSoundOn(isOn);
        SaveSettings();
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        GameManager.Instance.SetMusicOn(isOn);
        SaveSettings();
    }

    private void OnNotificationsToggleChanged(bool isOn)
    {
        GameManager.Instance.SetNotifications(isOn);
        SaveSettings();
    }

    private void HandleSaveButtonClick()
    {
        SaveManager.Instance.SaveGame();
        ShowSaveConfirmation();
    }

    private void ShowSaveConfirmation()
    {
        Debug.Log("Game saved successfully!");
    }

    #region Language Methods
    public void NextLanguage()
    {
        currentLanguageIndex = (currentLanguageIndex + 1) % languages.Count;
        ChangeLanguage();
    }

    public void PreviousLanguage()
    {
        currentLanguageIndex = (currentLanguageIndex - 1 + languages.Count) % languages.Count;
        ChangeLanguage();
    }

    private void ChangeLanguage()
    {
        Debug.Log($"Changing language to: {languages[currentLanguageIndex].code}"); // Debug log
        GameManager.Instance.SetLanguage(languages[currentLanguageIndex].code);
        UpdateLanguageDisplay();
        
        // Update all LocalizedText components in the scene
        LocalizedText[] localizedTexts = FindObjectsByType<LocalizedText>(FindObjectsSortMode.None);
        foreach (LocalizedText text in localizedTexts)
        {
            text.UpdateText();
        }
    }

    private void UpdateLanguageDisplay()
    {
        if (languageText != null)
        {
            languageText.text = languages[currentLanguageIndex].displayName;
        }
    }
    #endregion

    #region UI Methods
    public void ShowSettings()
    {
        gameObject.SetActive(true);
        StartCoroutine(InitializeSettingsDelayed());
    }

    private IEnumerator InitializeSettingsDelayed()
    {
        // Update background first for visual feedback
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        
        yield return new WaitForEndOfFrame();
        
        // Initialize settings
        if (SoundManager.Instance != null)
        {
            if (soundToggle != null) soundToggle.isOn = SoundManager.Instance.IsSoundOn;
            if (musicToggle != null) musicToggle.isOn = SoundManager.Instance.IsMusicOn;
        }

        if (notificationsToggle != null && SaveManager.Instance != null && SaveManager.Instance.Data != null && SaveManager.Instance.Data.settings != null)
        {
            notificationsToggle.isOn = SaveManager.Instance.Data.settings.notificationsEnabled;
        }

        UpdateLanguageDisplay();
    }

    public void OnLanguageButtonClicked()
    {
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(true);
        }
    }

    public void OnReturnButtonClickedPlayAnimation()
    {
        animator.SetBool("DeLoad", true);
    }

    public void CloseTab()
    {
        if (languageSelectionPanel != null)
        {
            languageSelectionPanel.SetActive(false);
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OnReturnLanguageButtonClicked()
    {
        // Only hide the language selection panel
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(false);
        }
        
        // Apply the selected language
        GameManager.Instance.SetLanguage(languages[currentLanguageIndex].code);
        UpdateLanguageDisplay();
    }
    #endregion

    private void LoadSettings()
    {
        GameSettings settings = GameManager.Instance.GetSettings();
        musicToggle.isOn = settings.musicEnabled;
        soundToggle.isOn = settings.soundEnabled;
        notificationsToggle.isOn = settings.notificationsEnabled;
    }

    private void SaveSettings()
    {
        GameSettings settings = new GameSettings
        {
            musicEnabled = musicToggle.isOn,
            soundEnabled = soundToggle.isOn,
            notificationsEnabled = notificationsToggle.isOn
        };

        if (SaveManager.Instance.Data != null && SaveManager.Instance.Data.settings != null)
        {
            SaveManager.Instance.Data.settings = settings;
        }

        SaveManager.Instance.SaveGame();
    }

    private void OnDestroy()
    {
        // Remove listeners to prevent memory leaks
        if (soundToggle != null)
        {
            soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
        }
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
        }
        if (notificationsToggle != null)
        {
            notificationsToggle.onValueChanged.RemoveListener(OnNotificationsToggleChanged);
        }
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(HandleSaveButtonClick);
        }
    }

    private class LanguageOption
    {
        public string code;
        public string displayName;

        public LanguageOption(string code, string displayName)
        {
            this.code = code;
            this.displayName = displayName;
        }
    }
}

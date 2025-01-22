using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsController : MonoBehaviour
{
    [Header("Language Settings")]
    [SerializeField] private TextMeshProUGUI languageText;
    [SerializeField] private GameObject languageSelectionPanel;

    [Header("Sound Settings")]
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Button saveButton;
    [SerializeField] private GameObject settingsPanel;  // Reference to the main settings panel

    [SerializeField] private Animator animator;

    public GameObject LanguageSelectionPanel;
    public SpriteRenderer BackgroundImage;

    private List<LanguageOption> languages = new List<LanguageOption>()
    {
        new LanguageOption("en", "English"),
        new LanguageOption("tr", "Türkçe")
    };

    private int currentLanguageIndex = 0;

    void Start()
    {
        Debug.Log("SettingsController Start");
        if (languageSelectionPanel != null)
        {
            languageSelectionPanel.SetActive(false);
        }

        InitializeLanguageSettings();
        InitializeSoundSettings();
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

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(HandleSaveButtonClick);
        }
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.IsSoundOn = isOn;
            SaveManager.Instance.SaveGame();
        }
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.IsMusicOn = isOn;
            SaveManager.Instance.SaveGame();
        }
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
        Debug.Log("ShowSettings called, current active state: " + gameObject.activeSelf);
        
        // Force activate the GameObject
        gameObject.SetActive(true);
        
        // Refresh settings state
        if (SoundManager.Instance != null)
        {
            if (soundToggle != null)
            {
                soundToggle.isOn = SoundManager.Instance.IsSoundOn;
            }
            if (musicToggle != null)
            {
                musicToggle.isOn = SoundManager.Instance.IsMusicOn;
            }
        }

        // Refresh language display
        UpdateLanguageDisplay();
        
        Debug.Log("Settings panel should now be visible");
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

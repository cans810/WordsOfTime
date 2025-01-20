using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsController : MonoBehaviour
{
    [Header("Language Settings")]
    [SerializeField] private TextMeshProUGUI languageText;

    [Header("Sound Settings")]
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Toggle musicToggle;

    public GameObject LanguageSelectionPanel;
    public SpriteRenderer BackgroundImage;

    public Animator animator;

    private List<LanguageOption> languages = new List<LanguageOption>()
    {
        new LanguageOption("en", "English"),
        new LanguageOption("tr", "Türkçe")
    };

    private int currentLanguageIndex = 0;

    void Awake()
    {
        // Always ensure settings panel is hidden on startup
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Initially hide the language panel
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(false);
        }

        // Set background image to match current era
        if (BackgroundImage != null && GameManager.Instance != null)
        {
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        }

        InitializeLanguageSettings();

        // Initialize sound toggles
        if (soundToggle != null)
        {
            soundToggle.isOn = SoundManager.Instance.IsSoundOn;
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }

        if (musicToggle != null)
        {
            musicToggle.isOn = SoundManager.Instance.IsMusicOn;
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }
    }

    private void InitializeLanguageSettings()
    {
        // Find initial language index
        string currentLang = GameManager.Instance.CurrentLanguage;
        currentLanguageIndex = languages.FindIndex(l => l.code == currentLang);
        if (currentLanguageIndex == -1) currentLanguageIndex = 0;

        UpdateLanguageDisplay();
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
        
        // Store current word if we're in a game
        string currentWord = null;
        if (WordGameManager.Instance != null)
        {
            currentWord = WordGameManager.Instance.targetWord;
        }

        // Change language
        GameManager.Instance.SetLanguage(languages[currentLanguageIndex].code);
        UpdateLanguageDisplay();

        // If we were in a game, refresh the current word
        if (currentWord != null && WordGameManager.Instance != null)
        {
            Debug.Log($"Refreshing grid for word: {currentWord}");
            WordGameManager.Instance.SetupWordGame(currentWord);
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

    #region Other Settings Methods
    private void ToggleMusic()
    {
        // Implement music toggle logic
    }

    private void ToggleSound()
    {
        // Implement sound toggle logic
    }

    private void ToggleNotifications()
    {
        // Implement notifications toggle logic
    }

    private void SaveSettings()
    {
        // Implement settings save logic
    }

    private void ShowHelp()
    {
        // Implement help display logic
    }

    private void ReturnToMenu()
    {
        // Implement return to menu logic
    }
    #endregion

    private void OnSoundToggleChanged(bool isOn)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.IsSoundOn = isOn;
        }
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.IsMusicOn = isOn;
        }
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

    public void ShowSettings()
    {
        // Update background image when showing settings
        if (BackgroundImage != null && GameManager.Instance != null)
        {
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        }
        gameObject.SetActive(true);
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
        animator.SetBool("DeLoad",true);
    }

    public void CloseTab(){
        // Hide language panel
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(false);
        }
        
        // Hide this settings panel
        gameObject.SetActive(false);
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
}

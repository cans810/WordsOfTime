using UnityEngine;
using TMPro;

public class TranslatedUI : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private string translationKey;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        GenerateTranslationKey();
    }

    private void Start()
    {
        // Initial translation
        UpdateText();
        
        // Subscribe to language change event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged += OnLanguageChanged;
            Debug.Log($"Subscribed to language changes for {translationKey}"); // Debug log
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe when destroyed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    // New method to handle the language change event
    private void OnLanguageChanged()
    {
        Debug.Log($"Language change detected for {translationKey}"); // Debug log
        UpdateText();
    }

    private void GenerateTranslationKey()
    {
        string parentName = transform.parent.gameObject.name;
        parentName = parentName.ToLower()
            .Replace("button", "")
            .Replace("panel", "")
            .Replace(" ", "")
            .Trim();

        switch (parentName)
        {
            case "play":
                translationKey = "play_button";
                break;
            case "selectera":
                translationKey = "select_era_button";
                break;
            case "settings":
                translationKey = "settings_button";
                break;
            case "point":
                translationKey = "points_panel";
                break;
            case "music":
                translationKey = "music_button";
                break;
            case "sound":
                translationKey = "sound_button";
                break;
            case "notifications":
                translationKey = "notifications_button";
                break;
            case "language":
                translationKey = "language_button";
                break;
            case "save":
                translationKey = "save_button";
                break;
            case "help":
                translationKey = "help_button";
                break;
            case "return":
                translationKey = "return_button";
                break;
            case "hint":
                translationKey = "hint_button";
                break;
            case "next":
                translationKey = "next_button";
                break;
            case "previous":
                translationKey = "previous_button";
                break;
            case "home":
                translationKey = "home_button";
                break;
            case "ancientegypt":
                translationKey = "ancient_egypt";
                break;
            case "medivaleurope":
                translationKey = "medival_europe";
                break;
            case "renaissance":
                translationKey = "renaissance";
                break;
            case "industrialrevolution":
                translationKey = "industrial_revolution";
                break;
            case "home":
                translationKey = "home_button";
                break;
            case "home":
                translationKey = "home_button";
                break;
            case "home":
                translationKey = "home_button";
                break;
            case "home":
                translationKey = "home_button";
                break;
            default:
                translationKey = parentName + "_button";
                break;
        }

        Debug.Log($"Generated translation key: {translationKey} for object: {parentName}"); // Debug log
    }

    public void UpdateText()
    {
        Debug.Log($"UpdateText called for {translationKey}"); // Debug log
        
        if (textComponent == null)
        {
            Debug.LogError($"TextComponent is null for {translationKey}");
            return;
        }
        
        if (TranslationManager.Instance == null)
        {
            Debug.LogError("TranslationManager.Instance is null");
            return;
        }

        if (translationKey == "points_panel" && GameManager.Instance != null)
        {
            textComponent.text = TranslationManager.Instance.GetFormattedTranslation(
                translationKey, 
                GameManager.Instance.CurrentPoints
            );
        }
        else if (translationKey == "hint_button" && GameManager.Instance != null)
        {
            textComponent.text = TranslationManager.Instance.GetFormattedTranslation(
                translationKey,
                GameManager.HINT_COST
            );
        }
        else
        {
            string translation = TranslationManager.Instance.GetTranslation(translationKey);
            Debug.Log($"Setting text for {translationKey} to: {translation}"); // Debug log
            textComponent.text = translation;
        }
    }
} 
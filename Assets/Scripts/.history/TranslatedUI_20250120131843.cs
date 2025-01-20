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
        // Special case for Points text - check the GameObject's own name first
        if (gameObject.name == "Points")
        {
            // Get the era name from the parent GameObject
            string eraName = transform.parent.gameObject.name.ToLower()
                .Replace(" ", "")
                .Trim();

            switch (eraName)
            {
                case "ancientegypt":
                    translationKey = "ancient_egypt_points";
                    break;
                case "medievaleurope":
                    translationKey = "medieval_europe_points";
                    break;
                case "renaissance":
                    translationKey = "renaissance_points";
                    break;
                case "industrialrevolution":
                    translationKey = "industrial_revolution_points";
                    break;
                case "ancientgreece":
                    translationKey = "ancient_greece_points";
                    break;
                default:
                    translationKey = "points";
                    break;
            }
            Debug.Log($"Generated translation key for Points: {translationKey}");
            return;
        }

        // Get the parent GameObject's name for all other cases
        string parentName = transform.parent.gameObject.name;
        
        // Convert to lowercase and remove common suffixes
        parentName = parentName.ToLower()
            .Replace("button", "")
            .Replace("panel", "")
            .Replace(" ", "")
            .Trim();

        // Generate the translation key
        translationKey = parentName + "_button";

        // Special cases
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
            case "ancientgreece":
                translationKey = "ancient_greece";
                break;
            case "points":
                translationKey = "points";
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
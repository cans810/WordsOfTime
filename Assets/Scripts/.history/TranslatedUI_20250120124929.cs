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
            GameManager.Instance.OnLanguageChanged += UpdateText;
            Debug.Log($"Subscribed to language changes for {translationKey}"); // Debug log
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe when destroyed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged -= UpdateText;
        }
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
            default:
                translationKey = parentName + "_button";
                break;
        }

        Debug.Log($"Generated translation key: {translationKey} for object: {parentName}"); // Debug log
    }

    public void UpdateText()
    {
        if (textComponent != null && TranslationManager.Instance != null)
        {
            if (translationKey == "points_panel" && GameManager.Instance != null)
            {
                // Special handling for points display
                textComponent.text = TranslationManager.Instance.GetFormattedTranslation(
                    translationKey, 
                    GameManager.Instance.CurrentPoints
                );
            }
            else if (translationKey == "hint_button" && GameManager.Instance != null)
            {
                // Special handling for hint button
                textComponent.text = TranslationManager.Instance.GetFormattedTranslation(
                    translationKey,
                    GameManager.HINT_COST
                );
            }
            else
            {
                textComponent.text = TranslationManager.Instance.GetTranslation(translationKey);
            }
            Debug.Log($"Updated text for {translationKey} to: {textComponent.text}"); // Debug log
        }
    }
} 
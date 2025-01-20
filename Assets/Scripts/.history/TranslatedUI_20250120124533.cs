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
        UpdateText();
    }

    private void GenerateTranslationKey()
    {
        // Get the parent GameObject's name
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
        }

        Debug.Log($"Generated translation key: {translationKey} for object: {parentName}");
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged += UpdateText;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    public void UpdateText()
    {
        if (textComponent != null && TranslationManager.Instance != null)
        {
            textComponent.text = TranslationManager.Instance.GetTranslation(translationKey);
        }
    }
} 
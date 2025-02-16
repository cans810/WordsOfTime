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
            translationKey = "points"; // Just use "points" as key, we'll handle the formatting in UpdateText
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
            case "medievaleurope":
                translationKey = "medieval_europe";
                break;
            case "renaissance":
                translationKey = "renaissance";
                break;
            case "ındustrialrevolution":
                translationKey = "ındustrial_revolution";
                break;
            case "ancientgreece":
                translationKey = "ancient_greece";
                break;
            case "points":
                translationKey = "points";
                break;
            case "buy":
                translationKey = "buy";
                break;
            case "vikingage":
                translationKey = "viking_age";
                break;
            case "ottomanempire":
                translationKey = "ottoman_empire";
                break;
            case "feudaljapan":
                translationKey = "feudal_japan";
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
            if (gameObject.name == "Points")
            {
                // Get the era name from parent
                string eraName = transform.parent.gameObject.name;
                string eraNameLower = eraName.ToLower().Replace(" ", "").Trim();

                // Check if it's Ancient Egypt or Medieval Europe
                if (eraName == "Ancient Egypt" || eraName == "Medieval Europe")
                {
                    // Get unlocked text based on current language
                    string unlockedText = GameManager.Instance.CurrentLanguage == "tr" ? "AÇIK" : "UNLOCKED";
                    textComponent.text = unlockedText;
                    textComponent.color = Color.green;
                }
                else
                {
                    // Get points required for this era
                    int requiredPoints = GameManager.Instance.GetRequiredPointsForEra(eraNameLower);

                    // Get the translated "points" text
                    string pointsText = TranslationManager.Instance.GetTranslation("points");
                    
                    // Combine them
                    textComponent.text = $"{requiredPoints}";
                }
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
                textComponent.text = TranslationManager.Instance.GetTranslation(translationKey);
            }
        }
    }
} 
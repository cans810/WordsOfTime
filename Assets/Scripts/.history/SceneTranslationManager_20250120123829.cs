using UnityEngine;
using TMPro;

public class SceneTranslationManager : MonoBehaviour
{
    private void Start()
    {
        UpdateAllTranslations();
    }

    public void UpdateAllTranslations()
    {
        // Find all TextMeshPro components in the scene
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        
        foreach (TextMeshProUGUI text in allTexts)
        {
            // Get the GameObject name
            string objectName = text.gameObject.name.ToLower();
            
            // Remove "Text (TMP)" or similar suffixes from the name
            objectName = objectName.Replace("text", "").Replace("(tmp)", "").Trim();
            
            // Convert button names to translation keys
            string translationKey = "";
            
            switch (objectName)
            {
                case "PlayButton":
                    translationKey = "play_button";
                    break;
                case "selecterabutton":
                    translationKey = "select_era_button";
                    break;
                case "settingsbutton":
                    translationKey = "settings_button";
                    break;
                case "pointpanel":
                    translationKey = "points_panel";
                    break;
                case "musicbutton":
                    translationKey = "music_button";
                    break;
                case "soundbutton":
                    translationKey = "sound_button";
                    break;
                case "notificationsbutton":
                    translationKey = "notifications_button";
                    break;
                case "languagebutton":
                    translationKey = "language_button";
                    break;
                case "savebutton":
                    translationKey = "save_button";
                    break;
                case "helpbutton":
                    translationKey = "help_button";
                    break;
                case "returnbutton":
                    translationKey = "return_button";
                    break;
            }

            // If we found a matching translation key, update the text
            if (!string.IsNullOrEmpty(translationKey))
            {
                string translation = TranslationManager.Instance.GetTranslation(translationKey);
                
                // Special handling for points panel
                if (translationKey == "points_panel" && GameManager.Instance != null)
                {
                    text.text = string.Format(translation, GameManager.Instance.CurrentPoints);
                }
                else
                {
                    text.text = translation;
                }
            }
        }
    }
} 
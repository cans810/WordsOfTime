using UnityEngine;
using System.Collections.Generic;

public class TranslationManager : MonoBehaviour
{
    public static TranslationManager Instance { get; private set; }

    private Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>()
    {
        {
            "en", new Dictionary<string, string>
            {
                {"play_button", "Play"},
                {"select_era_button", "Select Era"},
                {"settings_button", "Settings"},
                {"points_panel", "Points: {0}"},
                {"music_button", "Music"},
                {"sound_button", "Sound"},
                {"notifications_button", "Notifications"},
                {"language_button", "Language"},
                {"save_button", "Save"},
                {"help_button", "Help"},
                {"return_button", "Return"},
                {"hint_button", "Hint ({0} pts)"},
                {"next_button", "Next"},
                {"previous_button", "Previous"},
                {"home_button", "Home"}
            }
        },
        {
            "tr", new Dictionary<string, string>
            {
                {"play_button", "Oyna"},
                {"select_era_button", "Dönem Seç"},
                {"settings_button", "Ayarlar"},
                {"points_panel", "Puan: {0}"},
                {"music_button", "Müzik"},
                {"sound_button", "Ses"},
                {"notifications_button", "Bildirimler"},
                {"language_button", "Dil"},
                {"save_button", "Kaydet"},
                {"help_button", "Yardım"},
                {"return_button", "Geri"},
                {"hint_button", "İpucu ({0} puan)"},
                {"next_button", "İleri"},
                {"previous_button", "Geri"},
                {"home_button", "Ana Sayfa"}
            }
        }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetTranslation(string key)
    {
        string currentLanguage = GameManager.Instance.CurrentLanguage;
        
        if (translations.ContainsKey(currentLanguage) && 
            translations[currentLanguage].ContainsKey(key))
        {
            return translations[currentLanguage][key];
        }

        Debug.LogWarning($"Translation not found for key: {key} in language: {currentLanguage}");
        return key; // Return key as fallback
    }

    // Special method for formatted strings (like points display)
    public string GetFormattedTranslation(string key, params object[] args)
    {
        string translation = GetTranslation(key);
        return string.Format(translation, args);
    }
} 
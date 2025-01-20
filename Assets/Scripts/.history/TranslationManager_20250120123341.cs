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
                {"hint_button", "({0} pts)"},
                {"next_button", "Next"},
                {"previous_button", "Previous"},
                {"home_button", "Home"},
                {"settings_button", "Settings"},
                {"sound_toggle", "Sound Effects"},
                {"music_toggle", "Music"},
                {"language_button", "Language"},
                // Add more translations as needed
            }
        },
        {
            "tr", new Dictionary<string, string>
            {
                {"hint_button", "({0} puan)"},
                {"next_button", "İleri"},
                {"previous_button", "Geri"},
                {"home_button", "Ana Sayfa"},
                {"settings_button", "Ayarlar"},
                {"sound_toggle", "Ses Efektleri"},
                {"music_toggle", "Müzik"},
                {"language_button", "Dil"},
                // Add more translations as needed
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

    public string GetTranslation(string key, string language = null)
    {
        language = language ?? GameManager.Instance.CurrentLanguage;
        
        if (translations.ContainsKey(language) && translations[language].ContainsKey(key))
        {
            return translations[language][key];
        }
        
        return key;
    }
} 
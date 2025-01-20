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
                {"hint_button", "({0} pts)"},
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
                {"hint_button", "({0} puan)"},
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

    private void Start()
    {
        Debug.Log("TranslationManager started. Available languages: " + 
                  string.Join(", ", translations.Keys));
        
        foreach (var lang in translations.Keys)
        {
            Debug.Log($"Keys in {lang}: " + 
                      string.Join(", ", translations[lang].Keys));
        }
    }

    public string GetTranslation(string key)
    {
        string currentLanguage = GameManager.Instance.CurrentLanguage;
        Debug.Log($"Getting translation for key: {key}, language: {currentLanguage}"); // Debug log
        
        if (translations.ContainsKey(currentLanguage) && 
            translations[currentLanguage].ContainsKey(key))
        {
            string translation = translations[currentLanguage][key];
            Debug.Log($"Found translation: {translation}"); // Debug log
            return translation;
        }

        Debug.LogWarning($"Translation not found for key: {key} in language: {currentLanguage}");
        return key;
    }

    // Special method for formatted strings (like points display)
    public string GetFormattedTranslation(string key, params object[] args)
    {
        string translation = GetTranslation(key);
        return string.Format(translation, args);
    }
} 
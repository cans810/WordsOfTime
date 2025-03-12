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
                {"ancient_egypt", "Ancient Egypt"},
                {"medieval_europe", "Medieval Europe"},
                {"renaissance", "Renaissance"},
                {"ındustrial_revolution", "Industrial Revolution"},
                {"ancient_greece", "Ancient Greece"},
                {"points", "points"},
                {"buy", "Buy"},
                {"viking_age", "Viking Age"},
                {"ottoman_empire", "Ottoman Empire"},
                {"feudal_japan", "Feudal Japan"}
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
                {"ancient_egypt", "Antik Mısır"},
                {"medieval_europe", "Orta Cag Avrupa"},
                {"renaissance", "Rönesans"},
                {"ındustrial_revolution", "Sanayi Devrimi"},
                {"ancient_greece", "Antik Yunan"},
                {"points", "puan"},
                {"buy", "Al"},
                {"viking_age", "Viking Çağı"},
                {"ottoman_empire", "Osmanlı İmparatorluğu"},
                {"feudal_japan", "Feodal Japonya"}
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

    }

    public string GetTranslation(string key)
    {
        string currentLanguage = GameManager.Instance.CurrentLanguage;
        
        if (translations.ContainsKey(currentLanguage) && 
            translations[currentLanguage].ContainsKey(key))
        {
            string translation = translations[currentLanguage][key];
            return translation;
        }

        return key;
    }

    // Special method for formatted strings (like points display)
    public string GetFormattedTranslation(string key, params object[] args)
    {
        string translation = GetTranslation(key);
        return string.Format(translation, args);
    }
} 
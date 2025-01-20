using UnityEngine;
using System.Collections.Generic;

public class TranslationManager : MonoBehaviour
{
    public static TranslationManager Instance { get; private set; }

    private TextAsset translationFile;
    private Dictionary<string, Dictionary<string, string>> translations;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTranslations();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadTranslations()
    {
        // Load from a JSON file in Resources folder
        translationFile = Resources.Load<TextAsset>("translations");
        if (translationFile != null)
        {
            translations = JsonUtility.FromJson<Dictionary<string, Dictionary<string, string>>>(translationFile.text);
        }
    }

    public string GetTranslation(string key, string language)
    {
        if (translations != null && 
            translations.ContainsKey(language) && 
            translations[language].ContainsKey(key))
        {
            return translations[language][key];
        }
        return key; // Return key as fallback
    }
} 
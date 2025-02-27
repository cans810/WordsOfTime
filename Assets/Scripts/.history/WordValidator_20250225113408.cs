using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> wordSetsWithFactsByLanguage;
    private static bool isInitialized = false;
    private static MonoBehaviour coroutineRunner;

    static WordValidator()
    {
        // Initialize the dictionary to store facts for different languages
        wordSetsWithFactsByLanguage = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        wordSetsWithFactsByLanguage["en"] = new Dictionary<string, Dictionary<string, string>>();
        wordSetsWithFactsByLanguage["tr"] = new Dictionary<string, Dictionary<string, string>>();
        
        // We'll initialize the word sets when a MonoBehaviour is available to run coroutines
    }

    public static void Initialize(MonoBehaviour runner)
    {
        coroutineRunner = runner;
        if (!isInitialized)
        {
            LoadWordSets();
        }
    }

    public static void LoadWordSets()
    {
        Debug.Log("[Android Debug] Initializing word sets dictionaries");
        
        if (coroutineRunner == null)
        {
            Debug.LogError("[Android Debug] No MonoBehaviour available to run coroutines for loading word sets!");
            // Try to find a suitable runner
            coroutineRunner = GameObject.FindObjectOfType<WordGameManager>();
            if (coroutineRunner == null)
            {
                coroutineRunner = GameObject.FindObjectOfType<MonoBehaviour>();
                if (coroutineRunner == null)
                {
                    Debug.LogError("[Android Debug] Could not find any MonoBehaviour to run coroutines!");
                    return;
                }
            }
        }

        // Load English words
        #if UNITY_ANDROID && !UNITY_EDITOR
            coroutineRunner.StartCoroutine(LoadWordsForLanguageAndroid("en"));
            coroutineRunner.StartCoroutine(LoadWordsForLanguageAndroid("tr"));
        #else
            LoadWordsForLanguage("en");
            LoadWordsForLanguage("tr");
        #endif
        
        isInitialized = true;
        Debug.Log("[Android Debug] Word sets loading started");
    }

    private static IEnumerator LoadWordsForLanguageAndroid(string language)
    {
        Debug.Log($"[Android Debug] Loading words for language: {language} on Android");
        string fileName = language == "en" ? "words.json" : $"words_{language}.json";
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        
        using (UnityWebRequest request = UnityWebRequest.Get(filePath))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"[Android Debug] Successfully loaded {language} words JSON from {filePath}");
                ParseWordSetJson(json, language);
            }
            else
            {
                Debug.LogError($"[Android Debug] Failed to load {language} words: {request.error}");
            }
        }
    }

    private static void LoadWordsForLanguage(string language)
    {
        Debug.Log($"[Android Debug] Loading words for language: {language}");
        string fileName = language == "en" ? "words.json" : $"words_{language}.json";
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"[Android Debug] File not found: {filePath}");
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            ParseWordSetJson(json, language);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error reading JSON file for {language}: {e.Message}");
        }
    }

    private static void ParseWordSetJson(string json, string language)
    {
        try
        {
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

            if (wordSetList?.sets != null)
            {
                wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                
                foreach (var wordSet in wordSetList.sets)
                {
                    Debug.Log($"[Android Debug] Loading era: '{wordSet.era}' for language: {language}");
                    
                    var wordDict = new Dictionary<string, List<string>>();
                    var factDict = new Dictionary<string, string>();
                    
                    foreach (var wordEntry in wordSet.words)
                    {
                        string wordKey = wordEntry.word.ToUpper();
                        wordDict[wordKey] = new List<string>(wordEntry.sentences);
                        if (!string.IsNullOrEmpty(wordEntry.didYouKnow))
                        {
                            Debug.Log($"[Android Debug] Adding fact for word: '{wordKey}' in era: '{wordSet.era}' ({language})");
                            factDict[wordKey] = wordEntry.didYouKnow;
                        }
                    }
                    
                    wordSetsWithSentences[wordSet.era] = wordDict;
                    
                    // Initialize the dictionary for this language if it doesn't exist
                    if (!wordSetsWithFactsByLanguage[language].ContainsKey(wordSet.era))
                    {
                        wordSetsWithFactsByLanguage[language][wordSet.era] = new Dictionary<string, string>();
                    }
                    wordSetsWithFactsByLanguage[language][wordSet.era] = factDict;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error parsing JSON file for {language}: {e.Message}");
        }

        // Log all loaded eras for this language
        Debug.Log($"[Android Debug] Loaded eras for {language}: {string.Join(", ", wordSetsWithFactsByLanguage[language].Keys)}");
    }

    public static string GetSentenceForWord(string word, string era)
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GetRandomSentenceForWord(word, era);
        }
        return null;
    }

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSetsWithSentences == null || !wordSetsWithSentences.ContainsKey(era))
            return new List<string>();

        return new List<string>(wordSetsWithSentences[era].Keys);
    }

    public static bool IsValidWord(string word, string era)
    {
        return wordSetsWithSentences != null && 
               wordSetsWithSentences.ContainsKey(era) &&
               wordSetsWithSentences[era].ContainsKey(word.ToUpper());
    }

    public static string GetFactForWord(string word, string era, string language = "en")
    {
        Debug.Log($"Getting fact for word: '{word}', era: '{era}', language: '{language}'");
        
        if (!wordSetsWithFactsByLanguage.ContainsKey(language))
        {
            Debug.LogWarning($"No facts dictionary found for language: {language}");
            return string.Empty;
        }

        // Translate era name if language is Turkish
        string translatedEra = era;
        if (language == "tr")
        {
            switch (era)
            {
                case "Ancient Egypt":
                    translatedEra = "Antik Mısır";
                    break;
                case "Medieval Europe":
                    translatedEra = "Orta Çağ Avrupası";
                    break;
                case "Renaissance":
                    translatedEra = "Rönesans";
                    break;
                case "Industrial Revolution":
                    translatedEra = "Sanayi Devrimi";
                    break;
                case "Ancient Greece":
                    translatedEra = "Antik Yunan";
                    break;
                case "Viking Age":
                    translatedEra = "Viking Çağı";
                    break;
                case "Feudal Japan":
                    translatedEra = "Feodal Japonya";
                    break;
                case "Ottoman Empire":
                    translatedEra = "Osmanlı İmparatorluğu";
                    break;
            }
        }

        Debug.Log($"Looking for era: '{translatedEra}' in {language}");
        Debug.Log($"Available eras in {language}: {string.Join(", ", wordSetsWithFactsByLanguage[language].Keys)}");

        if (!wordSetsWithFactsByLanguage[language].ContainsKey(translatedEra))
        {
            Debug.LogWarning($"No facts found for era: '{translatedEra}' in language: {language}. Available eras: {string.Join(", ", wordSetsWithFactsByLanguage[language].Keys)}");
            return string.Empty;
        }

        string wordKey = word.ToUpper();
        Debug.Log($"Available words in era '{translatedEra}': {string.Join(", ", wordSetsWithFactsByLanguage[language][translatedEra].Keys)}");
        
        if (wordSetsWithFactsByLanguage[language][translatedEra].ContainsKey(wordKey))
        {
            return wordSetsWithFactsByLanguage[language][translatedEra][wordKey];
        }

        Debug.LogWarning($"No fact found for word: '{wordKey}' in era: '{translatedEra}', language: {language}");
        return string.Empty;
    }
}

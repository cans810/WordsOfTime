using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> wordSetsWithFactsByLanguage;
    private static bool isLoading = false;

    static WordValidator()
    {
        LoadWordSets();
    }

    public static void LoadWordSets()
    {
        Debug.Log("Initializing word sets dictionaries");
        // Initialize the dictionary to store facts for different languages
        wordSetsWithFactsByLanguage = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        wordSetsWithFactsByLanguage["en"] = new Dictionary<string, Dictionary<string, string>>();
        wordSetsWithFactsByLanguage["tr"] = new Dictionary<string, Dictionary<string, string>>();

        // Load English words
        #if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, we need to use a coroutine to load the files
            // Since static classes can't use coroutines directly, we'll use a helper MonoBehaviour
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("en"));
                GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("tr"));
            }
            else
            {
                Debug.LogError("GameManager.Instance is null, cannot load words on Android");
            }
        #else
            // On other platforms, we can load directly
            LoadWordsForLanguage("en");
            LoadWordsForLanguage("tr");
        #endif
        
        Debug.Log("Word sets loading initiated");
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    private static IEnumerator LoadWordsForLanguageAndroid(string language)
    {
        isLoading = true;
        Debug.Log($"[Android] Loading words for language: {language}");
        string fileName = language == "en" ? "words.json" : $"words_{language}.json";
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        
        using (UnityWebRequest request = UnityWebRequest.Get(filePath))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                ProcessJsonContent(json, language);
                Debug.Log($"[Android] Successfully loaded {language} words");
            }
            else
            {
                Debug.LogError($"[Android] Failed to load {language} words: {request.error}");
            }
        }
        
        isLoading = false;
        Debug.Log($"[Android] Loaded eras for {language}: {string.Join(", ", wordSetsWithFactsByLanguage[language].Keys)}");
    }
    #endif

    private static void LoadWordsForLanguage(string language)
    {
        Debug.Log($"Loading words for language: {language}");
        string fileName = language == "en" ? "words.json" : $"words_{language}.json";
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            ProcessJsonContent(json, language);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON file for {language}: {e.Message}");
        }

        // Log all loaded eras for this language
        Debug.Log($"Loaded eras for {language}: {string.Join(", ", wordSetsWithFactsByLanguage[language].Keys)}");
    }
    
    private static void ProcessJsonContent(string json, string language)
    {
        try
        {
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

            if (wordSetList?.sets != null)
            {
                wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                
                foreach (var wordSet in wordSetList.sets)
                {
                    Debug.Log($"Loading era: '{wordSet.era}' for language: {language}");
                    
                    var wordDict = new Dictionary<string, List<string>>();
                    var factDict = new Dictionary<string, string>();
                    
                    foreach (var wordEntry in wordSet.words)
                    {
                        string wordKey = wordEntry.word.ToUpper();
                        wordDict[wordKey] = new List<string>(wordEntry.sentences);
                        if (!string.IsNullOrEmpty(wordEntry.didYouKnow))
                        {
                            Debug.Log($"Adding fact for word: '{wordKey}' in era: '{wordSet.era}' ({language})");
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
            Debug.LogError($"Error processing JSON content for {language}: {e.Message}");
        }
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
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Check if we're still loading the words on Android
        if (isLoading)
        {
            Debug.LogWarning($"[Android] Facts are still loading for {language}. Cannot retrieve fact for '{word}' yet.");
            return "LOADING"; // Special return value to indicate loading state
        }
        #endif
        
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

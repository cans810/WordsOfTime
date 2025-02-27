using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;
using System;

[System.Serializable]
internal class WordData
{
    public WordSet[] sets;
}

[System.Serializable]
internal class Era
{
    public string name;
    public Word[] words;
}

[System.Serializable]
internal class Word
{
    public string word;
    public Translations translations;
    public string difficulty;
    public string[] sentences;
    public string didYouKnow;
}

[System.Serializable]
internal class Translations
{
    public string en;
    public string tr;
}

public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> wordSetsWithFactsByLanguage;
    private static bool isLoading = false;
    private static bool didInitiateLoading = false;
    private static Dictionary<string, Dictionary<string, Dictionary<string, Word>>> loadedWords = new Dictionary<string, Dictionary<string, Dictionary<string, Word>>>();
    private static List<string> loadedLanguages = new List<string>();

    static WordValidator()
    {
        LoadWordSets();
    }

    public static void LoadWordSets()
    {
        Debug.Log("[WordValidator] Initializing word sets dictionaries");
        // Initialize the dictionary to store facts for different languages
        wordSetsWithFactsByLanguage = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        wordSetsWithFactsByLanguage["en"] = new Dictionary<string, Dictionary<string, string>>();
        wordSetsWithFactsByLanguage["tr"] = new Dictionary<string, Dictionary<string, string>>();

        // If we're on Android and not in the editor, wait until GameManager is ready
        #if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("[WordValidator] Running on Android platform, will load words when GameManager is available");
            isLoading = true;  // Set loading state to true immediately
            // Check for GameManager in a separate method that can be called repeatedly
            TryStartAndroidLoading();
        #else
            // On other platforms, we can load directly
            LoadWordsForLanguage("en");
            LoadWordsForLanguage("tr");
        #endif
        
        Debug.Log("[WordValidator] Word sets loading initiated");
    }
    
    // Method to initialize word loading on Android platforms
    private static void TryStartAndroidLoading()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Android Debug] GameManager.Instance is null, cannot start Android loading yet");
            return;
        }

        Debug.Log("[Android Debug] Starting Android loading for words and facts");
        isLoading = true;

        // Start coroutines to load words for both English and Turkish
        GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("en"));
        GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("tr"));
        #else
        // No-op for non-Android platforms
        Debug.Log("[WordValidator] TryStartAndroidLoading is a no-op on this platform");
        #endif
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    private static IEnumerator LoadWordsForLanguageAndroid(string language)
    {
        Debug.Log($"[Android Debug] LoadWordsForLanguageAndroid started for {language}");
        
        string fileName = language == "en" ? "words.json" : "words_tr.json";
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        
        Debug.Log($"[Android Debug] Loading words from: {filePath}");
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(filePath))
        {
            // Send the request and wait for it to complete
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonContent = webRequest.downloadHandler.text;
                Debug.Log($"[Android Debug] Successfully downloaded {fileName}. Content length: {jsonContent.Length}");
                
                try
                {
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        // Process the JSON content
                        ProcessWordJson(jsonContent, language);
                        
                        // Log some sample data to verify content
                        LogSampleData(language);
                    }
                    else
                    {
                        Debug.LogError($"[Android Debug] Downloaded JSON content is empty for {language}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Android Debug] Error processing JSON for {language}: {e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                Debug.LogError($"[Android Debug] Failed to download {fileName}: {webRequest.error}");
            }
        }
        
        // Update loading state
        if (language == "tr") // Assume tr is loaded last
        {
            isLoading = false;
            Debug.Log("[Android Debug] All word loading completed");
        }
    }
    
    private static void ProcessWordJson(string jsonContent, string language)
    {
        Debug.Log($"[Android Debug] Processing JSON for {language}");
        
        try
        {
            // Parse the JSON
            WordData wordData = JsonUtility.FromJson<WordData>(jsonContent);
            
            if (wordData == null || wordData.sets == null)
            {
                Debug.LogError($"[Android Debug] Failed to parse JSON data for {language}");
                return;
            }
            
            // Initialize dictionaries if needed
            if (!loadedWords.ContainsKey(language))
            {
                loadedWords[language] = new Dictionary<string, Dictionary<string, Word>>(System.StringComparer.OrdinalIgnoreCase);
                loadedLanguages.Add(language);
            }
            
            // Process each era
            foreach (WordSet set in wordData.sets)
            {
                string eraName = set.era.ToLower();
                
                if (!loadedWords[language].ContainsKey(eraName))
                {
                    loadedWords[language][eraName] = new Dictionary<string, Word>(System.StringComparer.OrdinalIgnoreCase);
                }
                
                // Process each word in the era
                foreach (WordEntry wordEntry in set.words)
                {
                    if (!string.IsNullOrEmpty(wordEntry.word))
                    {
                        // Create a Word object from the WordEntry
                        Word word = new Word
                        {
                            word = wordEntry.word,
                            translations = wordEntry.translations,
                            difficulty = wordEntry.difficulty,
                            sentences = wordEntry.sentences,
                            didYouKnow = wordEntry.didYouKnow
                        };
                        
                        // Store the word using the appropriate key based on language
                        string wordKey = language == "en" ? wordEntry.word.ToLower() : wordEntry.translations.tr.ToLower();
                        loadedWords[language][eraName][wordKey] = word;
                    }
                }
            }
            
            Debug.Log($"[Android Debug] Successfully processed JSON for {language}. Found {loadedWords[language].Count} eras");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error in ProcessWordJson for {language}: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private static void LogSampleData(string language)
    {
        if (!loadedWords.ContainsKey(language))
        {
            Debug.LogWarning($"[Android Debug] No data available for language: {language}");
            return;
        }
        
        Debug.Log($"[Android Debug] Sample data for {language}:");
        int erasCount = loadedWords[language].Count;
        Debug.Log($"[Android Debug] - Total eras: {erasCount}");
        
        if (erasCount > 0)
        {
            string firstEra = loadedWords[language].Keys.First();
            int wordsInFirstEra = loadedWords[language][firstEra].Count;
            Debug.Log($"[Android Debug] - First era: {firstEra}, Word count: {wordsInFirstEra}");
            
            if (wordsInFirstEra > 0)
            {
                string firstWord = loadedWords[language][firstEra].Keys.First();
                Word wordData = loadedWords[language][firstEra][firstWord];
                Debug.Log($"[Android Debug] - Sample word: {firstWord}");
                Debug.Log($"[Android Debug] - Has fact: {!string.IsNullOrEmpty(wordData.didYouKnow)}");
                if (!string.IsNullOrEmpty(wordData.didYouKnow))
                {
                    string sampleFact = wordData.didYouKnow.Length > 50 ? 
                        wordData.didYouKnow.Substring(0, 50) + "..." : 
                        wordData.didYouKnow;
                    Debug.Log($"[Android Debug] - Sample fact: {sampleFact}");
                }
            }
        }
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
                // Initialize the dictionary if it doesn't exist yet
                if (wordSetsWithSentences == null)
                {
                    wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                }
                
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
                    
                    // Use the standardized era name for all languages
                    string standardEra = language == "tr" ? MapTurkishEraToEnglish(wordSet.era) : wordSet.era;
                    
                    // Add or update the era in the dictionary
                    wordSetsWithSentences[standardEra] = wordDict;
                    
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

    // Helper method to standardize era names
    private static string MapTurkishEraToEnglish(string turkishEra)
    {
        Dictionary<string, string> eraMapping = new Dictionary<string, string>
        {
            {"Antik Mısır", "Ancient Egypt"},
            {"Antik Yunan", "Ancient Greece"},
            {"Orta Çağ Avrupası", "Medieval Europe"},
            {"Rönesans", "Renaissance"},
            {"Sanayi Devrimi", "Industrial Revolution"},
            {"Viking Çağı", "Viking Age"},
            {"Osmanlı İmparatorluğu", "Ottoman Empire"},
            {"Feodal Japonya", "Feudal Japan"}
        };

        if (eraMapping.ContainsKey(turkishEra))
        {
            Debug.Log($"Mapped Turkish era '{turkishEra}' to '{eraMapping[turkishEra]}'");
            return eraMapping[turkishEra];
        }
        return turkishEra;
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

    public static string GetFactForWord(string word, string era, string language)
    {
        if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(era))
        {
            Debug.LogWarning($"[Android Debug] GetFactForWord called with invalid parameters: word='{word}', era='{era}', language='{language}'");
            return string.Empty;
        }

        Debug.Log($"[Android Debug] GetFactForWord called for word='{word}', era='{era}', language='{language}'");
        
        // Force initialization if not already done
        if (!didInitiateLoading && GameManager.Instance != null)
        {
            Debug.Log("[Android Debug] Forcing initialization of word loading for facts");
            didInitiateLoading = true;
            
            if (Application.platform == RuntimePlatform.Android)
            {
                Debug.Log("[Android Debug] Starting Android loading");
                TryStartAndroidLoading();
            }
        }

        // Check if GameManager instance is available
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Android Debug] GameManager.Instance is null in GetFactForWord!");
            return "LOADING";
        }

        // Check if facts are still being loaded
        if (isLoading)
        {
            Debug.Log("[Android Debug] Facts are still being loaded...");
            return "LOADING";
        }

        // Translate era name if language is Turkish
        string translatedEra = era;
        if (language == "tr")
        {
            switch (era.ToLower())
            {
                case "stone age":
                    translatedEra = "taş devri";
                    break;
                case "bronze age":
                    translatedEra = "tunç çağı";
                    break;
                case "iron age":
                    translatedEra = "demir çağı";
                    break;
                case "middle ages":
                    translatedEra = "orta çağ";
                    break;
                case "renaissance":
                    translatedEra = "rönesans";
                    break;
                case "industrial age":
                    translatedEra = "sanayi çağı";
                    break;
                case "modern age":
                    translatedEra = "modern çağ";
                    break;
                case "information age":
                    translatedEra = "bilgi çağı";
                    break;
                case "future":
                    translatedEra = "gelecek";
                    break;
            }
        }

        // Log available languages and eras
        Debug.Log("[Android Debug] Available languages and eras:");
        foreach (var lang in loadedLanguages)
        {
            Debug.Log($"[Android Debug] Language: {lang}, Number of eras: {(loadedWords.ContainsKey(lang) ? loadedWords[lang].Count : 0)}");
            if (loadedWords.ContainsKey(lang))
            {
                foreach (var eraKey in loadedWords[lang].Keys)
                {
                    Debug.Log($"[Android Debug]   - Era: {eraKey}, Number of words: {(loadedWords[lang].ContainsKey(eraKey) ? loadedWords[lang][eraKey].Count : 0)}");
                }
            }
        }

        // Check if the language exists in loadedWords
        if (!loadedWords.ContainsKey(language))
        {
            Debug.LogWarning($"[Android Debug] Language '{language}' not found in loadedWords. Available languages: {string.Join(", ", loadedWords.Keys)}");
            return string.Empty;
        }

        // Check if the era exists in the language
        if (!loadedWords[language].ContainsKey(translatedEra.ToLower()))
        {
            Debug.LogWarning($"[Android Debug] Era '{translatedEra}' not found in language '{language}'. Available eras: {string.Join(", ", loadedWords[language].Keys)}");
            return string.Empty;
        }

        // Check if the word exists in the era
        Dictionary<string, Word> eraWords = loadedWords[language][translatedEra.ToLower()];
        if (!eraWords.ContainsKey(word.ToLower()))
        {
            Debug.LogWarning($"[Android Debug] Word '{word}' not found in era '{translatedEra}' for language '{language}'. Available words: {string.Join(", ", eraWords.Keys.Take(10))}{(eraWords.Keys.Count > 10 ? "..." : "")}");
            return string.Empty;
        }

        // Get the fact for the word
        string fact = eraWords[word.ToLower()].didYouKnow;
        
        if (string.IsNullOrEmpty(fact))
        {
            Debug.LogWarning($"[Android Debug] Fact for word '{word}' is empty");
            return string.Empty;
        }
        
        Debug.Log($"[Android Debug] Found fact for word '{word}': {fact.Substring(0, Math.Min(30, fact.Length))}...");
        return fact;
    }
}

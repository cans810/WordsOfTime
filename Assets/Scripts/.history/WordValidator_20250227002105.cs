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
internal class Word
{
    public string word;
    public Translations translations;
    public string difficulty;
    public string[] sentences;
    public string didYouKnow;
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
            
            // Ensure words are loaded into loadedWords dictionary as well
            EnsureWordsAreLoaded();
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
        Debug.Log($"[WordValidator] Loading words for language '{language}' on Android");
        
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, $"words{(language == "en" ? "" : "_" + language)}.json");
        Debug.Log($"[WordValidator] Android JSON file path: {jsonFilePath}");
        
        // Use UnityWebRequest to load the file from StreamingAssets on Android
        yield return StartCoroutine(LoadJsonFromStreamingAssets(jsonFilePath, language));
    }
    
    private static IEnumerator LoadJsonFromStreamingAssets(string filePath, string language)
    {
        Debug.Log($"[WordValidator] Starting to load JSON from {filePath}");
        
        UnityWebRequest www;
        if (filePath.Contains("://"))
        {
            www = UnityWebRequest.Get(filePath);
        }
        else
        {
            www = UnityWebRequest.Get("file://" + filePath);
        }
        
        yield return www.SendWebRequest();
        
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[WordValidator] Error loading JSON file: {www.error}");
            
            // Try alternative path format
            string alternativePath = "jar:file://" + Application.dataPath + "!/assets/" + Path.GetFileName(filePath);
            Debug.Log($"[WordValidator] Trying alternative path: {alternativePath}");
            
            www = UnityWebRequest.Get(alternativePath);
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[WordValidator] Error loading JSON file from alternative path: {www.error}");
                isLoading = false;
                yield break;
            }
        }
        
        string jsonText = www.downloadHandler.text;
        
        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError($"[WordValidator] JSON text is empty for language {language}");
            isLoading = false;
            yield break;
        }
        
        Debug.Log($"[WordValidator] Successfully loaded JSON for language {language}, length: {jsonText.Length}");
        
        try
        {
            // Parse the JSON
            Translations translations = JsonUtility.FromJson<Translations>(jsonText);
            
            if (translations == null || translations.words == null)
            {
                Debug.LogError($"[WordValidator] Failed to parse JSON for language {language}");
                isLoading = false;
                yield break;
            }
            
            Debug.Log($"[WordValidator] Successfully parsed JSON for language {language}, word count: {translations.words.Count}");
            
            // Process the words
            foreach (Word word in translations.words)
            {
                if (string.IsNullOrEmpty(word.era) || string.IsNullOrEmpty(word.word))
                {
                    continue;
                }
                
                string era = word.era.ToLower();
                string wordText = word.word.ToLower();
                
                // Initialize dictionaries if needed
                if (!loadedWords.ContainsKey(language))
                {
                    loadedWords[language] = new Dictionary<string, Dictionary<string, Word>>(System.StringComparer.OrdinalIgnoreCase);
                    loadedLanguages.Add(language);
                }
                
                if (!loadedWords[language].ContainsKey(era))
                {
                    loadedWords[language][era] = new Dictionary<string, Word>(System.StringComparer.OrdinalIgnoreCase);
                }
                
                // Add the word to the dictionary
                loadedWords[language][era][wordText] = word;
                
                // Also add to wordSetsWithFactsByLanguage if it has a didYouKnow field
                if (!string.IsNullOrEmpty(word.didYouKnow))
                {
                    if (!wordSetsWithFactsByLanguage.ContainsKey(language))
                    {
                        wordSetsWithFactsByLanguage[language] = new Dictionary<string, Dictionary<string, string>>();
                    }
                    
                    string originalEra = word.era; // Use original case for era
                    
                    if (!wordSetsWithFactsByLanguage[language].ContainsKey(originalEra))
                    {
                        wordSetsWithFactsByLanguage[language][originalEra] = new Dictionary<string, string>();
                    }
                    
                    string originalWord = word.word.ToUpper(); // Use uppercase for word
                    wordSetsWithFactsByLanguage[language][originalEra][originalWord] = word.didYouKnow;
                }
            }
            
            Debug.Log($"[WordValidator] Successfully loaded words for language {language}");
            
            // Log sample data for verification
            LogSampleData(language);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WordValidator] Error processing JSON for language {language}: {e.Message}\n{e.StackTrace}");
        }
        
        isLoading = false;
    }
    
    private static void LogSampleData(string language)
    {
        if (loadedWords.ContainsKey(language) && loadedWords[language].Count > 0)
        {
            var firstEra = loadedWords[language].Keys.First();
            if (loadedWords[language][firstEra].Count > 0)
            {
                var firstWord = loadedWords[language][firstEra].Values.First();
                Debug.Log($"[WordValidator] Sample data for language {language}: Era: {firstEra}, Word: {firstWord.word}, Definition: {firstWord.definition}, DidYouKnow: {firstWord.didYouKnow}");
            }
        }
        
        if (wordSetsWithFactsByLanguage.ContainsKey(language) && wordSetsWithFactsByLanguage[language].Count > 0)
        {
            var firstEra = wordSetsWithFactsByLanguage[language].Keys.First();
            if (wordSetsWithFactsByLanguage[language][firstEra].Count > 0)
            {
                var firstWordKey = wordSetsWithFactsByLanguage[language][firstEra].Keys.First();
                var fact = wordSetsWithFactsByLanguage[language][firstEra][firstWordKey];
                Debug.Log($"[WordValidator] Sample fact data for language {language}: Era: {firstEra}, Word: {firstWordKey}, Fact: {fact}");
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
            else
            {
                // For non-Android platforms, ensure words are loaded
                EnsureWordsAreLoaded();
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

        // Normalize the era name based on language
        string normalizedEra = era.ToLower();
        
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

        // Try to get fact from loadedWords first
        string fact = TryGetFactFromLoadedWords(word, normalizedEra, language);
        
        // If fact is empty, try to get it from wordSetsWithFactsByLanguage as fallback
        if (string.IsNullOrEmpty(fact))
        {
            Debug.Log($"[Android Debug] Fact not found in loadedWords, trying wordSetsWithFactsByLanguage as fallback");
            fact = TryGetFactFromWordSetsWithFacts(word, era, language);
        }
        
        // If still empty, try to ensure words are loaded and try again
        if (string.IsNullOrEmpty(fact))
        {
            Debug.Log($"[Android Debug] Fact still not found, ensuring words are loaded and trying again");
            EnsureWordsAreLoaded();
            
            // Try loadedWords again
            fact = TryGetFactFromLoadedWords(word, normalizedEra, language);
            
            // If still empty, try wordSetsWithFactsByLanguage one more time
            if (string.IsNullOrEmpty(fact))
            {
                fact = TryGetFactFromWordSetsWithFacts(word, era, language);
            }
        }
        
        if (string.IsNullOrEmpty(fact))
        {
            Debug.LogWarning($"[Android Debug] Fact for word '{word}' is empty after all attempts");
            return string.Empty;
        }
        
        Debug.Log($"[Android Debug] Found fact for word '{word}': {fact.Substring(0, Math.Min(30, fact.Length))}...");
        return fact;
    }

    private static string TryGetFactFromLoadedWords(string word, string normalizedEra, string language)
    {
        // Check if the language exists in loadedWords
        if (!loadedWords.ContainsKey(language))
        {
            Debug.LogWarning($"[Android Debug] Language '{language}' not found in loadedWords. Available languages: {string.Join(", ", loadedWords.Keys)}");
            return string.Empty;
        }

        // Check if the era exists in the language
        if (!loadedWords[language].ContainsKey(normalizedEra))
        {
            Debug.LogWarning($"[Android Debug] Era '{normalizedEra}' not found in language '{language}'. Available eras: {string.Join(", ", loadedWords[language].Keys)}");
            return string.Empty;
        }

        // Get the word key based on language
        string wordKey = word.ToLower();
        
        // Check if the word exists in the era
        Dictionary<string, Word> eraWords = loadedWords[language][normalizedEra];
        if (!eraWords.ContainsKey(wordKey))
        {
            Debug.LogWarning($"[Android Debug] Word '{wordKey}' not found in era '{normalizedEra}' for language '{language}'. Available words: {string.Join(", ", eraWords.Keys.Take(10))}{(eraWords.Keys.Count > 10 ? "..." : "")}");
            return string.Empty;
        }

        // Get the fact for the word
        string fact = eraWords[wordKey].didYouKnow;
        
        if (string.IsNullOrEmpty(fact))
        {
            Debug.LogWarning($"[Android Debug] Fact for word '{wordKey}' is empty in loadedWords");
            return string.Empty;
        }
        
        return fact;
    }

    private static string TryGetFactFromWordSetsWithFacts(string word, string era, string language)
    {
        // Check if wordSetsWithFactsByLanguage is initialized
        if (wordSetsWithFactsByLanguage == null)
        {
            Debug.LogWarning("[Android Debug] wordSetsWithFactsByLanguage is null");
            return string.Empty;
        }
        
        // Check if language exists
        if (!wordSetsWithFactsByLanguage.ContainsKey(language))
        {
            Debug.LogWarning($"[Android Debug] Language '{language}' not found in wordSetsWithFactsByLanguage");
            return string.Empty;
        }
        
        // Check if era exists
        if (!wordSetsWithFactsByLanguage[language].ContainsKey(era))
        {
            Debug.LogWarning($"[Android Debug] Era '{era}' not found in wordSetsWithFactsByLanguage for language '{language}'");
            return string.Empty;
        }
        
        // Try to get the fact using the word (case insensitive)
        string wordKey = word.ToUpper(); // wordSetsWithFactsByLanguage uses uppercase keys
        
        if (wordSetsWithFactsByLanguage[language][era].ContainsKey(wordKey))
        {
            string fact = wordSetsWithFactsByLanguage[language][era][wordKey];
            
            if (!string.IsNullOrEmpty(fact))
            {
                Debug.Log($"[Android Debug] Found fact in wordSetsWithFactsByLanguage for word '{wordKey}': {fact.Substring(0, Math.Min(30, fact.Length))}...");
                return fact;
            }
        }
        
        Debug.LogWarning($"[Android Debug] Word '{wordKey}' not found in wordSetsWithFactsByLanguage for era '{era}' and language '{language}'");
        return string.Empty;
    }

    // Method to ensure words are loaded into the loadedWords dictionary
    private static void EnsureWordsAreLoaded()
    {
        // If loadedWords is empty but wordSetsWithFactsByLanguage has data, populate loadedWords
        if ((loadedWords.Count == 0 || !loadedWords.ContainsKey("en")) && 
            wordSetsWithFactsByLanguage != null && 
            wordSetsWithFactsByLanguage.ContainsKey("en") && 
            wordSetsWithFactsByLanguage["en"].Count > 0)
        {
            Debug.Log("[WordValidator] Populating loadedWords from wordSetsWithFactsByLanguage");
            
            foreach (var language in wordSetsWithFactsByLanguage.Keys)
            {
                if (!loadedWords.ContainsKey(language))
                {
                    loadedWords[language] = new Dictionary<string, Dictionary<string, Word>>(System.StringComparer.OrdinalIgnoreCase);
                    loadedLanguages.Add(language);
                }
                
                foreach (var era in wordSetsWithFactsByLanguage[language].Keys)
                {
                    string normalizedEra = era.ToLower();
                    
                    if (!loadedWords[language].ContainsKey(normalizedEra))
                    {
                        loadedWords[language][normalizedEra] = new Dictionary<string, Word>(System.StringComparer.OrdinalIgnoreCase);
                    }
                    
                    foreach (var wordPair in wordSetsWithFactsByLanguage[language][era])
                    {
                        string wordKey = wordPair.Key.ToLower();
                        
                        // Create a basic Word object with just the didYouKnow field
                        Word word = new Word
                        {
                            word = wordPair.Key,
                            didYouKnow = wordPair.Value
                        };
                        
                        loadedWords[language][normalizedEra][wordKey] = word;
                    }
                }
            }
            
            Debug.Log($"[WordValidator] Successfully populated loadedWords. Languages: {loadedWords.Count}, First language eras: {(loadedWords.ContainsKey("en") ? loadedWords["en"].Count : 0)}");
        }
    }
}

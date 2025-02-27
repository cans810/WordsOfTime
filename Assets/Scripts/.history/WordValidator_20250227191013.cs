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
        #endif
        
        Debug.Log("[WordValidator] Word sets loading initiated");
    }
    
    // Method to initialize word loading on Android platforms
    private static void TryStartAndroidLoading()
    {
        if (GameManager.Instance != null && !didInitiateLoading)
        {
            Debug.Log("[Android Debug] GameManager is available, starting word loading");
            didInitiateLoading = true;
            
            // Start coroutines through GameManager
            GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("en"));
            GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("tr"));
        }
        else if (!didInitiateLoading)
        {
            Debug.Log("[Android Debug] GameManager not available yet, will try again later");
            // Try again in a moment
            if (Application.isPlaying)
            {
                MonoBehaviour mb = GameObject.FindObjectOfType<MonoBehaviour>();
                if (mb != null)
                {
                    mb.StartCoroutine(RetryAndroidLoading());
                }
            }
        }
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    private static IEnumerator LoadWordsForLanguageAndroid(string language)
    {
        Debug.Log($"[Android Debug] LoadWordsForLanguageAndroid started for {language}");
        
        string fileName = language == "en" ? "words.json" : "words_tr.json";
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        
        Debug.Log($"[Android Debug] Loading words from: {filePath}");
        
        // On Android, we need to use UnityWebRequest to access StreamingAssets
        string jsonContent = null;
        bool loadSuccess = false;
        
        // First attempt: Try using UnityWebRequest with the StreamingAssets path
        using (UnityWebRequest webRequest = UnityWebRequest.Get(filePath))
        {
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                jsonContent = webRequest.downloadHandler.text;
                Debug.Log($"[Android Debug] Successfully downloaded {fileName} using UnityWebRequest. Content length: {jsonContent.Length}");
                loadSuccess = true;
            }
            else
            {
                Debug.LogWarning($"[Android Debug] Failed to download {fileName} using UnityWebRequest: {webRequest.error}");
            }
        }
        
        // Second attempt: Try with jar:file:// URL format (specific to Android)
        if (!loadSuccess)
        {
            string androidPath = $"jar:file://{Application.dataPath}!/assets/{fileName}";
            Debug.Log($"[Android Debug] Trying alternative Android path: {androidPath}");
            
            using (UnityWebRequest webRequest = UnityWebRequest.Get(androidPath))
            {
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    jsonContent = webRequest.downloadHandler.text;
                    Debug.Log($"[Android Debug] Successfully downloaded {fileName} using alternative path. Content length: {jsonContent.Length}");
                    loadSuccess = true;
                }
                else
                {
                    Debug.LogWarning($"[Android Debug] Failed to download {fileName} using alternative path: {webRequest.error}");
                }
            }
        }
        
        // Third attempt: Try using Android's AssetManager directly
        if (!loadSuccess)
        {
            Debug.Log($"[Android Debug] Attempting to extract {fileName} using Android AssetManager...");
            
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                {
                    using (AndroidJavaObject assetManager = context.Call<AndroidJavaObject>("getAssets"))
                    {
                        try
                        {
                            using (AndroidJavaObject inputStream = assetManager.Call<AndroidJavaObject>("open", fileName))
                            {
                                // Get the length of the file
                                int length = inputStream.Call<int>("available");
                                Debug.Log($"[Android Debug] Found file in assets with length: {length}");
                                
                                // Create a byte array to store the data
                                byte[] buffer = new byte[length];
                                
                                // Read the data
                                inputStream.Call<int>("read", buffer);
                                
                                // Convert to string
                                jsonContent = System.Text.Encoding.UTF8.GetString(buffer);
                                
                                if (!string.IsNullOrEmpty(jsonContent))
                                {
                                    Debug.Log($"[Android Debug] Successfully extracted file using AssetManager. Content length: {jsonContent.Length}");
                                    loadSuccess = true;
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[Android Debug] Error accessing file with AssetManager: {e.Message}\n{e.StackTrace}");
                        }
                    }
                }
            }
        }
        
        // Process the JSON content if we successfully loaded it
        if (loadSuccess && !string.IsNullOrEmpty(jsonContent))
        {
            ProcessWordJson(jsonContent, language);
            LogSampleData(language);
            
            // Save a copy to persistent data path for future use
            string fallbackPath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            try
            {
                System.IO.File.WriteAllText(fallbackPath, jsonContent);
                Debug.Log($"[Android Debug] Saved a copy of {fileName} to: {fallbackPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Android Debug] Failed to save copy to persistent data path: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[Android Debug] Failed to load {fileName} after all attempts");
        }
        
        // Update loading state
        isLoading = false;
        Debug.Log($"[Android Debug] Finished loading words for {language}");
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
                        string wordKey;
                        
                        if (language == "en")
                        {
                            // For English, use the English word as the key
                            wordKey = wordEntry.word.ToLower();
                            
                            // Log the didYouKnow content for debugging
                            if (!string.IsNullOrEmpty(wordEntry.didYouKnow))
                            {
                                Debug.Log($"[Android Debug] English word '{wordKey}' has didYouKnow: '{wordEntry.didYouKnow.Substring(0, Math.Min(30, wordEntry.didYouKnow.Length))}...'");
                            }
                        }
                        else if (language == "tr")
                        {
                            // For Turkish, use the Turkish translation as the key
                            // Make sure we have a valid Turkish translation
                            if (wordEntry.translations != null && !string.IsNullOrEmpty(wordEntry.translations.tr))
                            {
                                wordKey = wordEntry.translations.tr.ToLower();
                                
                                // Log the didYouKnow content for debugging
                                if (!string.IsNullOrEmpty(wordEntry.didYouKnow))
                                {
                                    Debug.Log($"[Android Debug] Turkish word '{wordKey}' has didYouKnow: '{wordEntry.didYouKnow.Substring(0, Math.Min(30, wordEntry.didYouKnow.Length))}...'");
                                    
                                    // Check if the fact contains Turkish characters
                                    bool containsTurkishChars = wordEntry.didYouKnow.Contains('ç') || 
                                                               wordEntry.didYouKnow.Contains('ğ') || 
                                                               wordEntry.didYouKnow.Contains('ı') || 
                                                               wordEntry.didYouKnow.Contains('ö') || 
                                                               wordEntry.didYouKnow.Contains('ş') || 
                                                               wordEntry.didYouKnow.Contains('ü') || 
                                                               wordEntry.didYouKnow.Contains('İ');
                                    
                                    Debug.Log($"[Android Debug] Turkish word '{wordKey}' fact contains Turkish characters: {containsTurkishChars}");
                                }
                            }
                            else
                            {
                                // If no Turkish translation, use the original word
                                wordKey = wordEntry.word.ToLower();
                                Debug.LogWarning($"[Android Debug] No Turkish translation found for '{wordEntry.word}', using original word as key");
                            }
                        }
                        else
                        {
                            // For other languages, use the original word
                            wordKey = wordEntry.word.ToLower();
                        }
                        
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
        
        // Get the first era
        if (loadedWords[language].Count > 0)
        {
            string firstEra = loadedWords[language].Keys.First();
            Debug.Log($"[Android Debug] First era: {firstEra}");
            
            // Get the first few words from this era
            var eraWords = loadedWords[language][firstEra];
            int wordCount = Math.Min(3, eraWords.Count);
            
            if (wordCount > 0)
            {
                Debug.Log($"[Android Debug] First {wordCount} words from {firstEra}:");
                
                int i = 0;
                foreach (var wordEntry in eraWords)
                {
                    if (i >= wordCount) break;
                    
                    string wordKey = wordEntry.Key;
                    Word word = wordEntry.Value;
                    
                    Debug.Log($"[Android Debug] Word {i+1}: {wordKey}");
                    Debug.Log($"[Android Debug]   - Original word: {word.word}");
                    
                    if (word.translations != null)
                    {
                        Debug.Log($"[Android Debug]   - EN translation: {word.translations.en}");
                        Debug.Log($"[Android Debug]   - TR translation: {word.translations.tr}");
                    }
                    
                    if (word.sentences != null && word.sentences.Length > 0)
                    {
                        Debug.Log($"[Android Debug]   - First sentence: {word.sentences[0]}");
                    }
                    
                    Debug.Log($"[Android Debug]   - Did You Know: {(string.IsNullOrEmpty(word.didYouKnow) ? "MISSING" : word.didYouKnow.Substring(0, Math.Min(30, word.didYouKnow.Length)) + "...")}");
                    Debug.Log($"[Android Debug]   - Difficulty: {word.difficulty}");
                    
                    i++;
                }
            }
            else
            {
                Debug.LogWarning($"[Android Debug] No words found in era: {firstEra}");
            }
        }
        else
        {
            Debug.LogWarning($"[Android Debug] No eras found for language: {language}");
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

        // Normalize the era name based on language
        string normalizedEra = era.ToLower();
        
        // Log available languages and eras
        Debug.Log("[Android Debug] Available languages and eras:");
        foreach (var lang in loadedLanguages)
        {
            if (loadedWords.ContainsKey(lang))
            {
                Debug.Log($"[Android Debug] Language: {lang}, Eras: {string.Join(", ", loadedWords[lang].Keys)}");
            }
        }
        
        // Check if the language exists in the loaded words
        if (!loadedWords.ContainsKey(language))
        {
            Debug.LogWarning($"[Android Debug] Language '{language}' not found in loaded words. Available languages: {string.Join(", ", loadedWords.Keys)}");
            
            // Try to find a matching language with different case
            var possibleMatches = loadedWords.Keys.Where(k => k.Equals(language, StringComparison.OrdinalIgnoreCase)).ToList();
            if (possibleMatches.Any())
            {
                Debug.Log($"[Android Debug] Found possible case-insensitive match for language '{language}': {possibleMatches.First()}");
                language = possibleMatches.First();
            }
            else
            {
                return string.Empty;
            }
        }

        // Check if the era exists in the language
        if (!loadedWords[language].ContainsKey(normalizedEra))
        {
            Debug.LogWarning($"[Android Debug] Era '{normalizedEra}' not found in language '{language}'. Available eras: {string.Join(", ", loadedWords[language].Keys)}");
            
            // Try with different case variations of the era name
            var possibleMatches = loadedWords[language].Keys.Where(k => k.Equals(normalizedEra, StringComparison.OrdinalIgnoreCase)).ToList();
            if (possibleMatches.Any())
            {
                Debug.Log($"[Android Debug] Found possible case-insensitive matches for era '{normalizedEra}': {string.Join(", ", possibleMatches)}");
                normalizedEra = possibleMatches.First();
            }
            else
            {
                return string.Empty;
            }
        }

        // Get the word key based on language
        string wordKey = word.ToLower();
        
        // Check if the word exists in the era
        Dictionary<string, Word> eraWords = loadedWords[language][normalizedEra];
        
        // Log all words in this era for debugging
        Debug.Log($"[Android Debug] All words in era '{normalizedEra}' for language '{language}': {string.Join(", ", eraWords.Keys.Take(20))}{(eraWords.Keys.Count > 20 ? "..." : "")}");
        
        if (!eraWords.ContainsKey(wordKey))
        {
            Debug.LogWarning($"[Android Debug] Word '{wordKey}' not found in era '{normalizedEra}' for language '{language}'.");
            
            // Try with different case variations of the word
            var possibleMatches = eraWords.Keys.Where(k => k.Equals(wordKey, StringComparison.OrdinalIgnoreCase)).ToList();
            if (possibleMatches.Any())
            {
                Debug.Log($"[Android Debug] Found possible case-insensitive matches for word '{wordKey}': {string.Join(", ", possibleMatches)}");
                wordKey = possibleMatches.First();
            }
            else
            {
                // If we're in Turkish language, try to find the word in the English data and get its Turkish translation
                if (language == "tr")
                {
                    Debug.Log($"[Android Debug] Trying to find Turkish translation for word '{wordKey}' in English data");
                    
                    // Check if English data is available
                    if (loadedWords.ContainsKey("en") && loadedWords["en"].ContainsKey(normalizedEra))
                    {
                        // Look for the word in English data
                        foreach (var englishWord in loadedWords["en"][normalizedEra])
                        {
                            // Check if this English word has a Turkish translation matching our target word
                            if (englishWord.Value.translations != null && 
                                !string.IsNullOrEmpty(englishWord.Value.translations.tr) && 
                                englishWord.Value.translations.tr.ToLower() == wordKey)
                            {
                                // Found the English word with matching Turkish translation
                                Debug.Log($"[Android Debug] Found English word '{englishWord.Key}' with Turkish translation '{wordKey}'");
                                
                                // Get the fact from the English word
                                string factFromEnglish = englishWord.Value.didYouKnow;
                                if (!string.IsNullOrEmpty(factFromEnglish))
                                {
                                    Debug.Log($"[Android Debug] Using fact from English word: {factFromEnglish.Substring(0, Math.Min(30, factFromEnglish.Length))}...");
                                    return factFromEnglish;
                                }
                            }
                        }
                    }
                }
                
                return string.Empty;
            }
        }

        // Get the fact for the word
        string fact = eraWords[wordKey].didYouKnow;
        
        // Log the fact for debugging
        if (!string.IsNullOrEmpty(fact))
        {
            Debug.Log($"[Android Debug] Raw fact for word '{wordKey}' in {language}: {fact.Substring(0, Math.Min(30, fact.Length))}...");
        }
        else
        {
            Debug.LogWarning($"[Android Debug] Fact for word '{wordKey}' in {language} is empty or null");
        }
        
        // Check if the fact is in the correct language
        bool isFactInCorrectLanguage = true;
        
        // Simple language detection - check if the fact contains language-specific characters
        if (language == "en" && fact != null)
        {
            // Check if the fact contains Turkish-specific characters
            if (fact.Contains('ç') || fact.Contains('ğ') || fact.Contains('ı') || fact.Contains('ö') || fact.Contains('ş') || fact.Contains('ü') || fact.Contains('İ'))
            {
                Debug.LogWarning($"[Android Debug] Fact for English word '{wordKey}' appears to be in Turkish");
                isFactInCorrectLanguage = false;
            }
        }
        else if (language == "tr" && fact != null)
        {
            // For Turkish, we expect to see Turkish-specific characters
            if (!fact.Contains('ç') && !fact.Contains('ğ') && !fact.Contains('ı') && !fact.Contains('ö') && !fact.Contains('ş') && !fact.Contains('ü') && !fact.Contains('İ'))
            {
                Debug.LogWarning($"[Android Debug] Fact for Turkish word '{wordKey}' appears to be in English");
                isFactInCorrectLanguage = false;
            }
        }
        
        if (string.IsNullOrEmpty(fact) || !isFactInCorrectLanguage)
        {
            Debug.LogWarning($"[Android Debug] Fact for word '{wordKey}' is empty or in wrong language");
            
            // Try to find a fact in the correct language
            if (language == "en" && loadedWords.ContainsKey("tr") && loadedWords["tr"].ContainsKey(normalizedEra))
            {
                // Look for the Turkish word that has this English word as its translation
                foreach (var turkishWord in loadedWords["tr"][normalizedEra])
                {
                    if (turkishWord.Value.translations != null && 
                        !string.IsNullOrEmpty(turkishWord.Value.translations.en) && 
                        turkishWord.Value.translations.en.ToLower().Equals(wordKey, StringComparison.OrdinalIgnoreCase))
                    {
                        // Found the Turkish word with matching English translation
                        Debug.Log($"[Android Debug] Found Turkish word '{turkishWord.Key}' with English translation '{wordKey}'");
                        
                        // Get the fact from the Turkish word
                        string factFromTurkish = turkishWord.Value.didYouKnow;
                        
                        // Check if this fact is in English (simple check)
                        if (!string.IsNullOrEmpty(factFromTurkish) && 
                            !factFromTurkish.Contains('ç') && !factFromTurkish.Contains('ğ') && 
                            !factFromTurkish.Contains('ı') && !factFromTurkish.Contains('ö') && 
                            !factFromTurkish.Contains('ş') && !factFromTurkish.Contains('ü') && 
                            !factFromTurkish.Contains('İ'))
                        {
                            Debug.Log($"[Android Debug] Using English fact from Turkish word: {factFromTurkish.Substring(0, Math.Min(30, factFromTurkish.Length))}...");
                            return factFromTurkish;
                        }
                    }
                }
            }
            else if (language == "tr" && loadedWords.ContainsKey("en") && loadedWords["en"].ContainsKey(normalizedEra))
            {
                // Find the English word that has this Turkish translation
                foreach (var englishWord in loadedWords["en"][normalizedEra])
                {
                    if (englishWord.Value.translations != null && 
                        !string.IsNullOrEmpty(englishWord.Value.translations.tr) && 
                        englishWord.Value.translations.tr.ToLower().Equals(wordKey, StringComparison.OrdinalIgnoreCase))
                    {
                        // Found the English word with matching Turkish translation
                        Debug.Log($"[Android Debug] Found English word '{englishWord.Key}' with Turkish translation '{wordKey}'");
                        
                        // Get the fact from the English word
                        string factFromEnglish = englishWord.Value.didYouKnow;
                        
                        // Check if this fact is in Turkish (simple check)
                        if (!string.IsNullOrEmpty(factFromEnglish) && 
                            (factFromEnglish.Contains('ç') || factFromEnglish.Contains('ğ') || 
                             factFromEnglish.Contains('ı') || factFromEnglish.Contains('ö') || 
                             factFromEnglish.Contains('ş') || factFromEnglish.Contains('ü') || 
                             factFromEnglish.Contains('İ')))
                        {
                            Debug.Log($"[Android Debug] Using Turkish fact from English word: {factFromEnglish.Substring(0, Math.Min(30, factFromEnglish.Length))}...");
                            return factFromEnglish;
                        }
                    }
                }
            }
            
            // If we still have a fact but it's in the wrong language, return it anyway as a fallback
            if (!string.IsNullOrEmpty(fact))
            {
                Debug.LogWarning($"[Android Debug] Using fact in wrong language as fallback: {fact.Substring(0, Math.Min(30, fact.Length))}...");
                return fact;
            }
            
            return string.Empty;
        }
        
        Debug.Log($"[Android Debug] Found fact for word '{wordKey}' in {language}: {fact.Substring(0, Math.Min(30, fact.Length))}...");
        return fact;
    }

    public static void CheckWordInJsonFiles(string word, string era)
    {
        Debug.Log($"[WordValidator] Checking for word '{word}' in era '{era}' in JSON files");
        
        // Check if the word exists in the English JSON file
        string enFilePath = Path.Combine(Application.streamingAssetsPath, "words.json");
        if (File.Exists(enFilePath))
        {
            Debug.Log($"[WordValidator] English words file exists at: {enFilePath}");
            try
            {
                string json = File.ReadAllText(enFilePath);
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);
                
                if (wordSetList?.sets != null)
                {
                    foreach (var wordSet in wordSetList.sets)
                    {
                        if (wordSet.era.Equals(era, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.Log($"[WordValidator] Found era '{era}' in English JSON file with {wordSet.words.Length} words");
                            
                            foreach (var wordEntry in wordSet.words)
                            {
                                if (wordEntry.word.Equals(word, StringComparison.OrdinalIgnoreCase))
                                {
                                    Debug.Log($"[WordValidator] FOUND WORD '{word}' in era '{era}' in English JSON file!");
                                    Debug.Log($"[WordValidator] Did You Know content: '{(string.IsNullOrEmpty(wordEntry.didYouKnow) ? "EMPTY" : wordEntry.didYouKnow.Substring(0, Math.Min(30, wordEntry.didYouKnow.Length)) + "...")}'");
                                    
                                    // Ensure the word and fact are properly stored in memory
                                    string normalizedEra = era.ToLower();
                                    string normalizedWord = word.ToLower();
                                    
                                    // Make sure the language, era, and word dictionaries exist
                                    if (!loadedWords.ContainsKey("en"))
                                    {
                                        loadedWords["en"] = new Dictionary<string, Dictionary<string, Word>>();
                                    }
                                    
                                    if (!loadedWords["en"].ContainsKey(normalizedEra))
                                    {
                                        loadedWords["en"][normalizedEra] = new Dictionary<string, Word>();
                                    }
                                    
                                    // Store or update the word entry
                                    Word wordObj = new Word
                                    {
                                        word = wordEntry.word,
                                        translations = wordEntry.translations,
                                        difficulty = wordEntry.difficulty,
                                        sentences = wordEntry.sentences,
                                        didYouKnow = wordEntry.didYouKnow
                                    };
                                    loadedWords["en"][normalizedEra][normalizedWord] = wordObj;
                                    
                                    Debug.Log($"[WordValidator] Successfully stored fact for word '{word}' in memory");
                                    return;
                                }
                            }
                            
                            Debug.LogWarning($"[WordValidator] Word '{word}' NOT FOUND in era '{era}' in English JSON file");
                            
                            // Log some sample words from this era
                            var sampleWords = wordSet.words.Take(5).Select(w => w.word).ToList();
                            Debug.Log($"[WordValidator] Sample words in this era: {string.Join(", ", sampleWords)}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WordValidator] Error reading English JSON file: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[WordValidator] English words file not found at: {enFilePath}");
        }
        
        // Check if the word exists in the Turkish JSON file
        string trFilePath = Path.Combine(Application.streamingAssetsPath, "words_tr.json");
        if (File.Exists(trFilePath))
        {
            Debug.Log($"[WordValidator] Turkish words file exists at: {trFilePath}");
            try
            {
                string json = File.ReadAllText(trFilePath);
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);
                
                if (wordSetList?.sets != null)
                {
                    foreach (var wordSet in wordSetList.sets)
                    {
                        // Map Turkish era name to English if needed
                        string mappedEra = MapTurkishEraToEnglish(wordSet.era);
                        
                        if (mappedEra.Equals(era, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.Log($"[WordValidator] Found era '{era}' (as '{wordSet.era}') in Turkish JSON file with {wordSet.words.Length} words");
                            
                            foreach (var wordEntry in wordSet.words)
                            {
                                // Check both the original word and its translations
                                if (wordEntry.word.Equals(word, StringComparison.OrdinalIgnoreCase) ||
                                    (wordEntry.translations != null && 
                                     (wordEntry.translations.en.Equals(word, StringComparison.OrdinalIgnoreCase) ||
                                      wordEntry.translations.tr.Equals(word, StringComparison.OrdinalIgnoreCase))))
                                {
                                    Debug.Log($"[WordValidator] FOUND WORD '{word}' in era '{era}' in Turkish JSON file!");
                                    Debug.Log($"[WordValidator] Original word: '{wordEntry.word}'");
                                    if (wordEntry.translations != null)
                                    {
                                        Debug.Log($"[WordValidator] EN translation: '{wordEntry.translations.en}'");
                                        Debug.Log($"[WordValidator] TR translation: '{wordEntry.translations.tr}'");
                                    }
                                    Debug.Log($"[WordValidator] Did You Know content: '{(string.IsNullOrEmpty(wordEntry.didYouKnow) ? "EMPTY" : wordEntry.didYouKnow.Substring(0, Math.Min(30, wordEntry.didYouKnow.Length)) + "...")}'");
                                    
                                    // Ensure the word and fact are properly stored in memory for Turkish language
                                    string normalizedEra = era.ToLower();
                                    
                                    // Make sure the language, era, and word dictionaries exist for Turkish
                                    if (!loadedWords.ContainsKey("tr"))
                                    {
                                        loadedWords["tr"] = new Dictionary<string, Dictionary<string, Word>>();
                                        loadedLanguages.Add("tr");
                                        Debug.Log("[WordValidator] Created 'tr' language entry in loadedWords dictionary");
                                    }
                                    
                                    if (!loadedWords["tr"].ContainsKey(normalizedEra))
                                    {
                                        loadedWords["tr"][normalizedEra] = new Dictionary<string, Word>();
                                        Debug.Log($"[WordValidator] Created '{normalizedEra}' era entry for 'tr' language");
                                    }
                                    
                                    // Store the word using the Turkish translation as the key
                                    string wordKey = wordEntry.translations != null && !string.IsNullOrEmpty(wordEntry.translations.tr) 
                                        ? wordEntry.translations.tr.ToLower() 
                                        : word.ToLower();
                                    
                                    // Store or update the word entry
                                    Word wordObj = new Word
                                    {
                                        word = wordEntry.word,
                                        translations = wordEntry.translations,
                                        difficulty = wordEntry.difficulty,
                                        sentences = wordEntry.sentences,
                                        didYouKnow = wordEntry.didYouKnow
                                    };
                                    
                                    loadedWords["tr"][normalizedEra][wordKey] = wordObj;
                                    
                                    Debug.Log($"[WordValidator] Successfully stored Turkish fact for word '{wordKey}' in memory");
                                    
                                    // Also store in English dictionary if it has an English translation
                                    if (wordEntry.translations != null && !string.IsNullOrEmpty(wordEntry.translations.en))
                                    {
                                        if (!loadedWords.ContainsKey("en"))
                                        {
                                            loadedWords["en"] = new Dictionary<string, Dictionary<string, Word>>();
                                            loadedLanguages.Add("en");
                                        }
                                        
                                        if (!loadedWords["en"].ContainsKey(normalizedEra))
                                        {
                                            loadedWords["en"][normalizedEra] = new Dictionary<string, Word>();
                                        }
                                        
                                        string enWordKey = wordEntry.translations.en.ToLower();
                                        loadedWords["en"][normalizedEra][enWordKey] = wordObj;
                                        Debug.Log($"[WordValidator] Also stored fact for English translation '{enWordKey}'");
                                    }
                                    
                                    return;
                                }
                            }
                            
                            Debug.LogWarning($"[WordValidator] Word '{word}' NOT FOUND in era '{era}' in Turkish JSON file");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WordValidator] Error reading Turkish JSON file: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[WordValidator] Turkish words file not found at: {trFilePath}");
        }
        
        Debug.LogError($"[WordValidator] Word '{word}' NOT FOUND in any JSON file for era '{era}'");
    }

    // Add a method to verify Turkish facts are properly loaded
    public static void VerifyTurkishFacts()
    {
        Debug.Log("[Android Debug] Verifying Turkish facts...");
        
        if (isLoading)
        {
            Debug.LogWarning("[Android Debug] Cannot verify Turkish facts - still loading data");
            return;
        }
        
        if (!loadedWords.ContainsKey("tr"))
        {
            Debug.LogError("[Android Debug] Turkish language data not found in loadedWords!");
            return;
        }
        
        int totalWords = 0;
        int wordsWithFacts = 0;
        int wordsWithTurkishFacts = 0;
        
        foreach (var era in loadedWords["tr"].Keys)
        {
            Debug.Log($"[Android Debug] Checking era: {era}");
            
            var eraWords = loadedWords["tr"][era];
            totalWords += eraWords.Count;
            
            foreach (var wordEntry in eraWords)
            {
                string wordKey = wordEntry.Key;
                Word word = wordEntry.Value;
                
                if (!string.IsNullOrEmpty(word.didYouKnow))
                {
                    wordsWithFacts++;
                    
                    // Check if the fact contains Turkish-specific characters
                    bool containsTurkishChars = word.didYouKnow.Contains('ç') || 
                                               word.didYouKnow.Contains('ğ') || 
                                               word.didYouKnow.Contains('ı') || 
                                               word.didYouKnow.Contains('ö') || 
                                               word.didYouKnow.Contains('ş') || 
                                               word.didYouKnow.Contains('ü') || 
                                               word.didYouKnow.Contains('İ');
                    
                    if (containsTurkishChars)
                    {
                        wordsWithTurkishFacts++;
                    }
                    else
                    {
                        Debug.LogWarning($"[Android Debug] Turkish word '{wordKey}' has a fact without Turkish characters: '{word.didYouKnow.Substring(0, Math.Min(30, word.didYouKnow.Length))}...'");
                    }
                }
            }
        }
        
        Debug.Log($"[Android Debug] Turkish verification complete: {totalWords} total words, {wordsWithFacts} with facts, {wordsWithTurkishFacts} with Turkish facts");
        
        // If we have very few Turkish facts, try to find them in the English data
        if (wordsWithTurkishFacts < totalWords * 0.1f && loadedWords.ContainsKey("en"))
        {
            Debug.LogWarning("[Android Debug] Very few Turkish facts found. Checking English data for Turkish facts...");
            
            int turkishFactsInEnglishData = 0;
            
            foreach (var era in loadedWords["en"].Keys)
            {
                var eraWords = loadedWords["en"][era];
                
                foreach (var wordEntry in eraWords)
                {
                    Word word = wordEntry.Value;
                    
                    if (!string.IsNullOrEmpty(word.didYouKnow))
                    {
                        // Check if the fact contains Turkish-specific characters
                        bool containsTurkishChars = word.didYouKnow.Contains('ç') || 
                                                   word.didYouKnow.Contains('ğ') || 
                                                   word.didYouKnow.Contains('ı') || 
                                                   word.didYouKnow.Contains('ö') || 
                                                   word.didYouKnow.Contains('ş') || 
                                                   word.didYouKnow.Contains('ü') || 
                                                   word.didYouKnow.Contains('İ');
                        
                        if (containsTurkishChars)
                        {
                            turkishFactsInEnglishData++;
                            Debug.Log($"[Android Debug] Found Turkish fact in English data for word '{wordEntry.Key}': '{word.didYouKnow.Substring(0, Math.Min(30, word.didYouKnow.Length))}...'");
                        }
                    }
                }
            }
            
            Debug.Log($"[Android Debug] Found {turkishFactsInEnglishData} Turkish facts in English data");
        }
    }

    // Call this method from GameManager after loading is complete
    public static void VerifyFactsAfterLoading()
    {
        if (isLoading)
        {
            Debug.Log("[Android Debug] Still loading data, will verify facts later");
            GameManager.Instance.Invoke("VerifyFactsAfterLoadingDelayed", 2.0f);
        }
        else
        {
            VerifyTurkishFacts();
        }
    }

    private static IEnumerator RetryAndroidLoading()
    {
        Debug.Log("[Android Debug] Waiting to retry Android loading...");
        yield return new WaitForSeconds(1.0f);
        
        if (GameManager.Instance != null)
        {
            Debug.Log("[Android Debug] GameManager is now available, starting word loading");
            didInitiateLoading = true;
            isLoading = true;
            
            // Start coroutines through GameManager
            GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("en"));
            GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("tr"));
        }
        else
        {
            Debug.Log("[Android Debug] GameManager still not available, will try again");
            // Try again with another MonoBehaviour
            MonoBehaviour mb = GameObject.FindObjectOfType<MonoBehaviour>();
            if (mb != null)
            {
                mb.StartCoroutine(RetryAndroidLoading());
            }
            else
            {
                Debug.LogError("[Android Debug] Cannot find any MonoBehaviour to start coroutines!");
            }
        }
    }
}

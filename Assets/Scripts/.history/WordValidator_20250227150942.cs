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
                
                // Fallback: Try to load from a different path or method
                Debug.Log($"[Android Debug] Attempting fallback loading method for {language}...");
                
                // For Android 11+ (API level 30+), we might need a different approach
                // Try using the Application.persistentDataPath as a fallback
                string fallbackPath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                
                if (System.IO.File.Exists(fallbackPath))
                {
                    Debug.Log($"[Android Debug] Found fallback file at: {fallbackPath}");
                    string jsonContent = System.IO.File.ReadAllText(fallbackPath);
                    
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        ProcessWordJson(jsonContent, language);
                        LogSampleData(language);
                    }
                }
                else
                {
                    Debug.LogError($"[Android Debug] Fallback file not found at: {fallbackPath}");
                }
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

    public static string GetFactForWord(string word, string era, string language = "en")
    {
        Debug.Log($"[WordValidator] GetFactForWord called for word: {word}, era: {era}, language: {language}");
        
        if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(era))
        {
            Debug.LogWarning($"[WordValidator] GetFactForWord called with invalid parameters: word='{word}', era='{era}', language='{language}'");
            return string.Empty;
        }
        
        // Normalize inputs
        string normalizedWord = word.ToLower();
        string normalizedEra = era.ToLower();
        
        // Check if the language exists in the dictionary
        if (!wordSetsWithFactsByLanguage.ContainsKey(language))
        {
            Debug.LogWarning($"[WordValidator] Language '{language}' not found in wordSetsWithFactsByLanguage");
            
            // If language is not English, try English as fallback
            if (language != "en" && wordSetsWithFactsByLanguage.ContainsKey("en"))
            {
                Debug.Log($"[WordValidator] Trying English as fallback for language '{language}'");
                language = "en";
            }
            else
            {
                return string.Empty;
            }
        }
        
        // Check if the era exists for this language
        if (!wordSetsWithFactsByLanguage[language].ContainsKey(normalizedEra))
        {
            Debug.LogWarning($"[WordValidator] Era '{normalizedEra}' not found for language '{language}'");
            
            // Try with different case variations of the era name
            var possibleMatches = wordSetsWithFactsByLanguage[language].Keys
                .Where(k => k.Equals(normalizedEra, StringComparison.OrdinalIgnoreCase))
                .ToList();
                
            if (possibleMatches.Any())
            {
                Debug.Log($"[WordValidator] Found possible case-insensitive match for era '{normalizedEra}': {possibleMatches.First()}");
                normalizedEra = possibleMatches.First();
            }
            else
            {
                return string.Empty;
            }
        }
        
        // Check if the word exists in this era
        if (wordSetsWithFactsByLanguage[language][normalizedEra].ContainsKey(normalizedWord))
        {
            string fact = wordSetsWithFactsByLanguage[language][normalizedEra][normalizedWord];
            Debug.Log($"[WordValidator] Found fact for word '{normalizedWord}' in era '{normalizedEra}' and language '{language}'");
            return fact;
        }
        
        // Try with different case variations of the word
        var wordMatches = wordSetsWithFactsByLanguage[language][normalizedEra].Keys
            .Where(k => k.Equals(normalizedWord, StringComparison.OrdinalIgnoreCase))
            .ToList();
            
        if (wordMatches.Any())
        {
            string matchedWord = wordMatches.First();
            Debug.Log($"[WordValidator] Found case-insensitive match for word '{normalizedWord}': {matchedWord}");
            return wordSetsWithFactsByLanguage[language][normalizedEra][matchedWord];
        }
        
        // If language is not English and no fact was found, try English as fallback
        if (language != "en" && wordSetsWithFactsByLanguage.ContainsKey("en"))
        {
            Debug.Log($"[WordValidator] No fact found in '{language}', trying English as fallback for word '{normalizedWord}'");
            return GetFactForWord(word, era, "en");
        }
        
        Debug.LogWarning($"[WordValidator] No fact found for word '{normalizedWord}' in era '{normalizedEra}' and language '{language}'");
        return string.Empty;
    }

    public static void CheckWordInJsonFiles(string word, string era, string language = "en")
    {
        Debug.Log($"[WordValidator] Checking for word '{word}' in era '{era}' in {language} JSON files");
        
        // Determine which file to check first based on language
        bool checkTurkishFirst = language == "tr";
        
        if (checkTurkishFirst)
        {
            // Check Turkish file first, then English as fallback
            bool foundInTurkish = CheckWordInTurkishJsonFile(word, era);
            if (!foundInTurkish)
            {
                CheckWordInEnglishJsonFile(word, era);
            }
        }
        else
        {
            // Check English file first, then Turkish if needed
            bool foundInEnglish = CheckWordInEnglishJsonFile(word, era);
            if (!foundInEnglish)
            {
                CheckWordInTurkishJsonFile(word, era);
            }
        }
    }
    
    private static bool CheckWordInEnglishJsonFile(string word, string era)
    {
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
                                    if (!wordSetsWithFactsByLanguage["en"].ContainsKey(normalizedEra))
                                    {
                                        wordSetsWithFactsByLanguage["en"][normalizedEra] = new Dictionary<string, string>();
                                    }
                                    
                                    // Store or update the word entry
                                    wordSetsWithFactsByLanguage["en"][normalizedEra][normalizedWord] = wordEntry.didYouKnow;
                                    
                                    Debug.Log($"[WordValidator] Successfully stored fact for word '{word}' in memory (English)");
                                    return true;
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
        
        return false;
    }
    
    private static bool CheckWordInTurkishJsonFile(string word, string era)
    {
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
                                    
                                    // Store the Turkish fact in memory
                                    string normalizedEra = era.ToLower();
                                    string normalizedWord = word.ToLower();
                                    
                                    // Make sure the language, era, and word dictionaries exist
                                    if (!wordSetsWithFactsByLanguage["tr"].ContainsKey(normalizedEra))
                                    {
                                        wordSetsWithFactsByLanguage["tr"][normalizedEra] = new Dictionary<string, string>();
                                    }
                                    
                                    // Store or update the word entry
                                    wordSetsWithFactsByLanguage["tr"][normalizedEra][normalizedWord] = wordEntry.didYouKnow;
                                    
                                    Debug.Log($"[WordValidator] Successfully stored fact for word '{word}' in memory (Turkish)");
                                    return true;
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
        
        return false;
    }
}

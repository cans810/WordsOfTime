using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;

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
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    private static void TryStartAndroidLoading()
    {
        if (didInitiateLoading) return; // Avoid multiple attempts
        
        if (GameManager.Instance != null)
        {
            Debug.Log("[WordValidator] GameManager instance found, starting to load words on Android");
            GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("en"));
            GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("tr"));
            didInitiateLoading = true;
        }
        else
        {
            Debug.LogWarning("[WordValidator] GameManager.Instance is null, will try again later to load words on Android");
            // Will be called again later from GetFactForWord when needed
        }
    }
    
    private static IEnumerator LoadWordsForLanguageAndroid(string language)
    {
        isLoading = true;
        Debug.Log($"[WordValidator] Loading words for language: {language} on Android");
        string fileName = language == "en" ? "words.json" : $"words_{language}.json";
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        
        Debug.Log($"[WordValidator] Attempting to load file from: {filePath}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(filePath))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"[WordValidator] Successfully downloaded JSON for {language}, length: {json.Length}");
                ProcessJsonContent(json, language);
                Debug.Log($"[WordValidator] Successfully loaded {language} words");
                
                // Log some sample data for verification
                if (wordSetsWithFactsByLanguage.ContainsKey(language))
                {
                    Debug.Log($"[WordValidator] Loaded eras for {language}: {string.Join(", ", wordSetsWithFactsByLanguage[language].Keys)}");
                    foreach (var era in wordSetsWithFactsByLanguage[language].Keys)
                    {
                        Debug.Log($"[WordValidator] Era '{era}' has {wordSetsWithFactsByLanguage[language][era].Count} facts");
                        // Log a few examples of facts if available
                        int count = 0;
                        foreach (var word in wordSetsWithFactsByLanguage[language][era].Keys)
                        {
                            if (count < 3) // Log only first 3 facts as examples
                            {
                                Debug.Log($"[WordValidator] Sample fact for word '{word}': {wordSetsWithFactsByLanguage[language][era][word].Substring(0, Mathf.Min(50, wordSetsWithFactsByLanguage[language][era][word].Length))}...");
                                count++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"[WordValidator] Failed to load {language} words: {request.error}, URL: {filePath}");
            }
        }
        
        isLoading = false;
        Debug.Log($"[WordValidator] Finished loading {language} words, isLoading set to false");
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

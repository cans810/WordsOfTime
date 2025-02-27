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
    private static bool didInitiateLoading = false;

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

    public static string GetFactForWord(string word, string era, string language = "en")
    {
        Debug.Log($"[WordValidator] Getting fact for word: '{word}', era: '{era}', language: '{language}'");
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Try to force initialization if it hasn't happened yet
        if (!didInitiateLoading && GameManager.Instance != null)
        {
            Debug.Log("[WordValidator] Force initializing word loading on Android");
            GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("en"));
            GameManager.Instance.StartCoroutine(LoadWordsForLanguageAndroid("tr"));
            didInitiateLoading = true;
            
            // Return LOADING status since we just now started loading
            return "LOADING";
        }
        else if (!didInitiateLoading)
        {
            Debug.LogWarning("[WordValidator] GameManager.Instance is null, can't start loading on Android!");
            return "LOADING";
        }
        
        // Check if we're still loading the words on Android
        if (isLoading)
        {
            Debug.LogWarning($"[WordValidator] Facts are still loading for {language}. Cannot retrieve fact for '{word}' yet.");
            return "LOADING"; // Special return value to indicate loading state
        }
        #endif
        
        if (!wordSetsWithFactsByLanguage.ContainsKey(language))
        {
            Debug.LogWarning($"[WordValidator] No facts dictionary found for language: {language}");
            return string.Empty;
        }

        // Logging to help debug
        Debug.Log($"[WordValidator] Loaded languages: {string.Join(", ", wordSetsWithFactsByLanguage.Keys)}");
        foreach (var lang in wordSetsWithFactsByLanguage.Keys)
        {
            Debug.Log($"[WordValidator] {lang} has {wordSetsWithFactsByLanguage[lang].Count} eras: {string.Join(", ", wordSetsWithFactsByLanguage[lang].Keys)}");
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

        Debug.Log($"[WordValidator] Looking for era: '{translatedEra}' in {language}");
        Debug.Log($"[WordValidator] Available eras in {language}: {string.Join(", ", wordSetsWithFactsByLanguage[language].Keys)}");

        if (!wordSetsWithFactsByLanguage[language].ContainsKey(translatedEra))
        {
            Debug.LogWarning($"[WordValidator] No facts found for era: '{translatedEra}' in language: {language}. Available eras: {string.Join(", ", wordSetsWithFactsByLanguage[language].Keys)}");
            return string.Empty;
        }

        string wordKey = word.ToUpper();
        Debug.Log($"[WordValidator] Available words in era '{translatedEra}': {string.Join(", ", wordSetsWithFactsByLanguage[language][translatedEra].Keys)}");
        
        if (wordSetsWithFactsByLanguage[language][translatedEra].ContainsKey(wordKey))
        {
            string fact = wordSetsWithFactsByLanguage[language][translatedEra][wordKey];
            Debug.Log($"[WordValidator] Found fact for word '{wordKey}': {fact.Substring(0, Mathf.Min(50, fact.Length))}...");
            return fact;
        }

        Debug.LogWarning($"[WordValidator] No fact found for word: '{wordKey}' in era: '{translatedEra}', language: {language}");
        return string.Empty;
    }
}

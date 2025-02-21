using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> wordSetsWithFactsByLanguage;

    static WordValidator()
    {
        LoadWordSets();
    }

    private static void LoadWordSets()
    {
        // Initialize the dictionary to store facts for different languages
        wordSetsWithFactsByLanguage = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        wordSetsWithFactsByLanguage["en"] = new Dictionary<string, Dictionary<string, string>>();
        wordSetsWithFactsByLanguage["tr"] = new Dictionary<string, Dictionary<string, string>>();

        // Load English words
        LoadWordsForLanguage("en");
        // Load Turkish words
        LoadWordsForLanguage("tr");
    }

    private static void LoadWordsForLanguage(string language)
    {
        string fileName = language == "en" ? "words.json" : $"words_{language}.json";
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!System.IO.File.Exists(filePath)) return;

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

            if (wordSetList?.sets != null)
            {
                wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                
                foreach (var wordSet in wordSetList.sets)
                {
                    var wordDict = new Dictionary<string, List<string>>();
                    var factDict = new Dictionary<string, string>();
                    
                    foreach (var wordEntry in wordSet.words)
                    {
                        wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences);
                        if (!string.IsNullOrEmpty(wordEntry.didYouKnow))
                        {
                            factDict[wordEntry.word.ToUpper()] = wordEntry.didYouKnow;
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
            Debug.LogError($"Error parsing JSON file for {language}: {e.Message}");
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
        // Check if we have facts for this language and era
        if (!wordSetsWithFactsByLanguage.ContainsKey(language) || 
            !wordSetsWithFactsByLanguage[language].ContainsKey(era))
            return string.Empty;

        var facts = wordSetsWithFactsByLanguage[language][era];
        
        // Try to get the fact directly
        if (facts.ContainsKey(word.ToUpper()))
        {
            return facts[word.ToUpper()];
        }
        
        // If not found and it's Turkish, try to get the Turkish translation
        if (language == "tr")
        {
            string baseWord = GameManager.Instance.GetBaseWord(word);
            string turkishWord = GameManager.Instance.GetTranslation(baseWord, "tr");
            if (facts.ContainsKey(turkishWord.ToUpper()))
            {
                return facts[turkishWord.ToUpper()];
            }
        }
        
        // If still not found and not English, try English as fallback
        if (language != "en")
        {
            string baseWord = GameManager.Instance.GetBaseWord(word);
            if (wordSetsWithFactsByLanguage["en"].ContainsKey(era) &&
                wordSetsWithFactsByLanguage["en"][era].ContainsKey(baseWord.ToUpper()))
            {
                return wordSetsWithFactsByLanguage["en"][era][baseWord.ToUpper()];
            }
        }
        
        return string.Empty;
    }
}

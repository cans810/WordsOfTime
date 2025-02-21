using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    private static Dictionary<string, Dictionary<string, string>> wordSetsWithFacts;

    static WordValidator()
    {
        LoadWordSets();
    }

    private static void LoadWordSets()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "words.json");
        if (!System.IO.File.Exists(filePath)) return;

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

            if (wordSetList?.sets != null)
            {
                wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                wordSetsWithFacts = new Dictionary<string, Dictionary<string, string>>();

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
                    wordSetsWithFacts[wordSet.era] = factDict;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON file: {e.Message}");
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
        if (wordSetsWithFacts == null || !wordSetsWithFacts.ContainsKey(era))
            return string.Empty;

        // Check if the current language is Turkish
        if (GameManager.Instance.CurrentLanguage == "tr")
        {
            // If the word is already in Turkish, try to get the fact directly
            if (wordSetsWithFacts[era].ContainsKey(word.ToUpper()))
            {
                return wordSetsWithFacts[era][word.ToUpper()];
            }
        }
        
        // For other languages, get the base word first
        string baseWord = GameManager.Instance.GetBaseWord(word);
        
        // Fallback to English fact
        if (wordSetsWithFacts[era].ContainsKey(baseWord.ToUpper()))
        {
            return wordSetsWithFacts[era][baseWord.ToUpper()];
        }
        
        return string.Empty;
    }
}

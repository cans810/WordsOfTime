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
        if (wordSetsWithFacts != null && 
            wordSetsWithFacts.ContainsKey(era) && 
            wordSetsWithFacts[era].ContainsKey(word.ToUpper()))
        {
            // Get the base word first
            string baseWord = GameManager.Instance.GetBaseWord(word);
            
            // Try to get the fact in the requested language
            if (language == "tr")
            {
                // Look for the Turkish fact in the Turkish word set
                var turkishWord = GameManager.Instance.GetTranslation(baseWord, "tr");
                if (wordSetsWithFacts.ContainsKey(era) && 
                    wordSetsWithFacts[era].ContainsKey(turkishWord.ToUpper()))
                {
                    return wordSetsWithFacts[era][turkishWord.ToUpper()];
                }
            }
            
            // Fallback to English fact
            return wordSetsWithFacts[era][word.ToUpper()];
        }
        return string.Empty;
    }
}

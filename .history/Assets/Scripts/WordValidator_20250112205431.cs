using System.Collections.Generic;
using UnityEngine;

public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, string>> wordSetsWithSentences;

    static WordValidator()
    {
        LoadWordSets();
    }

    private static void LoadWordSets()
    {
        string filePath = Application.dataPath + "/words.json"; // Path to the file
        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);
            WordSetList loadedData = JsonUtility.FromJson<WordSetList>("{\"sets\":" + json + "}");

            wordSetsWithSentences = new Dictionary<string, Dictionary<string, string>>();
            foreach (var wordSet in loadedData.sets)
            {
                var wordDict = new Dictionary<string, string>();
                foreach (var wordEntry in wordSet.words)
                {
                    wordDict[wordEntry.word.ToUpper()] = wordEntry.sentence;
                }
                wordSetsWithSentences[wordSet.era] = wordDict;
            }
        }
        else
        {
            Debug.LogError("Error: words.json file not found in Assets folder.");
        }
    }

    public static string GetSentenceForWord(string word, string era)
    {
        if (wordSetsWithSentences != null && wordSetsWithSentences.ContainsKey(era) &&
            wordSetsWithSentences[era].ContainsKey(word.ToUpper()))
        {
            return wordSetsWithSentences[era][word.ToUpper()];
        }
        return "Sentence not found.";
    }

    public static bool IsValidWord(string word, string era)
    {
        if (wordSetsWithSentences != null && wordSetsWithSentences.ContainsKey(era) && wordSetsWithSentences[era].Contains(word.ToUpper()))
        {
            return true;
        }
        return false;
    }

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSetsWithSentences != null && wordSetsWithSentences.ContainsKey(era))
        {
            // Convert the HashSet to a List and return it
            return new List<string>(wordSetsWithSentences[era]);
        }

        Debug.LogError($"Error: Era '{era}' not found in word sets.");
        return new List<string>(); // Return an empty list if era not found
    }
}

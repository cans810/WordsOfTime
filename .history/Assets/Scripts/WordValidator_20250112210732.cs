using System.Collections.Generic;
using UnityEngine;

public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;

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
            WordSet[] wordSets = JsonUtility.FromJson<WordSet[]>(json);  // Deserialize into an array of WordSets

            wordSetsWithSentences = new Dictionary<string, WordSet>();

            foreach (var wordSet in wordSets)
            {
                wordSetsWithSentences[wordSet.era] = wordSet;  // Store each era by its name
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
            var sentences = wordSetsWithSentences[era][word.ToUpper()];
            return sentences.Count > 0 ? sentences[0] : "Sentence not found.";
        }
        return "Sentence not found.";
    }

    public static bool IsValidWord(string word, string era)
    {
        if (wordSetsWithSentences != null && wordSetsWithSentences.ContainsKey(era) && wordSetsWithSentences[era].ContainsKey(word.ToUpper()))
        {
            return true;
        }
        return false;
    }

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSetsWithSentences != null && wordSetsWithSentences.ContainsKey(era))
        {
            // Return the list of words for the specific era
            return new List<string>(wordSetsWithSentences[era].Keys);
        }

        Debug.LogError($"Error: Era '{era}' not found in word sets.");
        return new List<string>(); // Return an empty list if era not found
    }
    
}
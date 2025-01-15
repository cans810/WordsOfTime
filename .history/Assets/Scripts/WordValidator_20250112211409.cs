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
            WordSetList loadedData = JsonUtility.FromJson<WordSetList>("{\"sets\":" + json + "}");

            wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach (var wordSet in loadedData.sets)
            {
                var wordDict = new Dictionary<string, List<string>>();
                foreach (var wordEntry in wordSet.words)
                {
                    wordDict[wordEntry.word.ToUpper()] = wordEntry.sentences;
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
            var sentences = wordSetsWithSentences[era][word.ToUpper()];
            if (sentences.Count > 0)
            {
                return sentences[Random.Range(0, sentences.Count)]; // Pick a random sentence
            }
            else
            {
                Debug.LogError($"No sentences available for the word '{word}' in era '{era}'.");
            }
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
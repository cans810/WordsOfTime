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
        string filePath = Application.dataPath + "/words.json";
        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                Debug.Log("Loading JSON file from: " + filePath);

                // Parse the JSON array directly
                WordSet[] wordSets = JsonUtility.FromJson<WordSetList>("{\"sets\":" + json + "}").sets;

                if (wordSets != null && wordSets.Length > 0)
                {
                    wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                    
                    foreach (var wordSet in wordSets)
                    {
                        var wordDict = new Dictionary<string, List<string>>();
                        foreach (var wordEntry in wordSet.words)
                        {
                            wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences);
                        }
                        wordSetsWithSentences[wordSet.era] = wordDict;
                        Debug.Log($"Loaded era: {wordSet.era} with {wordSet.words.Length} words");
                    }
                }
                else
                {
                    Debug.LogError("No word sets found in the JSON file.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing JSON file: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"words.json file not found at path: {filePath}");
        }
    }

    public static string GetSentenceForWord(string word, string era)
    {
        if (wordSetsWithSentences == null)
        {
            Debug.LogError("Word sets have not been initialized.");
            return "Sentence not found.";
        }

        if (!wordSetsWithSentences.ContainsKey(era))
        {
            Debug.LogError($"Era '{era}' not found in word sets.");
            return "Sentence not found.";
        }

        string upperWord = word.ToUpper();
        if (!wordSetsWithSentences[era].ContainsKey(upperWord))
        {
            Debug.LogError($"Word '{word}' not found in era '{era}'.");
            return "Sentence not found.";
        }

        var sentences = wordSetsWithSentences[era][upperWord];
        if (sentences.Count == 0)
        {
            Debug.LogError($"No sentences available for word '{word}'.");
            return "Sentence not found.";
        }

        return sentences[Random.Range(0, sentences.Count)];
    }

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSetsWithSentences == null)
        {
            Debug.LogError("Word sets have not been initialized.");
            return new List<string>();
        }

        if (!wordSetsWithSentences.ContainsKey(era))
        {
            Debug.LogError($"Era '{era}' not found in word sets. Available eras: {string.Join(", ", wordSetsWithSentences.Keys)}");
            return new List<string>();
        }

        return new List<string>(wordSetsWithSentences[era].Keys);
    }

    public static bool IsValidWord(string word, string era)
    {
        if (wordSetsWithSentences == null)
            return false;

        return wordSetsWithSentences.ContainsKey(era) && 
               wordSetsWithSentences[era].ContainsKey(word.ToUpper());
    }
}
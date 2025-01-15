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
        Debug.Log($"Attempting to load words from: {filePath}");
        
        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                //ebug.Log($"JSON file content length: {json.Length} characters");

                // Direct deserialization since JSON already has the correct structure
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

                if (wordSetList != null && wordSetList.sets != null && wordSetList.sets.Length > 0)
                {
                    wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                    
                    foreach (var wordSet in wordSetList.sets)
                    {
                        //Debug.Log($"Processing era: {wordSet.era}");
                        var wordDict = new Dictionary<string, List<string>>();
                        
                        foreach (var wordEntry in wordSet.words)
                        {
                            wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences);
                            //Debug.Log($"Added word: {wordEntry.word} with {wordEntry.sentences.Length} sentences");
                        }
                        
                        wordSetsWithSentences[wordSet.era] = wordDict;
                        //Debug.Log($"Successfully loaded era '{wordSet.era}' with {wordSet.words.Length} words");
                    }
                }
                else
                {
                    //Debug.LogError("Failed to parse JSON: WordSetList or sets array is null");
                }
            }
            catch (System.Exception e)
            {
                //Debug.LogError($"Error parsing JSON file: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            //Debug.LogError($"words.json file not found at path: {filePath}");
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
            Debug.LogError($"Era '{era}' not found. Available eras: {string.Join(", ", wordSetsWithSentences.Keys)}");
            return "Sentence not found.";
        }

        string upperWord = word.ToUpper();
        if (!wordSetsWithSentences[era].ContainsKey(upperWord))
        {
            Debug.LogError($"Word '{word}' not found in era '{era}'");
            return "Sentence not found.";
        }

        var sentences = wordSetsWithSentences[era][upperWord];
        if (sentences.Count == 0)
        {
            Debug.LogError($"No sentences available for word '{word}'");
            return "Sentence not found.";
        }

        return sentences[Random.Range(0, sentences.Count)];
    }

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSetsWithSentences == null)
        {
            Debug.LogError("Word sets have not been initialized properly");
            return new List<string>();
        }

        // Log available eras for debugging
        Debug.Log($"Available eras: {string.Join(", ", wordSetsWithSentences.Keys)}");
        
        if (!wordSetsWithSentences.ContainsKey(era))
        {
            Debug.LogError($"Era '{era}' not found in available eras");
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
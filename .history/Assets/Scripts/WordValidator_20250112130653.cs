using System.Collections.Generic;
using UnityEngine;

public static class WordValidator
{
    private static Dictionary<string, HashSet<string>> wordSets;

    static WordValidator()
    {
        LoadWordSets();
    }

    private static void LoadWordSets()
    {
        // Load JSON file from Resources
        TextAsset jsonFile = Resources.Load<TextAsset>("words");
        if (jsonFile != null)
        {
            // Parse JSON into WordSetList
            WordSetList loadedData = JsonUtility.FromJson<WordSetList>("{\"sets\":" + jsonFile.text + "}");

            // Convert to dictionary with HashSet for fast lookups
            wordSets = new Dictionary<string, HashSet<string>>();
            foreach (var wordSet in loadedData.sets)
            {
                wordSets[wordSet.era] = new HashSet<string>(wordSet.words);
            }
        }
        else
        {
            Debug.LogError("Word sets JSON file not found!");
        }
    }

    public static bool IsValidWord(string word, string era)
    {
        if (wordSets != null && wordSets.ContainsKey(era) && wordSets[era].Contains(word.ToUpper()))
        {
            return true;
        }
        return false;
    }
}

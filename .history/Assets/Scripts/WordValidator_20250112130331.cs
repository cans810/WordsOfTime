using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json; // Install Newtonsoft.Json via Unity Package Manager

public static class WordValidator
{
    private static Dictionary<string, HashSet<string>> wordSets;

    static WordValidator()
    {
        LoadWordSets();
    }

    private static void LoadWordSets()
    {
        // Load JSON file from Resources folder
        TextAsset jsonFile = Resources.Load<TextAsset>("word_sets");
        if (jsonFile != null)
        {
            // Deserialize JSON to Dictionary
            var loadedData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonFile.text);

            // Convert List<string> to HashSet<string> for faster lookups
            wordSets = new Dictionary<string, HashSet<string>>();
            foreach (var entry in loadedData)
            {
                wordSets[entry.Key] = new HashSet<string>(entry.Value);
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

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
        string filePath = Application.dataPath + "/words.json"; // Path to the file
        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);

            // Wrap the JSON array with a root object for JsonUtility
            WordSetList loadedData = JsonUtility.FromJson<WordSetList>("{\"sets\":" + json + "}");

            // Convert the loaded data into a dictionary
            wordSets = new Dictionary<string, HashSet<string>>();
            foreach (var wordSet in loadedData.sets)
            {
                wordSets[wordSet.era] = new HashSet<string>(wordSet.words);
            }
        }
        else
        {
            Debug.LogError("Error: words.json file not found in Assets folder.");
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

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSets != null && wordSets.ContainsKey(era))
        {
            // Convert the HashSet to a List and return it
            return new List<string>(wordSets[era]);
        }

        Debug.LogError($"Error: Era '{era}' not found in word sets.");
        return new List<string>(); // Return an empty list if era not found
    }
}

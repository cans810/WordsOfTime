using System;
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
                Debug.Log($"JSON file content length: {json.Length} characters");

                // Direct deserialization since JSON already has the correct structure
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

                if (wordSetList != null && wordSetList.sets != null && wordSetList.sets.Length > 0)
                {
                    wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                    
                    foreach (var wordSet in wordSetList.sets)
                    {
                        Debug.Log($"Processing era: {wordSet.era}");
                        var wordDict = new Dictionary<string, List<string>>();
                        
                        foreach (var wordEntry in wordSet.words)
                        {
                            wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences);
                            Debug.Log($"Added word: {wordEntry.word} with {wordEntry.sentences.Length} sentences");
                        }
                        
                        wordSetsWithSentences[wordSet.era] = wordDict;
                        Debug.Log($"Successfully loaded era '{wordSet.era}' with {wordSet.words.Length} words");
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse JSON: WordSetList or sets array is null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing JSON file: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"words.json file not found at path: {filePath}");
        }
    }

    public static bool IsValidWord(string word, string era)
    {
        if (wordSetsWithSentences == null)
            return false;

        return wordSetsWithSentences.ContainsKey(era) && 
               wordSetsWithSentences[era].ContainsKey(word.ToUpper());
    }

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSetsWithSentences == null || !wordSetsWithSentences.ContainsKey(era))
        {
            Debug.LogError($"Era '{era}' not found in available eras");
            return new List<string>();
        }

        return new List<string>(wordSetsWithSentences[era].Keys);
    }

    public static string GetCurrentWord(string era)
    {
        var words = GetWordsForEra(era);
        int currentIndex = GameManager.Instance.GetCurrentWordIndex();
        
        if (currentIndex < words.Count)
        {
            return words[currentIndex];
        }
        
        return null;
    }

    public static string GetSentenceForWord(string word, string era)
    {
        // Only get sentence if this is the current word
        string currentWord = GetCurrentWord(era);
        if (currentWord != null && currentWord.Equals(word, StringComparison.OrdinalIgnoreCase))
        {
            if (wordSetsWithSentences.ContainsKey(era) && 
                wordSetsWithSentences[era].ContainsKey(word.ToUpper()))
            {
                var sentences = wordSetsWithSentences[era][word.ToUpper()];
                return sentences[Random.Range(0, sentences.Count)];
            }
        }
        
        return "Sentence not found.";
    }

    // Update WordGameManager's HandleCorrectWord method
    public static void HandleCorrectWord()
    {
        GameManager.Instance.AdvanceToNextWord();
        
        // Check if there are more words in this era
        string nextWord = WordValidator.GetCurrentWord(GameManager.Instance.EraSelected);
        if (nextWord == null)
        {
            // Era completed
            SceneManager.LoadScene("MainMenuScene");
        }
        else
        {
            // Continue with next word
            SetupGame();
        }
    }
    
}
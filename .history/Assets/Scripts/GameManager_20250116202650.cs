using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    private Dictionary<string, List<string>> eraWordLists;
    public string CurrentEra { get; private set; } = "Ancient";
    private Dictionary<string, Sprite> eraImages = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadWordSets();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadWordSets()
    {
        string filePath = Application.dataPath + "/words.json";
        Debug.Log($"Loading words from: {filePath}");

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

                wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                eraWordLists = new Dictionary<string, List<string>>();

                foreach (var wordSet in wordSetList.sets)
                {
                    var wordDict = new Dictionary<string, List<string>>();
                    var wordList = new List<string>();

                    foreach (var wordEntry in wordSet.words)
                    {
                        string upperWord = wordEntry.word.ToUpper();
                        wordDict[upperWord] = new List<string>(wordEntry.sentences);
                        wordList.Add(upperWord);
                    }

                    wordSetsWithSentences[wordSet.era] = wordDict;
                    eraWordLists[wordSet.era] = wordList;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading word sets: {e.Message}");
            }
        }
    }

    public string GetSentenceForWord(string word, string era)
    {
        if (!wordSetsWithSentences.ContainsKey(era)) return null;
        
        string upperWord = word.ToUpper();
        if (!wordSetsWithSentences[era].ContainsKey(upperWord)) return null;

        var sentences = wordSetsWithSentences[era][upperWord];
        return sentences.Count > 0 ? sentences[Random.Range(0, sentences.Count)] : null;
    }

    public List<string> GetWordsForEra(string era)
    {
        return eraWordLists.ContainsKey(era) ? new List<string>(eraWordLists[era]) : new List<string>();
    }

    public string GetNextWord()
    {
        var words = GetWordsForEra(CurrentEra);
        if (words.Count == 0) return null;

        // Logic to select next word based on your game progression
        return words[0]; // Simplified for example
    }

    public void SetEra(string era)
    {
        if (eraWordLists.ContainsKey(era))
        {
            CurrentEra = era;
            // Additional era change logic here
        }
    }

    public Sprite getEraImage(string era)
    {
        return eraImages.ContainsKey(era) ? eraImages[era] : null;
    }

    public bool IsValidWord(string word, string era)
    {
        return wordSetsWithSentences.ContainsKey(era) && 
               wordSetsWithSentences[era].ContainsKey(word.ToUpper());
    }
}
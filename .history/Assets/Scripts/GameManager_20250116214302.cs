using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour{
public static GameManager Instance { get; private set; }

    public List<string> EraList = new List<string>();
    public string CurrentEra { get; set; } = "";
    private int currentEraIndex = -1;

    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    public List<string> unsolvedWordsInCurrentEra;

    private Dictionary<string, List<char>> preGeneratedGrids = new Dictionary<string, List<char>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    private void InitializeGame()
    {
        Debug.Log("Initializing game...");

        // Step 1: Load word sets
        LoadWordSets();

        // Step 2: Start with a random era
        StartWithRandomEra();

        // Step 3: Pre-generate grids for the current era
        if (!string.IsNullOrEmpty(CurrentEra))
        {
            PreGenerateGridsForEra(CurrentEra);
        }
        else
        {
            Debug.LogError("CurrentEra is null or empty!");
        }
    }

    private void LoadWordSets()
    {
        string filePath = Application.dataPath + "/words.json";
        Debug.Log($"Attempting to load words from: {filePath}");

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

                if (wordSetList != null && wordSetList.sets != null && wordSetList.sets.Length > 0)
                {
                    wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();

                    foreach (var wordSet in wordSetList.sets)
                    {
                        var wordDict = new Dictionary<string, List<string>>();
                        foreach (var wordEntry in wordSet.words)
                        {
                            wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences);
                        }
                        wordSetsWithSentences[wordSet.era] = wordDict;
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

    private void StartWithRandomEra()
    {
        if (EraList.Count == 0)
        {
            Debug.LogError("Era List is empty! Add eras to the list to continue");
            return;
        }

        currentEraIndex = Random.Range(0, EraList.Count);
        CurrentEra = EraList[currentEraIndex];
        ResetUnsolvedWordsForEra(CurrentEra);
        Debug.Log($"Started with random era: {CurrentEra}");
    }

    private void PreGenerateGridsForEra(string era)
    {
        Debug.Log($"Pre-generating grids for era: {era}");

        if (wordSetsWithSentences == null)
        {
            Debug.LogError("wordSetsWithSentences is null! Ensure LoadWordSets() is called first.");
            return;
        }

        if (!wordSetsWithSentences.ContainsKey(era))
        {
            Debug.LogError($"Era '{era}' not found in wordSetsWithSentences!");
            return;
        }

        foreach (var word in wordSetsWithSentences[era].Keys)
        {
            Debug.Log($"Pre-generating grid for word: {word}");
            GridManager.Instance.GenerateAndStoreInitialGrid(word);
        }
    }

    private void ResetUnsolvedWordsForEra(string era)
    {
        if (wordSetsWithSentences.ContainsKey(era))
        {
            unsolvedWordsInCurrentEra = new List<string>(wordSetsWithSentences[era].Keys);
            Debug.Log($"Unsolved words reset for {era}. Count: {unsolvedWordsInCurrentEra.Count}");
        }
        else
        {
            Debug.LogError($"Era {era} not found in word sets!");
            unsolvedWordsInCurrentEra = new List<string>();
        }
    }

    public string GetNextWord()
    {
        if (unsolvedWordsInCurrentEra == null || unsolvedWordsInCurrentEra.Count == 0)
        {
            Debug.Log("All words solved in this era. Returning to main menu.");
            return null;
        }

        int randomIndex = Random.Range(0, unsolvedWordsInCurrentEra.Count);
        string nextWord = unsolvedWordsInCurrentEra[randomIndex];
        unsolvedWordsInCurrentEra.RemoveAt(randomIndex);
        return nextWord;
    }

public void SelectEra(string eraName) // Make SelectEra public so UI can use it
{
    CurrentEra = eraName;
    currentEraIndex = EraList.IndexOf(eraName);  // Set correct index!
    ResetUnsolvedWordsForEra(CurrentEra); // Reset when selecting an era
    Debug.Log($"Selected era: {CurrentEra}");
}


public Sprite getEraImage(string era)
{
    if (era.Equals("Ancient Egypt"))
    {
        return eraImages[0];
    }
    else if (era.Equals("Medieval Europe"))
    {
        return eraImages[1];
    }
    else if (era.Equals("Ancient Rome"))
    {
        return eraImages[2];
    }
    else if (era.Equals("Renaissance"))
    {
        return eraImages[3];
    }
    else if (era.Equals("Industrial Revolution"))
    {
        return eraImages[4];
    }
    else if (era.Equals("Ancient Greece"))
    {
        return eraImages[5];
    }
    return null;
}
}
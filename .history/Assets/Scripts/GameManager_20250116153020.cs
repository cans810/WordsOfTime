using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<string> EraList = new List<string>();
    public string CurrentEra { get; set; } = "";
    private int currentEraIndex = -1;

    public List<Sprite> eraImages = new List<Sprite>();

    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    public List<string> unsolvedWordsInCurrentEra;

    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadWordSets();
        StartWithRandomEra();
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
                        foreach (var wordEntry in wordSet.words) // Use Word[] directly
                        {
                            string word = wordEntry.word.ToUpper();
                            wordDict[word] = new List<string>(wordEntry.sentences);
                            GenerateAndStoreInitialGrid(word); 
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


    private void GenerateAndStoreInitialGrid(string word)
    {
        GridManager tempGridManager = null;
        if (GridManager.Instance == null)
        {
            GameObject tempGridManagerObject = new GameObject("TempGridManager");
            tempGridManager = tempGridManagerObject.AddComponent<GridManager>();

            tempGridManager.letterTilePrefab = Resources.Load<GameObject>("Prefabs/LetterButton"); // Or your prefab path
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject != null)
            {
                tempGridManager.gridContainer = canvasObject.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogError("Canvas not found!");
            }


            tempGridManager.WordGameManager = FindObjectOfType<WordGameManager>();

        }
        else
        {
            tempGridManager = GridManager.Instance;
        }



        if (!initialGrids.ContainsKey(word))
        {
            tempGridManager.GenerateInitialGridForGameManager(word);

            List<char> initialLetters = new List<char>();
            for (int x = 0; x < tempGridManager.gridSize; x++)
            {
                for (int y = 0; y < tempGridManager.gridSize; y++)
                {
                    initialLetters.Add(tempGridManager.grid[x, y].Letter);
                }
            }
            initialGrids.Add(word, initialLetters);


            if (GridManager.Instance == null)
            {
                Destroy(tempGridManager.gameObject); // Destroy Temporary game object.
            }
        }


    }

    public List<char> GetInitialGrid(string word)
    {
        if (initialGrids.TryGetValue(word, out List<char> grid))
        {
            return grid;
        }
        Debug.LogError($"Grid for word '{word}' not found!");
        return null;
    }



    public void InitializeUnsolvedWords()
    {
        if (unsolvedWordsInCurrentEra == null)
        {
            unsolvedWordsInCurrentEra = new List<string>(WordValidator.GetWordsForEra(CurrentEra));
        }
    }

    public void MoveToNextEra()
    {
        currentEraIndex++;
        if (currentEraIndex < EraList.Count)
        {
            CurrentEra = EraList[currentEraIndex];
            ResetUnsolvedWordsForEra(CurrentEra);
            Debug.Log($"Moved to next era: {CurrentEra}");
        }
        else
        {
            Debug.Log("All eras completed!");
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    public string GetNextWord()
    {
        if (unsolvedWordsInCurrentEra == null || unsolvedWordsInCurrentEra.Count == 0)
        {
            Debug.Log("All words solved in this era. Returning to main menu.");
            SceneManager.LoadScene("MainMenuScene");
            return null;
        }

        int randomIndex = Random.Range(0, unsolvedWordsInCurrentEra.Count);
        string nextWord = unsolvedWordsInCurrentEra[randomIndex];
        unsolvedWordsInCurrentEra.RemoveAt(randomIndex);
        return nextWord;
    }


    public void SelectEra(string eraName)
    {
        CurrentEra = eraName;
        currentEraIndex = EraList.IndexOf(eraName);
        ResetUnsolvedWordsForEra(CurrentEra);
        Debug.Log($"Selected era: {CurrentEra}");
    }

    private void ResetUnsolvedWordsForEra(string era)
    {
        if (wordSetsWithSentences.ContainsKey(era))
        {
            unsolvedWordsInCurrentEra = new List<string>(wordSetsWithSentences[era].Keys);
            Debug.Log($"Unsolved words reset for {era}.  Count: {unsolvedWordsInCurrentEra.Count}");
        }
        else
        {
            Debug.LogError($"Era {era} not found in word sets!");
            unsolvedWordsInCurrentEra = new List<string>();
        }
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
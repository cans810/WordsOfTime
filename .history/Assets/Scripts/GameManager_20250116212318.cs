using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ERA definitions
    public List<string> EraList = new List<string>();
    public string CurrentEra { get; set; } = "";
    private int currentEraIndex = -1;

    // UI / Visual references
    public List<Sprite> eraImages = new List<Sprite>();

    // The main data structure from JSON: era -> (word -> sentences)
    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;

    // A dictionary to store pre-generated grid layouts
    // preGeneratedLayouts[era][word] = List<char> (flattened 5x5 letters)
    public Dictionary<string, Dictionary<string, List<char>>> preGeneratedLayouts
        = new Dictionary<string, Dictionary<string, List<char>>>();

    // List of unsolved words in the current era
    public List<string> unsolvedWordsInCurrentEra;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 1) Load words.json exactly once
        LoadWordSets();

        // 2) Start with a random era (or pick the first, up to you)
        StartWithRandomEra();

        // 3) Pre-generate puzzle layouts for all (era, word) combos
        PreGenerateAllPuzzleLayouts();
    }

    private void LoadWordSets()
    {
        // Adjust filePath to your actual path
        string filePath = Application.dataPath + "/words.json";
        Debug.Log($"Attempting to load words from: {filePath}");

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

                if (wordSetList != null && wordSetList.sets != null && wordSetList.sets.Length > 0)
                {
                    wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();

                    foreach (var wordSet in wordSetList.sets)
                    {
                        var wordDict = new Dictionary<string, List<string>>();
                        foreach (var wordEntry in wordSet.words)
                        {
                            // store as UPPER for consistency
                            wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences);
                        }
                        wordSetsWithSentences[wordSet.era] = wordDict;
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse JSON: WordSetList or sets array is null/empty");
                    wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing JSON file: {e.Message}\n{e.StackTrace}");
                wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
            }
        }
        else
        {
            Debug.LogError($"words.json file not found at path: {filePath}");
            wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
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

    private void ResetUnsolvedWordsForEra(string era)
    {
        if (wordSetsWithSentences != null && wordSetsWithSentences.ContainsKey(era))
        {
            // all words from that era (keys in the dictionary)
            unsolvedWordsInCurrentEra = new List<string>(wordSetsWithSentences[era].Keys);
            Debug.Log($"Unsolved words reset for {era}. Count: {unsolvedWordsInCurrentEra.Count}");
        }
        else
        {
            Debug.LogError($"Era '{era}' not found in word sets!");
            unsolvedWordsInCurrentEra = new List<string>();
        }
    }

    // Called once after reading JSON
    private void PreGenerateAllPuzzleLayouts()
    {
        preGeneratedLayouts.Clear();

        if (wordSetsWithSentences == null)
        {
            Debug.LogError("wordSetsWithSentences is null, cannot pre-generate!");
            return;
        }

        // For every era in the dictionary
        foreach (var eraEntry in wordSetsWithSentences)
        {
            string eraName = eraEntry.Key;
            if (!preGeneratedLayouts.ContainsKey(eraName))
                preGeneratedLayouts[eraName] = new Dictionary<string, List<char>>();

            // For every word in this era
            foreach (var wordKvp in eraEntry.Value)
            {
                string word = wordKvp.Key;  // e.g., "CAT"
                List<char> layout = GeneratePuzzleLayoutForWord(word);
                preGeneratedLayouts[eraName][word] = layout;
            }
        }

        Debug.Log("All puzzle layouts have been pre-generated.");
    }

    // Generate a 5x5 layout with a single word placed adjacently, filling leftover cells with random letters
    private List<char> GeneratePuzzleLayoutForWord(string word)
    {
        int gridSize = 5; // or read from a config/Inspector
        char[,] tempGrid = new char[gridSize, gridSize];

        // 1) Initialize all cells to '\0'
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                tempGrid[x, y] = '\0';
            }
        }

        // 2) Attempt to place the word adjacently
        bool placed = false;
        int maxAttempts = 50;
        while (!placed && maxAttempts > 0)
        {
            placed = TryPlaceWord(tempGrid, word, gridSize);
            maxAttempts--;
        }

        if (!placed)
        {
            Debug.LogWarning($"Failed to place '{word}' in adjacency after several attempts. Filling anyway.");
        }

        // 3) Fill leftover spaces
        FillRemaining(tempGrid, gridSize);

        // 4) Flatten char[,] into List<char>
        List<char> layout = new List<char>(gridSize * gridSize);
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                layout.Add(tempGrid[x, y]);
            }
        }

        return layout;
    }

    private bool TryPlaceWord(char[,] grid, string word, int gridSize)
    {
        int startX = Random.Range(0, gridSize);
        int startY = Random.Range(0, gridSize);
        Vector2Int currentPos = new Vector2Int(startX, startY);

        for (int i = 0; i < word.Length; i++)
        {
            if (currentPos.x < 0 || currentPos.x >= gridSize ||
                currentPos.y < 0 || currentPos.y >= gridSize ||
                grid[currentPos.x, currentPos.y] != '\0')
            {
                // Placement fails
                return false;
            }

            grid[currentPos.x, currentPos.y] = word[i];

            // If not last letter, pick a valid adjacency for the next letter
            if (i < word.Length - 1)
            {
                var validPositions = GetValidAdjacentPositions(grid, currentPos, gridSize);
                if (validPositions.Count == 0) return false;
                currentPos = validPositions[Random.Range(0, validPositions.Count)];
            }
        }
        return true;
    }

    private List<Vector2Int> GetValidAdjacentPositions(char[,] grid, Vector2Int pos, int gridSize)
    {
        var result = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        foreach (var dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (newPos.x >= 0 && newPos.x < gridSize &&
                newPos.y >= 0 && newPos.y < gridSize &&
                grid[newPos.x, newPos.y] == '\0')
            {
                result.Add(newPos);
            }
        }

        return result;
    }

    private void FillRemaining(char[,] grid, int gridSize)
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] == '\0')
                {
                    char randomLetter = (char)Random.Range('A', 'Z' + 1);
                    grid[x, y] = randomLetter;
                }
            }
        }
    }

    // ERA movement logic
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

    // Return a random unsolved word (or null if none left)
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

    public Sprite getEraImage(string era)
    {
        // example mapping
        if (era.Equals("Ancient Egypt")) return eraImages[0];
        else if (era.Equals("Medieval Europe")) return eraImages[1];
        else if (era.Equals("Ancient Rome")) return eraImages[2];
        else if (era.Equals("Renaissance")) return eraImages[3];
        else if (era.Equals("Industrial Revolution")) return eraImages[4];
        else if (era.Equals("Ancient Greece")) return eraImages[5];
        return null;
    }
}

// Classes for JSON parsing
[System.Serializable]
public class WordSetList
{
    public WordSet[] sets;
}

[System.Serializable]
public class WordSet
{
    public string era;
    public WordEntry[] words;
}

[System.Serializable]
public class WordEntry
{
    public string word;
    public string[] sentences;
}

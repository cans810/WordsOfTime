using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour{
public static GameManager Instance { get; private set; }

public List<string> EraList = new List<string>();
public string CurrentEra { get; set; } = "";
private int currentEraIndex = -1;

public List<Sprite> eraImages = new List<Sprite>();

private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
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

        preGeneratedTiles = new Dictionary<string, Dictionary<string, List<char>>>();
        wordPositions = new Dictionary<string, Dictionary<string, List<Vector2Int>>>();
        
        // Load JSON and generate tiles only once
        LoadWordSetsAndGenerateTiles();
    }

    private void LoadWordSetsAndGenerateTiles()
    {
        string filePath = Application.dataPath + "/words.json";
        Debug.Log($"Loading words from: {filePath}");

        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"words.json not found at: {filePath}");
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

            if (wordSetList == null || wordSetList.sets == null)
            {
                Debug.LogError("Failed to parse JSON: WordSetList is null");
                return;
            }

            foreach (var wordSet in wordSetList.sets)
            {
                string era = wordSet.era;
                preGeneratedTiles[era] = new Dictionary<string, List<char>>();
                wordPositions[era] = new Dictionary<string, List<Vector2Int>>();

                foreach (var wordEntry in wordSet.words)
                {
                    string word = wordEntry.word.ToUpper();
                    GenerateTilesForWord(era, word);
                }
            }
            
            Debug.Log("Successfully loaded words and generated tiles for all eras");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading words: {e.Message}\n{e.StackTrace}");
        }
    }

    private void GenerateTilesForWord(string era, string word)
    {
        const int gridSize = 5; // Make this configurable if needed
        List<char> tiles = new List<char>(gridSize * gridSize);
        List<Vector2Int> positions = new List<Vector2Int>();

        // Initialize grid with empty chars
        char[,] tempGrid = new char[gridSize, gridSize];
        for (int i = 0; i < gridSize; i++)
            for (int j = 0; j < gridSize; j++)
                tempGrid[i, j] = '\0';

        // Place the word in random adjacent positions
        if (PlaceWordInGrid(tempGrid, word, positions))
        {
            // Fill remaining spaces with random letters
            FillRemainingSpaces(tempGrid);

            // Convert grid to list
            for (int i = 0; i < gridSize; i++)
                for (int j = 0; j < gridSize; j++)
                    tiles.Add(tempGrid[i, j]);

            preGeneratedTiles[era][word] = tiles;
            wordPositions[era][word] = new List<Vector2Int>(positions);
        }
        else
        {
            Debug.LogError($"Failed to generate tiles for word: {word} in era: {era}");
        }
    }

    private bool PlaceWordInGrid(char[,] grid, string word, List<Vector2Int> positions)
    {
        int gridSize = grid.GetLength(0);
        const int maxAttempts = 100;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            positions.Clear();
            
            // Start from random position
            int startX = Random.Range(0, gridSize);
            int startY = Random.Range(0, gridSize);
            Vector2Int currentPos = new Vector2Int(startX, startY);

            bool success = true;
            for (int i = 0; i < word.Length; i++)
            {
                if (!IsValidPosition(currentPos, gridSize) || grid[currentPos.x, currentPos.y] != '\0')
                {
                    success = false;
                    break;
                }

                grid[currentPos.x, currentPos.y] = word[i];
                positions.Add(currentPos);

                if (i < word.Length - 1)
                {
                    List<Vector2Int> validNextPositions = GetValidAdjacentPositions(grid, currentPos);
                    if (validNextPositions.Count == 0)
                    {
                        success = false;
                        break;
                    }
                    currentPos = validNextPositions[Random.Range(0, validNextPositions.Count)];
                }
            }

            if (success)
                return true;

            // Clear the grid for next attempt
            for (int i = 0; i < gridSize; i++)
                for (int j = 0; j < gridSize; j++)
                    grid[i, j] = '\0';
        }

        return false;
    }

    private List<Vector2Int> GetValidAdjacentPositions(char[,] grid, Vector2Int pos)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        Vector2Int[] directions = {
            Vector2Int.right, Vector2Int.left,
            Vector2Int.up, Vector2Int.down
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (IsValidPosition(newPos, grid.GetLength(0)) && grid[newPos.x, newPos.y] == '\0')
            {
                validPositions.Add(newPos);
            }
        }

        return validPositions;
    }

    private bool IsValidPosition(Vector2Int pos, int gridSize)
    {
        return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
    }

    private void FillRemainingSpaces(char[,] grid)
    {
        int gridSize = grid.GetLength(0);
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (grid[i, j] == '\0')
                {
                    grid[i, j] = (char)Random.Range('A', 'Z' + 1);
                }
            }
        }
    }

    // Public methods to access the pre-generated tiles
    public List<char> GetTilesForWord(string era, string word)
    {
        if (preGeneratedTiles.TryGetValue(era, out var eraWords))
        {
            if (eraWords.TryGetValue(word, out var tiles))
            {
                return tiles;
            }
        }
        Debug.LogError($"No tiles found for word {word} in era {era}");
        return null;
    }

    public List<Vector2Int> GetWordPositions(string era, string word)
    {
        if (wordPositions.TryGetValue(era, out var eraPositions))
        {
            if (eraPositions.TryGetValue(word, out var positions))
            {
                return positions;
            }
        }
        Debug.LogError($"No positions found for word {word} in era {era}");
        return null;
    }

private void StartWithRandomEra()
{
    if(EraList.Count == 0)
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
        ResetUnsolvedWordsForEra(CurrentEra); // Reset words when moving to a new era
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
        SceneManager.LoadScene("MainMenuScene");  // Go to the main menu.
        return null; // Indicate no more words.  Important!
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
        unsolvedWordsInCurrentEra = new List<string>(); // Or handle the error differently
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
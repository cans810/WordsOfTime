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

    // New: Dictionary to store pre-generated grids for each word
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

        LoadWordSets();
        GenerateAllGrids(); // New: Generate all grids once during initialization
        StartWithRandomEra();
    }

    // New method to generate all grids during initialization
    private void GenerateAllGrids()
    {
        if (wordSetsWithSentences == null) return;

        foreach (var eraPair in wordSetsWithSentences)
        {
            foreach (var wordPair in eraPair.Value)
            {
                string word = wordPair.Key;
                if (!preGeneratedGrids.ContainsKey(word))
                {
                    List<char> grid = GenerateGridForWord(word);
                    preGeneratedGrids.Add(word, grid);
                }
            }
        }
        
        Debug.Log($"Generated grids for {preGeneratedGrids.Count} words");
    }

    // New method to generate a single grid for a word
    private List<char> GenerateGridForWord(string word)
    {
        const int gridSize = 5;
        List<char> grid = new List<char>(new char[gridSize * gridSize]);
        
        // Place the word in the grid
        List<int> positions = GenerateAdjacentPositions(word.Length, gridSize);
        for (int i = 0; i < word.Length; i++)
        {
            grid[positions[i]] = word[i];
        }

        // Fill remaining spaces with random letters
        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] == '\0')
            {
                grid[i] = (char)UnityEngine.Random.Range('A', 'Z' + 1);
            }
        }

        return grid;
    }

    // Helper method to generate adjacent positions for word placement
    private List<int> GenerateAdjacentPositions(int wordLength, int gridSize)
    {
        List<int> positions = new List<int>();
        int startPos = UnityEngine.Random.Range(0, gridSize * gridSize);
        positions.Add(startPos);

        for (int i = 1; i < wordLength; i++)
        {
            List<int> validMoves = GetValidAdjacentPositions(positions[i - 1], gridSize, positions);
            if (validMoves.Count == 0)
            {
                // If we can't place the word, start over
                return GenerateAdjacentPositions(wordLength, gridSize);
            }
            positions.Add(validMoves[UnityEngine.Random.Range(0, validMoves.Count)]);
        }

        return positions;
    }

    private List<int> GetValidAdjacentPositions(int currentPos, int gridSize, List<int> usedPositions)
    {
        List<int> validMoves = new List<int>();
        int row = currentPos / gridSize;
        int col = currentPos % gridSize;

        // Check all four directions (up, right, down, left)
        int[] dr = { -1, 0, 1, 0 };
        int[] dc = { 0, 1, 0, -1 };

        for (int i = 0; i < 4; i++)
        {
            int newRow = row + dr[i];
            int newCol = col + dc[i];
            int newPos = newRow * gridSize + newCol;

            if (newRow >= 0 && newRow < gridSize && 
                newCol >= 0 && newCol < gridSize && 
                !usedPositions.Contains(newPos))
            {
                validMoves.Add(newPos);
            }
        }

        return validMoves;
    }

    // New method to get a pre-generated grid
    public List<char> GetGridForWord(string word)
    {
        if (preGeneratedGrids.TryGetValue(word, out List<char> grid))
        {
            return new List<char>(grid); // Return a copy to prevent modifications
        }
        
        Debug.LogError($"Grid not found for word: {word}");
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
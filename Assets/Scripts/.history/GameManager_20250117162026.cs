using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Required Settings")]
    public List<string> EraList = new List<string>() { "Ancient Egypt", "Medieval Europe", "Ancient Rome", "Renaissance", "Industrial Revolution", "Ancient Greece" };
    public List<Sprite> eraImages = new List<Sprite>();

    [Header("Game State")]
    public string CurrentEra { get; set; } = "";
    private int currentEraIndex = -1;
    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    public List<string> unsolvedWordsInCurrentEra;
    public Dictionary<string, List<char>> InitialGrids { get; private set; } = new Dictionary<string, List<char>>();
    private const int GRID_SIZE = 6;
    public Dictionary<string, List<Vector2Int>> solvedWordPositions { get; private set; } = new Dictionary<string, List<Vector2Int>>();
    private Dictionary<string, HashSet<int>> solvedWordsPerEra = new Dictionary<string, HashSet<int>>();

    [Header("Points System")]
    public int CurrentPoints { get; private set; } = 0;
    private Dictionary<string, int> eraUnlockRequirements = new Dictionary<string, int>
    {
        {"Ancient Egypt", 0},
        {"Medieval Europe", 0},
        {"Ancient Rome", 300},
        {"Renaissance", 600},
        {"Industrial Revolution", 900},
        {"Ancient Greece", 1200}
    };

    public const int POINTS_PER_WORD = 100;
    public const int HINT_COST = 50;
    public const int SECOND_HINT_COST = 100;

    private void Awake()
    {
        try
        {
            Debug.Log("GameManager Awake: Starting");
            InitializeSingleton();
            InitializeCollections();
            LoadSavedData();
            LoadWordSets();
            
            if (EraList.Count > 0)
            {
                GenerateAllGrids();
                StartWithRandomEra();
            }
            Debug.Log("GameManager Awake: Completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager initialization failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeCollections()
    {
        if (EraList == null) EraList = new List<string>();
        if (eraImages == null) eraImages = new List<Sprite>();
        if (InitialGrids == null) InitialGrids = new Dictionary<string, List<char>>();
        if (solvedWordPositions == null) solvedWordPositions = new Dictionary<string, List<Vector2Int>>();
        if (solvedWordsPerEra == null) solvedWordsPerEra = new Dictionary<string, HashSet<int>>();
        if (wordSetsWithSentences == null) wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
    }

    private void LoadSavedData()
    {
        CurrentPoints = PlayerPrefs.GetInt("CurrentPoints", 0);
    }

    public void GenerateAllGrids()
    {
        InitialGrids.Clear();
        foreach (var era in EraList)
        {
            List<string> words = WordValidator.GetWordsForEra(era);
            foreach (var word in words)
            {
                if (!InitialGrids.ContainsKey(word))
                {
                    GenerateGridForWord(word);
                }
            }
        }
    }

    private void GenerateGridForWord(string word)
    {
        List<char> grid = new List<char>();
        for (int i = 0; i < 25; i++) // 5x5 grid = 25 spaces
        {
            grid.Add('.');
        }

        PlaceWordInGrid(word, grid);
        FillRemainingSpaces(grid);
        InitialGrids[word] = grid;
    }

    private void PlaceWordInGrid(string word, List<char> grid)
    {
        int gridWidth = 5;
        int position;
        bool placed = false;

        do
        {
            position = UnityEngine.Random.Range(0, grid.Count - word.Length + 1);
            
            // Check if position is valid (doesn't wrap around grid edge)
            int row = position / gridWidth;
            int endRow = (position + word.Length - 1) / gridWidth;
            
            if (row == endRow) // Word fits on same row
            {
                placed = true;
                for (int i = 0; i < word.Length; i++)
                {
                    grid[position + i] = word[i];
                }
            }
        } while (!placed);
    }

    private void FillRemainingSpaces(List<char> grid)
    {
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] == '.')
            {
                grid[i] = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
            }
        }
    }

    private bool TryPlaceWordFromPosition(string word, int startPos, List<int> availablePositions, List<int> wordPositions)
    {
        wordPositions.Add(startPos);
        
        for (int i = 1; i < word.Length; i++)
        {
            List<int> adjacentPositions = GetAdjacentPositions(wordPositions[i - 1]);
            
            // Shuffle adjacent positions for more randomness
            for (int j = adjacentPositions.Count - 1; j > 0; j--)
            {
                int k = Random.Range(0, j + 1);
                int temp = adjacentPositions[j];
                adjacentPositions[j] = adjacentPositions[k];
                adjacentPositions[k] = temp;
            }

            bool foundValidPosition = false;
            foreach (int pos in adjacentPositions)
            {
                if (availablePositions.Contains(pos) && !wordPositions.Contains(pos))
                {
                    // Add some randomness to the direction choice
                    if (Random.value > 0.3f || i == word.Length - 1)
                    {
                        wordPositions.Add(pos);
                        foundValidPosition = true;
                        break;
                    }
                }
            }

            if (!foundValidPosition)
            {
                return false;
            }
        }

        return true;
    }

    private List<int> GetAdjacentPositions(int position)
    {
        List<int> adjacent = new List<int>();
        int row = position / GRID_SIZE;
        int col = position % GRID_SIZE;

        // Only use orthogonal directions (up, right, down, left)
        int[] dr = { -1, 0, 1, 0 };
        int[] dc = { 0, 1, 0, -1 };

        // Create a list of direction indices and shuffle them
        List<int> directions = new List<int>();
        for (int i = 0; i < 4; i++) directions.Add(i);
        
        // Shuffle directions for randomness
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }

        // Check each direction in random order
        foreach (int dir in directions)
        {
            int newRow = row + dr[dir];
            int newCol = col + dc[dir];
            
            if (newRow >= 0 && newRow < GRID_SIZE && newCol >= 0 && newCol < GRID_SIZE)
            {
                adjacent.Add(newRow * GRID_SIZE + newCol);
            }
        }

        return adjacent;
    }

    private List<int> PlaceWordRandomly(string word, List<int> availablePositions)
    {
        List<int> positions = new List<int>();
        List<int> tempAvailable = new List<int>(availablePositions);
        
        // More thorough shuffling of available positions
        for (int i = tempAvailable.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = tempAvailable[i];
            tempAvailable[i] = tempAvailable[j];
            tempAvailable[j] = temp;
        }

        // Take random positions for each letter
        for (int i = 0; i < word.Length && tempAvailable.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, tempAvailable.Count);
            positions.Add(tempAvailable[randomIndex]);
            tempAvailable.RemoveAt(randomIndex);
        }

        return positions;
    }

    private void LoadWordSets()
    {
        try
        {
            string filePath = Application.dataPath + "/words.json";
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogError($"GameManager LoadWordSets: words.json not found at path: {filePath}");
                return;
            }

            string json = System.IO.File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("GameManager LoadWordSets: words.json is empty");
                return;
            }

            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);
            if (wordSetList == null || wordSetList.sets == null)
            {
                Debug.LogError("GameManager LoadWordSets: Failed to parse WordSetList from JSON");
                return;
            }

            wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach (var wordSet in wordSetList.sets)
            {
                if (string.IsNullOrEmpty(wordSet.era))
                {
                    Debug.LogWarning("GameManager LoadWordSets: Found wordSet with empty era name");
                    continue;
                }

                var wordDict = new Dictionary<string, List<string>>();
                foreach (var wordEntry in wordSet.words)
                {
                    if (string.IsNullOrEmpty(wordEntry.word))
                    {
                        Debug.LogWarning($"GameManager LoadWordSets: Found empty word in era {wordSet.era}");
                        continue;
                    }
                    wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences ?? new string[0]);
                }
                wordSetsWithSentences[wordSet.era] = wordDict;
            }

            Debug.Log($"GameManager LoadWordSets: Successfully loaded {wordSetsWithSentences.Count} eras");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager LoadWordSets: Error loading word sets: {e.Message}\n{e.StackTrace}");
        }
    }

    private void StartWithRandomEra()
    {
        if (EraList.Count == 0) return;
        currentEraIndex = Random.Range(0, EraList.Count);
        CurrentEra = EraList[currentEraIndex];
        ResetUnsolvedWordsForEra(CurrentEra);
    }

    public void MoveToNextEra()
    {
        currentEraIndex++;
        if (currentEraIndex < EraList.Count)
        {
            CurrentEra = EraList[currentEraIndex];
            ResetUnsolvedWordsForEra(CurrentEra);
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    public string GetNextWord()
    {
        if (unsolvedWordsInCurrentEra == null || unsolvedWordsInCurrentEra.Count == 0)
        {
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
        
        // Initialize solved words set for this era if it doesn't exist
        if (!solvedWordsPerEra.ContainsKey(eraName))
        {
            solvedWordsPerEra[eraName] = new HashSet<int>();
        }
        
        // Update WordGameManager with the solved words for this era
        if (WordGameManager.Instance != null)
        {
            WordGameManager.Instance.solvedWordsInCurrentEra = solvedWordsPerEra[eraName];
        }
        
        ResetUnsolvedWordsForEra(eraName);
    }

    private void ResetUnsolvedWordsForEra(string era)
    {
        if (wordSetsWithSentences.ContainsKey(era))
        {
            unsolvedWordsInCurrentEra = new List<string>(wordSetsWithSentences[era].Keys);
        }
    }

    public Sprite getEraImage(string era)
    {
        int index = EraList.IndexOf(era);
        return index >= 0 && index < eraImages.Count ? eraImages[index] : null;
    }

    public void StoreSolvedWordPositions(string word, List<Vector2Int> positions)
    {
        solvedWordPositions[word] = positions;
        Debug.Log($"GameManager: Stored solved positions for word: {word}");
    }

    public List<Vector2Int> GetSolvedWordPositions(string word)
    {
        if (solvedWordPositions.ContainsKey(word))
        {
            return solvedWordPositions[word];
        }
        return null;
    }

    // Add method to store solved word for current era
    public void AddSolvedWordForCurrentEra(int wordIndex)
    {
        if (!solvedWordsPerEra.ContainsKey(CurrentEra))
        {
            solvedWordsPerEra[CurrentEra] = new HashSet<int>();
        }
        solvedWordsPerEra[CurrentEra].Add(wordIndex);
        Debug.Log($"Added solved word index {wordIndex} for era {CurrentEra}. Total solved in era: {solvedWordsPerEra[CurrentEra].Count}");
    }

    // Add method to get solved words for an era
    public HashSet<int> GetSolvedWordsForEra(string era)
    {
        if (solvedWordsPerEra.ContainsKey(era))
        {
            return solvedWordsPerEra[era];
        }
        return new HashSet<int>();
    }

    public void AddPoints(int points)
    {
        CurrentPoints += points;
        Debug.Log($"Added {points} points. Total points: {CurrentPoints}");
        // Save points to PlayerPrefs for persistence
        PlayerPrefs.SetInt("CurrentPoints", CurrentPoints);
        PlayerPrefs.Save();
    }

    public bool IsEraUnlocked(string era)
    {
        if (!eraUnlockRequirements.ContainsKey(era)) return false;
        return CurrentPoints >= eraUnlockRequirements[era];
    }

    public int GetEraUnlockRequirement(string era)
    {
        return eraUnlockRequirements.ContainsKey(era) ? eraUnlockRequirements[era] : 0;
    }

    public bool CanUseHint(int hintLevel)
    {
        int cost = hintLevel == 1 ? HINT_COST : SECOND_HINT_COST;
        return CurrentPoints >= cost;
    }

    public void UseHint(int hintLevel)
    {
        int cost = hintLevel == 1 ? HINT_COST : SECOND_HINT_COST;
        if (CurrentPoints >= cost)
        {
            CurrentPoints -= cost;
            PlayerPrefs.SetInt("CurrentPoints", CurrentPoints);
            PlayerPrefs.Save();
        }
    }
}
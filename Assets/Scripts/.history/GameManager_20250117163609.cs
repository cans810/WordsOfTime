using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Required Settings")]
    [SerializeField] private List<string> eraList = new List<string>() { "Ancient Egypt", "Medieval Europe", "Ancient Rome", "Renaissance", "Industrial Revolution", "Ancient Greece" };
    [SerializeField] private List<Sprite> eraImages = new List<Sprite>();

    [Header("Game State")]
    private string currentEra = "";
    private int currentEraIndex = -1;
    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
    private List<string> unsolvedWordsInCurrentEra = new List<string>();
    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();
    private Dictionary<string, List<Vector2Int>> solvedWordPositions = new Dictionary<string, List<Vector2Int>>();
    private Dictionary<string, HashSet<int>> solvedWordsPerEra = new Dictionary<string, HashSet<int>>();

    [Header("Points System")]
    private int currentPoints = 0;
    public const int POINTS_PER_WORD = 100;
    public const int HINT_COST = 50;
    public const int SECOND_HINT_COST = 100;

    // Properties
    public List<string> EraList => eraList;
    public List<Sprite> EraImages => eraImages;
    public string CurrentEra 
    { 
        get => currentEra;
        set => currentEra = value;
    }
    public int CurrentPoints
    {
        get => currentPoints;
        private set
        {
            currentPoints = value;
            PlayerPrefs.SetInt("CurrentPoints", value);
        }
    }
    public Dictionary<string, List<char>> InitialGrids => initialGrids;
    public Dictionary<string, List<Vector2Int>> SolvedWordPositions => solvedWordPositions;
    public List<string> UnsolvedWordsInCurrentEra => unsolvedWordsInCurrentEra;

    private void Awake()
    {
        try
        {
            Debug.Log("GameManager Awake: Starting initialization");
            
            if (Instance != null && Instance != this)
            {
                Debug.Log("GameManager: Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Validate required settings
            if (eraList == null || eraList.Count == 0)
            {
                Debug.LogError("GameManager: Era list is null or empty!");
                return;
            }

            if (eraImages == null || eraImages.Count == 0)
            {
                Debug.LogError("GameManager: Era images list is null or empty!");
                return;
            }

            if (eraList.Count != eraImages.Count)
            {
                Debug.LogError($"GameManager: Era list count ({eraList.Count}) does not match era images count ({eraImages.Count})!");
                return;
            }

            LoadSavedData();
            LoadWordSets();
            GenerateAllGrids();

            if (string.IsNullOrEmpty(CurrentEra))
            {
                StartWithRandomEra();
            }

            Debug.Log("GameManager Awake: Initialization complete");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager Awake: Critical error during initialization: {e.Message}\n{e.StackTrace}");
        }
    }

    private void LoadSavedData()
    {
        Debug.Log("GameManager: Loading saved data");
        CurrentPoints = PlayerPrefs.GetInt("CurrentPoints", 0);
        currentEra = PlayerPrefs.GetString("CurrentEra", "");
        if (!string.IsNullOrEmpty(currentEra))
        {
            currentEraIndex = eraList.IndexOf(currentEra);
        }
    }

    private void LoadWordSets()
    {
        Debug.Log("GameManager: Loading word sets");
        string filePath = Application.dataPath + "/words.json";
        
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"GameManager: words.json not found at: {filePath}");
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);
            
            if (wordSetList?.sets == null)
            {
                Debug.LogError("GameManager: Failed to parse words.json or sets is null");
                return;
            }

            wordSetsWithSentences.Clear();
            foreach (var wordSet in wordSetList.sets)
            {
                if (!string.IsNullOrEmpty(wordSet.era))
                {
                    var wordDict = new Dictionary<string, List<string>>();
                    foreach (var wordEntry in wordSet.words)
                    {
                        if (!string.IsNullOrEmpty(wordEntry.word))
                        {
                            wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences ?? new string[0]);
                        }
                    }
                    wordSetsWithSentences[wordSet.era] = wordDict;
                    Debug.Log($"GameManager: Loaded {wordDict.Count} words for era: {wordSet.era}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager: Error loading word sets: {e.Message}\n{e.StackTrace}");
        }
    }

    public void AddPoints(int points)
    {
        CurrentPoints += points;
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
        }
    }

    public void SelectEra(string eraName)
    {
        currentEra = eraName;
        currentEraIndex = eraList.IndexOf(eraName);
        
        if (!solvedWordsPerEra.ContainsKey(eraName))
        {
            solvedWordsPerEra[eraName] = new HashSet<int>();
        }
        
        if (WordGameManager.Instance != null)
        {
            WordGameManager.Instance.solvedWordsInCurrentEra = solvedWordsPerEra[eraName];
        }
        
        ResetUnsolvedWordsForEra(eraName);
    }

    private void StartWithRandomEra()
    {
        if (eraList.Count > 0)
        {
            currentEraIndex = Random.Range(0, eraList.Count);
            currentEra = eraList[currentEraIndex];
            ResetUnsolvedWordsForEra(currentEra);
        }
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
        int index = eraList.IndexOf(era);
        return index >= 0 && index < eraImages.Count ? eraImages[index] : null;
    }

    public void StoreSolvedWordPositions(string word, List<Vector2Int> positions)
    {
        solvedWordPositions[word] = positions;
    }

    public List<Vector2Int> GetSolvedWordPositions(string word)
    {
        return solvedWordPositions.ContainsKey(word) ? solvedWordPositions[word] : null;
    }

    private void GenerateAllGrids()
    {
        Debug.Log("GameManager: Starting grid generation");
        initialGrids.Clear();
        
        try
        {
            foreach (var era in wordSetsWithSentences.Keys)
            {
                if (wordSetsWithSentences[era] != null)
                {
                    foreach (var word in wordSetsWithSentences[era].Keys)
                    {
                        if (!string.IsNullOrEmpty(word) && !initialGrids.ContainsKey(word))
                        {
                            GenerateGridForWord(word);
                        }
                    }
                }
            }
            Debug.Log($"GameManager: Successfully generated {initialGrids.Count} grids");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager: Error generating grids: {e.Message}\n{e.StackTrace}");
        }
    }

    private void GenerateGridForWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return;

        List<char> grid = new List<char>();
        for (int i = 0; i < 36; i++)
        {
            grid.Add('.');
        }

        PlaceWordInGrid(word, grid);
        FillRemainingSpaces(grid);
        initialGrids[word] = grid;
        Debug.Log($"GameManager: Generated grid for word: {word}");
    }

    private void PlaceWordInGrid(string word, List<char> grid)
    {
        int gridWidth = 6;
        int position;
        bool placed = false;

        do
        {
            position = UnityEngine.Random.Range(0, grid.Count - word.Length + 1);
            
            // Check if word fits in current row
            int row = position / gridWidth;
            int endRow = (position + word.Length - 1) / gridWidth;
            
            if (row == endRow)
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
}
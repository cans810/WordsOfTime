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
    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    private List<string> unsolvedWordsInCurrentEra;
    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();
    private const int GRID_SIZE = 6;
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
        Debug.Log("GameManager Awake: Starting");
        
        if (Instance != null && Instance != this)
        {
            Debug.Log("GameManager: Destroying duplicate instance");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeCollections();
        LoadSavedData();

        try
        {
            LoadWordSets();
            if (EraList.Count > 0)
            {
                GenerateAllGrids();
                if (string.IsNullOrEmpty(CurrentEra))
                {
                    StartWithRandomEra();
                }
            }
            Debug.Log("GameManager Awake: Completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager initialization error: {e.Message}\n{e.StackTrace}");
        }
    }

    private void InitializeCollections()
    {
        if (eraList == null) eraList = new List<string>();
        if (eraImages == null) eraImages = new List<Sprite>();
        if (initialGrids == null) initialGrids = new Dictionary<string, List<char>>();
        if (solvedWordPositions == null) solvedWordPositions = new Dictionary<string, List<Vector2Int>>();
        if (solvedWordsPerEra == null) solvedWordsPerEra = new Dictionary<string, HashSet<int>>();
        if (wordSetsWithSentences == null) wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
        if (unsolvedWordsInCurrentEra == null) unsolvedWordsInCurrentEra = new List<string>();
    }

    private void LoadSavedData()
    {
        try
        {
            CurrentPoints = PlayerPrefs.GetInt("CurrentPoints", 0);
            string savedEra = PlayerPrefs.GetString("CurrentEra", "");
            if (!string.IsNullOrEmpty(savedEra) && EraList.Contains(savedEra))
            {
                CurrentEra = savedEra;
                currentEraIndex = EraList.IndexOf(savedEra);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading saved data: {e.Message}");
            CurrentPoints = 0;
            CurrentEra = "";
            currentEraIndex = -1;
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
        CurrentEra = eraName;
        currentEraIndex = EraList.IndexOf(eraName);
        
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

    private void LoadWordSets()
    {
        string filePath = Application.dataPath + "/words.json";
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"words.json not found at: {filePath}");
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);
            
            if (wordSetList?.sets == null)
            {
                Debug.LogError("Failed to parse words.json");
                return;
            }

            wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
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
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading word sets: {e.Message}");
        }
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
        for (int i = 0; i < 36; i++)
        {
            grid.Add('.');
        }

        PlaceWordInGrid(word, grid);
        FillRemainingSpaces(grid);
        InitialGrids[word] = grid;
    }

    private void PlaceWordInGrid(string word, List<char> grid)
    {
        int gridWidth = 6;
        int position;
        bool placed = false;

        do
        {
            position = UnityEngine.Random.Range(0, grid.Count - word.Length + 1);
            
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
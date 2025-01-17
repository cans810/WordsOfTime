using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<string> eraList = new List<string>() { "Ancient Egypt", "Medieval Europe", "Ancient Rome", "Renaissance", "Industrial Revolution", "Ancient Greece" };
    [SerializeField] private List<Sprite> eraImages = new List<Sprite>();
    
    private string currentEra = "";
    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();
    private Dictionary<string, List<Vector2Int>> solvedWordPositions = new Dictionary<string, List<Vector2Int>>();
    private int currentPoints = 0;

    public const int POINTS_PER_WORD = 100;
    public const int HINT_COST = 50;
    public const int SECOND_HINT_COST = 100;
    private const int GRID_SIZE = 6;

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
        set => currentPoints = value;
    }
    public Dictionary<string, List<char>> InitialGrids => initialGrids;
    public Dictionary<string, List<Vector2Int>> SolvedWordPositions => solvedWordPositions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeWordSets();
            GenerateAllGrids();
            
            if (string.IsNullOrEmpty(currentEra) && eraList.Count > 0)
            {
                currentEra = eraList[0];
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeWordSets()
    {
        // Initialize with some test data
        wordSetsWithSentences.Clear();

        // Ancient Egypt words
        var egyptWords = new Dictionary<string, List<string>>();
        egyptWords["PHARAOH"] = new List<string> { "The _____ ruled over Egypt with absolute power." };
        egyptWords["PYRAMID"] = new List<string> { "The Great _____ of Giza is one of the Seven Wonders." };
        egyptWords["SPHINX"] = new List<string> { "The _____ has the body of a lion and the head of a pharaoh." };
        wordSetsWithSentences["Ancient Egypt"] = egyptWords;

        // Ancient Rome words
        var romeWords = new Dictionary<string, List<string>>();
        romeWords["SENATE"] = new List<string> { "The Roman _____ was the governing body." };
        romeWords["LEGION"] = new List<string> { "The Roman _____ was the main unit of the army." };
        wordSetsWithSentences["Ancient Rome"] = romeWords;

        Debug.Log($"Initialized word sets with {wordSetsWithSentences.Count} eras");
    }

    private void GenerateAllGrids()
    {
        initialGrids.Clear();
        foreach (var era in wordSetsWithSentences.Keys)
        {
            foreach (var word in wordSetsWithSentences[era].Keys)
            {
                if (!initialGrids.ContainsKey(word))
                {
                    GenerateGridForWord(word);
                }
            }
        }
        Debug.Log($"Generated grids for {initialGrids.Count} words");
    }

    private void GenerateGridForWord(string word)
    {
        List<char> grid = new List<char>();
        for (int i = 0; i < GRID_SIZE * GRID_SIZE; i++)
        {
            grid.Add('.');
        }

        PlaceWordInGrid(word, grid);
        FillRemainingSpaces(grid);
        initialGrids[word] = grid;
        Debug.Log($"Generated grid for word: {word}");
    }

    private void PlaceWordInGrid(string word, List<char> grid)
    {
        int position;
        bool placed = false;

        do
        {
            position = Random.Range(0, grid.Count - word.Length + 1);
            int row = position / GRID_SIZE;
            int endRow = (position + word.Length - 1) / GRID_SIZE;
            
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
                grid[i] = alphabet[Random.Range(0, alphabet.Length)];
            }
        }
    }

    public Sprite getEraImage(string era)
    {
        int index = eraList.IndexOf(era);
        return index >= 0 && index < eraImages.Count ? eraImages[index] : null;
    }

    public void SelectEra(string eraName)
    {
        currentEra = eraName;
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

    public void StoreSolvedWordPositions(string word, List<Vector2Int> positions)
    {
        if (word != null && positions != null)
        {
            solvedWordPositions[word] = positions;
        }
    }

    public List<Vector2Int> GetSolvedWordPositions(string word)
    {
        if (word != null && solvedWordPositions.ContainsKey(word))
        {
            return solvedWordPositions[word];
        }
        return null;
    }
}
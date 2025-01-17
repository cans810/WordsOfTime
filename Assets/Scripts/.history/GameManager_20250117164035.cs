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

    // Constants
    public const int POINTS_PER_WORD = 100;
    public const int HINT_COST = 50;
    public const int SECOND_HINT_COST = 100;
    private const int GRID_SIZE = 6;

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
            
            LoadWordSets();
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

    private void LoadWordSets()
    {
        TextAsset wordSetJson = Resources.Load<TextAsset>("words");
        if (wordSetJson == null)
        {
            Debug.LogError("Failed to load words.json from Resources folder");
            return;
        }

        WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(wordSetJson.text);
        if (wordSetList?.sets == null)
        {
            Debug.LogError("Failed to parse words.json");
            return;
        }

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

    // Other methods
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
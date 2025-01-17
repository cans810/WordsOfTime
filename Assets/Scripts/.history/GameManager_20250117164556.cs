using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<string> eraList = new List<string>() { "Ancient Egypt" };
    [SerializeField] private List<Sprite> eraImages = new List<Sprite>();
    
    private string currentEra = "Ancient Egypt";
    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();
    private Dictionary<string, List<Vector2Int>> solvedWordPositions = new Dictionary<string, List<Vector2Int>>();
    private int currentPoints = 0;

    // Test words for each era
    private Dictionary<string, List<string>> eraWords = new Dictionary<string, List<string>>()
    {
        { "Ancient Egypt", new List<string>() { "PHARAOH", "PYRAMID", "SPHINX", "NILE" } }
    };

    public const int POINTS_PER_WORD = 100;
    public const int HINT_COST = 50;
    public const int SECOND_HINT_COST = 100;
    private const int GRID_SIZE = 6;

    public List<string> EraList => eraList;
    public List<Sprite> EraImages => eraImages;
    public string CurrentEra => currentEra;
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
            GenerateAllGrids();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void GenerateAllGrids()
    {
        initialGrids.Clear();
        foreach (var era in eraWords.Keys)
        {
            foreach (var word in eraWords[era])
            {
                if (!initialGrids.ContainsKey(word))
                {
                    List<char> grid = new List<char>();
                    for (int i = 0; i < GRID_SIZE * GRID_SIZE; i++)
                    {
                        grid.Add('.');
                    }

                    // Place the word horizontally at a random position
                    int row = Random.Range(0, GRID_SIZE);
                    int startCol = Random.Range(0, GRID_SIZE - word.Length + 1);
                    int startPos = row * GRID_SIZE + startCol;

                    // Place the word
                    for (int i = 0; i < word.Length; i++)
                    {
                        grid[startPos + i] = word[i];
                    }

                    // Fill remaining spaces with random letters
                    string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    for (int i = 0; i < grid.Count; i++)
                    {
                        if (grid[i] == '.')
                        {
                            grid[i] = alphabet[Random.Range(0, alphabet.Length)];
                        }
                    }

                    initialGrids[word] = grid;
                    Debug.Log($"Generated grid for word: {word}");
                }
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

    // Helper method to get words for the current era
    public List<string> GetCurrentEraWords()
    {
        if (eraWords.ContainsKey(currentEra))
        {
            return eraWords[currentEra];
        }
        return new List<string>();
    }
}
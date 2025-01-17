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

    // Constants needed by other scripts
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

    public Sprite getEraImage(string era)
    {
        int index = eraList.IndexOf(era);
        return index >= 0 && index < eraImages.Count ? eraImages[index] : null;
    }

    public void SelectEra(string eraName)
    {
        currentEra = eraName;
    }

    // Methods needed by other scripts
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
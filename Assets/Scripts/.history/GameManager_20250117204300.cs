using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<string> eraList = new List<string>() { "" };
    [SerializeField] private List<Sprite> eraImages = new List<Sprite>();
    
    private string currentEra;
    public string CurrentEra 
    { 
        get { return currentEra; }
        private set { currentEra = value; }
    }
    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();
    private Dictionary<string, List<Vector2Int>> solvedWordPositions = new Dictionary<string, List<Vector2Int>>();
    private HashSet<string> solvedWords = new HashSet<string>();
    private int currentPoints = 0;

    // Dictionary to store words and sentences for each era
    private Dictionary<string, List<string>> eraWords = new Dictionary<string, List<string>>();
    private Dictionary<string, Dictionary<string, List<string>>> wordSentences = 
        new Dictionary<string, Dictionary<string, List<string>>>();

    private Dictionary<string, HashSet<int>> solvedWordsPerEra = new Dictionary<string, HashSet<int>>();

    public const int POINTS_PER_WORD = 100;
    public const int HINT_COST = 50;
    public const int SECOND_HINT_COST = 100;
    private const int GRID_SIZE = 6;

    public List<string> EraList => eraList;
    public List<Sprite> EraImages => eraImages;
    public int CurrentPoints
    {
        get { return currentPoints; }
        private set 
        { 
            currentPoints = value;
            OnPointsChanged?.Invoke();
        }
    }
    public Dictionary<string, List<char>> InitialGrids => initialGrids;
    public Dictionary<string, List<Vector2Int>> SolvedWordPositions => solvedWordPositions;

    public delegate void PointsChangedHandler();
    public event PointsChangedHandler OnPointsChanged;

    private Dictionary<string, List<string>> shuffledEraWords = new Dictionary<string, List<string>>();
    private bool hasShuffledWords = false;

    private Dictionary<string, int> eraPrices = new Dictionary<string, int>()
    {
        { "Ancient Egypt", 0 },        
        { "Medieval Europe", 0 },   
        { "Renaissance", 1000 },   
        { "Industrial Revolution", 2000 } 
        { "Ancient Greece", 3000 },   
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadWordsFromJson();
            GenerateAllGrids();
            
            // Shuffle words only once when game starts
            if (!hasShuffledWords)
            {
                ShuffleAllEraWords();
                hasShuffledWords = true;
            }
            
            // Start with Ancient Egypt
            CurrentEra = "Ancient Egypt";
            Debug.Log($"Starting with era: {CurrentEra}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadWordsFromJson()
    {
        try
        {
            string filePath = Path.Combine(Application.dataPath, "words.json");
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(jsonContent);

                eraList.Clear(); // Clear default values
                eraWords.Clear();
                wordSentences.Clear();

                foreach (var set in wordSetList.sets)
                {
                    string era = set.era;
                    eraList.Add(era);
                    
                    List<string> words = new List<string>();
                    Dictionary<string, List<string>> sentences = new Dictionary<string, List<string>>();

                    foreach (var wordEntry in set.words)
                    {
                        string word = wordEntry.word.ToUpper();
                        words.Add(word);
                        sentences[word] = new List<string>(wordEntry.sentences);
                    }

                    eraWords[era] = words;
                    wordSentences[era] = sentences;
                }

                Debug.Log($"Successfully loaded {eraList.Count} eras from JSON");
                if (string.IsNullOrEmpty(currentEra) && eraList.Count > 0)
                {
                    currentEra = eraList[0];
                }
            }
            else
            {
                Debug.LogError($"words.json not found at path: {filePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading words from JSON: {e.Message}");
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
                    // Initialize grid with dots
                    List<char> grid = new List<char>(new char[GRID_SIZE * GRID_SIZE]);
                    for (int i = 0; i < GRID_SIZE * GRID_SIZE; i++)
                    {
                        grid[i] = '.';
                    }

                    if (word.Length <= GRID_SIZE * GRID_SIZE)
                    {
                        int maxAttempts = 300;
                        bool placed = false;
                        List<Vector2Int> wordPath = new List<Vector2Int>();
                        
                        while (!placed && maxAttempts > 0)
                        {
                            try
                            {
                                List<Vector2Int> possibleStarts = new List<Vector2Int>();
                                for (int y = 0; y < GRID_SIZE; y++)
                                {
                                    for (int x = 0; x < GRID_SIZE; x++)
                                    {
                                        possibleStarts.Add(new Vector2Int(x, y));
                                    }
                                }

                                for (int i = possibleStarts.Count - 1; i > 0; i--)
                                {
                                    int rnd = Random.Range(0, i + 1);
                                    var temp = possibleStarts[i];
                                    possibleStarts[i] = possibleStarts[rnd];
                                    possibleStarts[rnd] = temp;
                                }

                                foreach (var startPos in possibleStarts)
                                {
                                    wordPath.Clear();
                                    wordPath.Add(startPos);
                                    Vector2Int currentPos = startPos;
                                    bool validPath = true;

                                    for (int i = 1; i < word.Length; i++)
                                    {
                                        List<Vector2Int> possibleMoves = new List<Vector2Int>();
                                        Vector2Int[] directions = new Vector2Int[] {
                                            new Vector2Int(0, -1),  // up
                                            new Vector2Int(1, 0),   // right
                                            new Vector2Int(0, 1),   // down
                                            new Vector2Int(-1, 0)   // left
                                        };

                                        for (int j = directions.Length - 1; j > 0; j--)
                                        {
                                            int rnd = Random.Range(0, j + 1);
                                            var temp = directions[j];
                                            directions[j] = directions[rnd];
                                            directions[rnd] = temp;
                                        }

                                        bool foundMove = false;
                                        foreach (var dir in directions)
                                        {
                                            Vector2Int nextPos = currentPos + dir;
                                            if (nextPos.x >= 0 && nextPos.x < GRID_SIZE && 
                                                nextPos.y >= 0 && nextPos.y < GRID_SIZE && 
                                                !wordPath.Contains(nextPos))
                                            {
                                                currentPos = nextPos;
                                                wordPath.Add(currentPos);
                                                foundMove = true;
                                                break;
                                            }
                                        }

                                        if (!foundMove)
                                        {
                                            validPath = false;
                                            break;
                                        }
                                    }

                                    if (validPath)
                                    {
                                        for (int i = 0; i < word.Length; i++)
                                        {
                                            int gridIndex = wordPath[i].y * GRID_SIZE + wordPath[i].x;
                                            grid[gridIndex] = word[i];
                                        }
                                        placed = true;
                                        solvedWordPositions[word] = wordPath;
                                        break;
                                    }
                                }

                                if (placed) break;
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"Error placing word {word}: {e.Message}");
                            }
                            maxAttempts--;
                        }

                        if (!placed)
                        {
                            Debug.LogError($"Failed to place word: {word}");
                            continue;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Word {word} is too long for grid size {GRID_SIZE}");
                        continue;
                    }

                    string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    for (int i = 0; i < grid.Count; i++)
                    {
                        if (grid[i] == '.')
                        {
                            grid[i] = alphabet[Random.Range(0, alphabet.Length)];
                        }
                    }

                    initialGrids[word] = grid;
                    Debug.Log($"Generated snake grid for word: {word}");
                }
            }
        }
    }

    private void ShuffleAllEraWords()
    {
        System.Random rng = new System.Random();
        foreach (var era in eraWords.Keys)
        {
            List<string> words = new List<string>(eraWords[era]);
            int n = words.Count;
            
            // Fisher-Yates shuffle
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string temp = words[k];
                words[k] = words[n];
                words[n] = temp;
            }
            
            shuffledEraWords[era] = words;
            Debug.Log($"Shuffled words for {era}: {string.Join(", ", words)}");
        }
    }

    public List<string> GetCurrentEraWords()
    {
        if (shuffledEraWords.ContainsKey(currentEra))
        {
            return new List<string>(shuffledEraWords[currentEra]);
        }
        return new List<string>();
    }

    public List<string> GetSentencesForWord(string word)
    {
        if (wordSentences.ContainsKey(currentEra) && 
            wordSentences[currentEra].ContainsKey(word))
        {
            return wordSentences[currentEra][word];
        }
        return new List<string>();
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
        solvedWordPositions[word] = positions;
        solvedWords.Add(word);
    }

    public bool IsWordSolved(string word)
    {
        return solvedWords.Contains(word);
    }

    public List<Vector2Int> GetSolvedWordPositions(string word)
    {
        if (solvedWordPositions.ContainsKey(word))
        {
            return solvedWordPositions[word];
        }
        return null;
    }

    public void RestoreSolvedStates(string word, LetterTile[,] grid)
    {
        if (IsWordSolved(word) && solvedWordPositions.ContainsKey(word))
        {
            foreach (Vector2Int pos in solvedWordPositions[word])
            {
                if (pos.x >= 0 && pos.x < grid.GetLength(0) && 
                    pos.y >= 0 && pos.y < grid.GetLength(1))
                {
                    grid[pos.x, pos.y].SetSolvedColor();
                }
            }
        }
    }

    public void StoreSolvedWordIndex(string era, int wordIndex)
    {
        if (!solvedWordsPerEra.ContainsKey(era))
        {
            solvedWordsPerEra[era] = new HashSet<int>();
        }
        solvedWordsPerEra[era].Add(wordIndex);
        Debug.Log($"Stored solved word index {wordIndex} for era {era}. Total solved in era: {solvedWordsPerEra[era].Count}");
    }

    public HashSet<int> GetSolvedWordsForEra(string era)
    {
        if (solvedWordsPerEra.ContainsKey(era))
        {
            return new HashSet<int>(solvedWordsPerEra[era]);
        }
        return new HashSet<int>();
    }

    public void SwitchEra(string newEra)
    {
        CurrentEra = newEra;
        if (!solvedWordsPerEra.ContainsKey(newEra))
        {
            solvedWordsPerEra[newEra] = new HashSet<int>();
        }
    }

    public void ClearProgress()
    {
        solvedWordsPerEra.Clear();
    }

    public int GetEraPrice(string era)
    {
        return eraPrices.ContainsKey(era) ? eraPrices[era] : 0;
    }
}
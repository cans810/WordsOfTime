using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<string> eraList = new List<string>() { "Ancient Egypt" };
    [SerializeField] private List<Sprite> eraImages = new List<Sprite>();
    
    private string currentEra = "Ancient Egypt";
    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();
    private Dictionary<string, List<Vector2Int>> solvedWordPositions = new Dictionary<string, List<Vector2Int>>();
    private int currentPoints = 0;

    // Dictionary to store words and sentences for each era
    private Dictionary<string, List<string>> eraWords = new Dictionary<string, List<string>>();
    private Dictionary<string, Dictionary<string, List<string>>> wordSentences = 
        new Dictionary<string, Dictionary<string, List<string>>>();

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
            LoadWordsFromJson();
            GenerateAllGrids();
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

                    // Make sure word fits in grid
                    if (word.Length <= GRID_SIZE * GRID_SIZE)
                    {
                        // Try to place the word in a snake pattern
                        int maxAttempts = 100;
                        bool placed = false;
                        List<Vector2Int> wordPath = new List<Vector2Int>();
                        
                        while (!placed && maxAttempts > 0)
                        {
                            try
                            {
                                // Start from a random position
                                int startRow = Random.Range(0, GRID_SIZE);
                                int startCol = Random.Range(0, GRID_SIZE);
                                Vector2Int currentPos = new Vector2Int(startCol, startRow);
                                wordPath.Clear();
                                wordPath.Add(currentPos);
                                bool validPath = true;

                                // Try to build a snake path for the word
                                for (int i = 1; i < word.Length; i++)
                                {
                                    List<Vector2Int> possibleMoves = new List<Vector2Int>();
                                    
                                    // Check all adjacent cells (up, right, down, left)
                                    Vector2Int[] directions = new Vector2Int[] {
                                        new Vector2Int(0, -1),  // up
                                        new Vector2Int(1, 0),   // right
                                        new Vector2Int(0, 1),   // down
                                        new Vector2Int(-1, 0)   // left
                                    };

                                    foreach (var dir in directions)
                                    {
                                        Vector2Int nextPos = currentPos + dir;
                                        
                                        // Check if the position is valid and not already used
                                        if (nextPos.x >= 0 && nextPos.x < GRID_SIZE && 
                                            nextPos.y >= 0 && nextPos.y < GRID_SIZE && 
                                            !wordPath.Contains(nextPos))
                                        {
                                            possibleMoves.Add(nextPos);
                                        }
                                    }

                                    if (possibleMoves.Count > 0)
                                    {
                                        // Choose a random valid move
                                        currentPos = possibleMoves[Random.Range(0, possibleMoves.Count)];
                                        wordPath.Add(currentPos);
                                    }
                                    else
                                    {
                                        validPath = false;
                                        break;
                                    }
                                }

                                if (validPath)
                                {
                                    // Place the word along the path
                                    for (int i = 0; i < word.Length; i++)
                                    {
                                        int gridIndex = wordPath[i].y * GRID_SIZE + wordPath[i].x;
                                        grid[gridIndex] = word[i];
                                    }
                                    placed = true;
                                    
                                    // Store the word path for later use
                                    solvedWordPositions[word] = wordPath;
                                }
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
                        }
                    }
                    else
                    {
                        Debug.LogError($"Word {word} is too long for grid size {GRID_SIZE}");
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
                    Debug.Log($"Generated snake grid for word: {word}");
                }
            }
        }
        Debug.Log($"Generated {initialGrids.Count} snake grids in total");
    }

    public List<string> GetCurrentEraWords()
    {
        if (eraWords.ContainsKey(currentEra))
        {
            return eraWords[currentEra];
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
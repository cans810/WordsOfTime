using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour{
    public static GameManager Instance { get; private set; }

    public List<string> EraList = new List<string>();
    public string CurrentEra { get; set; } = "";
    private int currentEraIndex = -1;

    public List<Sprite> eraImages = new List<Sprite>();

    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    public List<string> unsolvedWordsInCurrentEra;

    private Dictionary<string, Dictionary<string, PreGeneratedGrid>> preGeneratedGrids; // era -> word -> grid


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
        StartWithRandomEra();
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

    // Structure to store pre-generated grid data
    public class PreGeneratedGrid
    {
        public char[,] letters;  // The grid of letters
        public List<Vector2Int> wordPositions;  // Positions of the target word's letters
        public Dictionary<char, List<Vector2Int>> letterPositions;  // All positions for each letter
        
        public PreGeneratedGrid(int size)
        {
            letters = new char[size, size];
            wordPositions = new List<Vector2Int>();
            letterPositions = new Dictionary<char, List<Vector2Int>>();
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadWordSets();
            PreGenerateAllGrids();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void PreGenerateAllGrids()
    {
        preGeneratedGrids = new Dictionary<string, Dictionary<string, PreGeneratedGrid>>();
        
        foreach (var era in wordSetsWithSentences.Keys)
        {
            preGeneratedGrids[era] = new Dictionary<string, PreGeneratedGrid>();
            foreach (var word in wordSetsWithSentences[era].Keys)
            {
                preGeneratedGrids[era][word] = GenerateGridForWord(word);
            }
        }
        
        Debug.Log($"Pre-generated grids for {preGeneratedGrids.Count} eras");
    }

    private PreGeneratedGrid GenerateGridForWord(string word)
    {
        const int GRID_SIZE = 5;
        PreGeneratedGrid grid = new PreGeneratedGrid(GRID_SIZE);
        
        // Place the word's letters in adjacent positions
        List<Vector2Int> validStartPositions = new List<Vector2Int>();
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                validStartPositions.Add(new Vector2Int(x, y));
            }
        }
        
        // Shuffle start positions for randomness
        for (int i = validStartPositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = validStartPositions[i];
            validStartPositions[i] = validStartPositions[j];
            validStartPositions[j] = temp;
        }

        foreach (var startPos in validStartPositions)
        {
            if (TryPlaceWord(word, startPos, grid, GRID_SIZE))
            {
                FillRemainingSpaces(grid, GRID_SIZE);
                return grid;
            }
        }

        Debug.LogError($"Failed to generate grid for word: {word}");
        return null;
    }

    private bool TryPlaceWord(string word, Vector2Int startPos, PreGeneratedGrid grid, int gridSize)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int currentPos = startPos;

        for (int i = 0; i < word.Length; i++)
        {
            if (!IsValidPosition(currentPos, gridSize) || grid.letters[currentPos.x, currentPos.y] != '\0')
            {
                return false;
            }

            grid.letters[currentPos.x, currentPos.y] = word[i];
            positions.Add(currentPos);

            if (!grid.letterPositions.ContainsKey(word[i]))
            {
                grid.letterPositions[word[i]] = new List<Vector2Int>();
            }
            grid.letterPositions[word[i]].Add(currentPos);

            if (i < word.Length - 1)
            {
                var nextPos = GetNextValidPosition(currentPos, grid, gridSize);
                if (!nextPos.HasValue) return false;
                currentPos = nextPos.Value;
            }
        }

        grid.wordPositions = positions;
        return true;
    }

    private Vector2Int? GetNextValidPosition(Vector2Int current, PreGeneratedGrid grid, int gridSize)
    {
        Vector2Int[] directions = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1)
        };

        List<Vector2Int> validPositions = new List<Vector2Int>();

        foreach (var dir in directions)
        {
            Vector2Int newPos = current + dir;
            if (IsValidPosition(newPos, gridSize) && grid.letters[newPos.x, newPos.y] == '\0')
            {
                validPositions.Add(newPos);
            }
        }

        if (validPositions.Count == 0) return null;
        return validPositions[Random.Range(0, validPositions.Count)];
    }

    private void FillRemainingSpaces(PreGeneratedGrid grid, int gridSize)
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid.letters[x, y] == '\0')
                {
                    char randomLetter = (char)Random.Range('A', 'Z' + 1);
                    grid.letters[x, y] = randomLetter;
                    
                    if (!grid.letterPositions.ContainsKey(randomLetter))
                    {
                        grid.letterPositions[randomLetter] = new List<Vector2Int>();
                    }
                    grid.letterPositions[randomLetter].Add(new Vector2Int(x, y));
                }
            }
        }
    }

    private bool IsValidPosition(Vector2Int pos, int gridSize)
    {
        return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
    }

    public PreGeneratedGrid GetPreGeneratedGrid(string era, string word)
    {
        if (preGeneratedGrids.TryGetValue(era, out var eraGrids))
        {
            if (eraGrids.TryGetValue(word.ToUpper(), out var grid))
            {
                return grid;
            }
        }
        return null;
    }
}
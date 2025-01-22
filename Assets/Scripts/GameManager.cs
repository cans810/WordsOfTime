using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
            }
            return _instance;
        }
    }

    [SerializeField] private List<string> eraList = new List<string>() { "Ancient Egypt", "Ancient Greece", "Medieval Europe", "Renaissance", "Industrial Revolution" };
    [SerializeField] private List<Sprite> eraImages = new List<Sprite>();
    
    private string currentEra;
    public string CurrentEra 
    { 
        get => currentEra;
        set
        {
            if (currentEra != value)
            {
                currentEra = value;
                OnEraChanged?.Invoke();
                // If we have a SoundManager, update the music
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayEraMusic(value);
                }
            }
        }
    }
    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();
    private Dictionary<string, List<Vector2Int>> solvedWordPositions = new Dictionary<string, List<Vector2Int>>();
    private HashSet<string> solvedWords = new HashSet<string>();
    private int currentPoints;

    // Dictionary to store words and sentences for each language and era
    private Dictionary<string, Dictionary<string, List<string>>> eraWordsPerLanguage = 
        new Dictionary<string, Dictionary<string, List<string>>>();
    private Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> wordSentencesPerLanguage = 
        new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();

    private Dictionary<string, HashSet<int>> solvedWordsPerEra = new Dictionary<string, HashSet<int>>();

    public const int POINTS_PER_WORD = 100;
    public const int HINT_COST = 50;
    public const int SECOND_HINT_COST = 100;
    private const int GRID_SIZE = 6;

    public List<string> EraList => eraList;
    public List<Sprite> EraImages => eraImages;
    public int CurrentPoints => currentPoints;
    public Dictionary<string, List<char>> InitialGrids => initialGrids;
    public Dictionary<string, List<Vector2Int>> SolvedWordPositions => solvedWordPositions;

    public delegate void PointsChangedHandler(int points);
    public event PointsChangedHandler OnPointsChanged;

    private Dictionary<string, List<string>> shuffledEraWords = new Dictionary<string, List<string>>();
    private bool hasShuffledWords = false;

    private Dictionary<string, int> eraPrices = new Dictionary<string, int>()
    {
        { "Ancient Egypt", 0 },        
        { "Medieval Europe", 0 },   
        { "Renaissance", 1000 },   
        { "Industrial Revolution", 2000 }, 
        { "Ancient Greece", 3000 },   
    };

    // Dictionary to store used hints: <Era_Word, HintLevel>
    private Dictionary<string, HashSet<int>> usedHints = new Dictionary<string, HashSet<int>>();

    // Add language-related fields
    private string currentLanguage = "en"; // Default language
    public string CurrentLanguage 
    { 
        get { return currentLanguage; }
        private set { currentLanguage = value; }
    }

    public delegate void LanguageChangeHandler();
    public event LanguageChangeHandler OnLanguageChanged;

    public event System.Action OnEraChanged;

    private GameSettings currentSettings;

    private readonly List<string> allEras = new List<string>
    {
        "Ancient Egypt",
        "Medieval Europe",
        "Industrial Revolution"
        // Add any other eras you have
    };

    private HashSet<string> guessedWords = new HashSet<string>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
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

        // Start playing music for the current era
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEraMusic(CurrentEra);
        }

        // Initialize default settings
        currentSettings = new GameSettings();
    }

    private void Start()
    {
        Debug.Log("GameManager Start");
        
        // Load save file if it exists
        if (SaveManager.Instance != null)
        {
            Debug.Log("SaveManager instance found, loading game...");
            SaveManager.Instance.LoadGame();
        }
        else
        {
            Debug.LogWarning("SaveManager instance not found!");
        }
        
        // Debug print current guessed words
        Debug.Log($"Guessed words after load: {string.Join(", ", guessedWords)}");
        
        // If no grids were loaded from save, generate new ones
        if (initialGrids.Count == 0)
        {
            Debug.Log("No grids found, generating new ones...");
            GenerateAllGrids();
            SaveManager.Instance.SaveGame();
        }

        Debug.Log($"Initial grids count: {initialGrids.Count}");
        Debug.Log($"Solved positions count: {solvedWordPositions.Count}");

        // Apply loaded or generated state
        ApplyGuessedWordsState();
    }

    private void ApplyGuessedWordsState()
    {
        Debug.Log($"Applying guessed words state. Count: {guessedWords.Count}");
        if (guessedWords.Count > 0 && GridManager.Instance != null)
        {
            foreach (string word in guessedWords)
            {
                Debug.Log($"Processing guessed word: {word}");
                if (solvedWordPositions.ContainsKey(word))
                {
                    Debug.Log($"Found positions for word: {word}");
                    GridManager.Instance.ShowSolvedWord(word, solvedWordPositions[word]);
                }
                else
                {
                    Debug.LogWarning($"No positions found for guessed word: {word}");
                }
            }
        }
    }

    private void LoadWordsFromJson()
    {
        try
        {
            // Load English words
            string enFilePath = Path.Combine(Application.dataPath, "words.json");
            if (File.Exists(enFilePath))
            {
                LoadLanguageWords(enFilePath, "en");
            }

            // Load Turkish words
            string trFilePath = Path.Combine(Application.dataPath, "words_tr.json");
            if (File.Exists(trFilePath))
            {
                LoadLanguageWords(trFilePath, "tr");
            }

            Debug.Log($"Successfully loaded words for {eraWordsPerLanguage.Count} languages");
            if (string.IsNullOrEmpty(currentEra) && eraList.Count > 0)
            {
                currentEra = eraList[0];
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading words from JSON: {e.Message}");
        }
    }

    private void LoadLanguageWords(string filePath, string language)
    {
        try 
        {
            string jsonContent = File.ReadAllText(filePath);
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(jsonContent);
            
            if (!eraWordsPerLanguage.ContainsKey(language))
            {
                eraWordsPerLanguage[language] = new Dictionary<string, List<string>>();
                wordSentencesPerLanguage[language] = new Dictionary<string, Dictionary<string, List<string>>>();
            }

            foreach (var set in wordSetList.sets)
            {
                // Use original era name for storage
                string era = set.era;
                
                // Map Turkish era names to English ones for internal use
                string internalEra = language == "tr" ? MapTurkishEraName(era) : era;
                
                if (!eraWordsPerLanguage[language].ContainsKey(internalEra))
                {
                    eraWordsPerLanguage[language][internalEra] = new List<string>();
                    wordSentencesPerLanguage[language][internalEra] = new Dictionary<string, List<string>>();
                }

                foreach (var wordEntry in set.words)
                {
                    string word = wordEntry.word.ToUpper();
                    eraWordsPerLanguage[language][internalEra].Add(word);
                    wordSentencesPerLanguage[language][internalEra][word] = new List<string>(wordEntry.sentences);
                    Debug.Log($"Loaded {language} word: {word} with {wordEntry.sentences.Count()} sentences for era {internalEra}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading {language} words from {filePath}: {e.Message}");
        }
    }

    private string MapTurkishEraName(string turkishEra)
    {
        Dictionary<string, string> eraMapping = new Dictionary<string, string>
        {
            {"Antik Mısır", "Ancient Egypt"},
            {"Antik Yunan", "Ancient Greece"},
            {"Orta Çağ Avrupası", "Medieval Europe"},
            {"Rönesans", "Renaissance"},
            {"Sanayi Devrimi", "Industrial Revolution"}
        };

        if (eraMapping.ContainsKey(turkishEra))
        {
            Debug.Log($"Mapped Turkish era '{turkishEra}' to '{eraMapping[turkishEra]}'");
            return eraMapping[turkishEra];
        }
        return turkishEra;
    }

    private void GenerateAllGrids()
    {
        initialGrids.Clear();
        foreach (var era in eraWordsPerLanguage[currentLanguage].Keys)
        {
            foreach (var word in eraWordsPerLanguage[currentLanguage][era])
            {
                if (!initialGrids.ContainsKey(word))
                {
                    List<char> grid = new List<char>(new char[GRID_SIZE * GRID_SIZE]);
                    for (int i = 0; i < grid.Capacity; i++)
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
                                    int rnd = UnityEngine.Random.Range(0, i + 1);
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
                                        Vector2Int[] directions = new Vector2Int[]
                                        {
                                            new Vector2Int(0, -1),  // up
                                            new Vector2Int(1, 0),   // right
                                            new Vector2Int(0, 1),   // down
                                            new Vector2Int(-1, 0)   // left
                                        };

                                        for (int j = directions.Length - 1; j > 0; j--)
                                        {
                                            int rnd = UnityEngine.Random.Range(0, j + 1);
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
                            grid[i] = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
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
        foreach (var era in eraWordsPerLanguage[currentLanguage].Keys)
        {
            List<string> words = new List<string>(eraWordsPerLanguage[currentLanguage][era]);
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
        if (eraWordsPerLanguage.ContainsKey(currentLanguage) && 
            eraWordsPerLanguage[currentLanguage].ContainsKey(currentEra))
        {
            return new List<string>(eraWordsPerLanguage[currentLanguage][currentEra]);
        }
        return new List<string>();
    }

    public List<string> GetSentencesForWord(string word, string era)
    {
        if (wordSentencesPerLanguage.ContainsKey(currentLanguage) && 
            wordSentencesPerLanguage[currentLanguage].ContainsKey(era) && 
            wordSentencesPerLanguage[currentLanguage][era].ContainsKey(word))
        {
            var sentences = wordSentencesPerLanguage[currentLanguage][era][word];
            Debug.Log($"Found {sentences.Count} sentences for word {word} in {currentLanguage}");
            return sentences;
        }
        Debug.LogError($"No sentences found for word {word} in {currentLanguage} for era {era}");
        return new List<string>();
    }

    public string GetRandomSentenceForWord(string word, string era)
    {
        var sentences = GetSentencesForWord(word, era);
        if (sentences.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, sentences.Count);
            return sentences[randomIndex];
        }
        Debug.LogError($"No sentences available for word {word}");
        return $"_____ {(currentLanguage == "tr" ? "için cümle bulunamadı" : "sentence not found")}";
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
        currentPoints += points;
        OnPointsChanged?.Invoke(currentPoints);
    }

    public bool CanUseHint(int hintLevel)
    {
        int cost = hintLevel == 1 ? HINT_COST : SECOND_HINT_COST;
        return currentPoints >= cost;
    }

    public void UseHint(int hintLevel)
    {
        int cost = hintLevel == 1 ? HINT_COST : SECOND_HINT_COST;
        if (currentPoints >= cost)
        {
            currentPoints -= cost;
            OnPointsChanged?.Invoke(currentPoints);
            Debug.Log($"Used hint level {hintLevel}. Deducted {cost} points");
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
        // Play the corresponding era music
        SoundManager.Instance.PlayEraMusic(newEra);
        
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

    public bool IsEraUnlocked(string era)
    {
        return currentPoints >= GetEraPrice(era); // Check if the player can afford the era
    }

    public bool CanUnlockEra(string era)
    {
        return IsEraUnlocked(era); // Reuse the IsEraUnlocked method
    }

    // Store hint usage for a specific word and level
    public void StoreHintUsage(string word, int hintLevel)
    {
        string key = $"{CurrentEra}_{word}";
        if (!usedHints.ContainsKey(key))
        {
            usedHints[key] = new HashSet<int>();
        }
        usedHints[key].Add(hintLevel);
        Debug.Log($"Stored hint level {hintLevel} for {word}. Total hints: {usedHints[key].Count}");
    }

    // Get current hint level for a word
    public int GetHintLevel(string word)
    {
        string key = $"{CurrentEra}_{word}";
        if (!usedHints.ContainsKey(key)) return 0;
        return usedHints[key].Count;
    }

    // Check if a specific hint level has been used for a word
    public bool HasUsedHint(string word, int hintLevel)
    {
        string key = $"{CurrentEra}_{word}";
        return usedHints.ContainsKey(key) && usedHints[key].Contains(hintLevel);
    }

    // Check if a specific hint level is available for a word
    public bool IsHintAvailable(string word, int hintLevel)
    {
        int currentLevel = GetHintLevel(word);
        
        // First hint is available if no hints have been used
        if (hintLevel == 1)
        {
            return currentLevel == 0;
        }
        // Second hint is available if only first hint has been used
        else if (hintLevel == 2)
        {
            return currentLevel == 1;
        }
        
        return false;
    }

    // Add method to change language
    public void SetLanguage(string languageCode)
    {
        Debug.Log($"SetLanguage called with: {languageCode}"); // Debug log
        if (currentLanguage != languageCode)
        {
            currentLanguage = languageCode;
            PlayerPrefs.SetString("Language", languageCode);
            PlayerPrefs.Save();
            
            Debug.Log($"Language changed to: {languageCode}, invoking OnLanguageChanged"); // Debug log
            if (OnLanguageChanged != null)
            {
                OnLanguageChanged.Invoke();
            }
            else
            {
                Debug.LogWarning("OnLanguageChanged has no subscribers!"); // Debug warning
            }
        }
    }

    public int GetRequiredPointsForEra(string eraName)
    {
        // Convert era name to proper format if needed (e.g., "ancientegypt" to "Ancient Egypt")
        string formattedEraName = eraName.ToLower() switch
        {
            "ancientegypt" => "Ancient Egypt",
            "medievaleurope" => "Medieval Europe",
            "renaissance" => "Renaissance",
            "ındustrialrevolution" => "Industrial Revolution",
            "ancientgreece" => "Ancient Greece",
            _ => eraName
        };

        // Get price from eraPrices dictionary
        if (eraPrices.ContainsKey(formattedEraName))
        {
            return eraPrices[formattedEraName];
        }

        Debug.LogWarning($"Unknown era: {eraName}, returning 0 points");
        return 0;
    }

    public void SetPoints(int points)
    {
        currentPoints = points;
        OnPointsChanged?.Invoke(currentPoints);
    }

    public GameSettings GetSettings()
    {
        // Create new settings based on current state
        GameSettings settings = new GameSettings
        {
            musicEnabled = SoundManager.Instance.IsMusicOn,
            soundEnabled = SoundManager.Instance.IsSoundOn,
            musicVolume = SoundManager.Instance.MusicVolume,
            soundVolume = SoundManager.Instance.SoundVolume
        };
        return settings;
    }

    public void LoadSettings(GameSettings settings)
    {
        currentSettings = settings;
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.IsMusicOn = currentSettings.musicEnabled;
            SoundManager.Instance.IsSoundOn = currentSettings.soundEnabled;
            SoundManager.Instance.SetMusicVolume(currentSettings.musicVolume);
            SoundManager.Instance.SetSoundVolume(currentSettings.soundVolume);
        }
    }

    public List<string> GetAllEras()
    {
        return allEras;
    }

    // Add these methods for SaveManager to use
    public void SaveGridData(out List<GridData> gridDataList)
    {
        gridDataList = new List<GridData>();
        
        foreach (var kvp in initialGrids)
        {
            GridData gridData = new GridData
            {
                targetWord = kvp.Key,
                letters = kvp.Value.Select(c => c.ToString()).ToList(),
                gridSize = GRID_SIZE,
                isSolved = guessedWords.Contains(kvp.Key)  // Set solved state
            };

            if (solvedWordPositions.ContainsKey(kvp.Key))
            {
                gridData.correctWordPositions = solvedWordPositions[kvp.Key]
                    .Select(v => new Vector2IntSerializable(v.x, v.y))
                    .ToList();
            }

            gridDataList.Add(gridData);
        }
    }

    public void LoadGridData(List<GridData> gridDataList)
    {
        if (gridDataList == null || gridDataList.Count == 0)
        {
            return;
        }

        initialGrids.Clear();
        solvedWordPositions.Clear();

        foreach (var gridData in gridDataList)
        {
            List<char> charList = gridData.letters.Select(s => s[0]).ToList();
            initialGrids[gridData.targetWord] = charList;

            List<Vector2Int> positions = gridData.correctWordPositions
                .Select(v => new Vector2Int(v.x, v.y))
                .ToList();
            solvedWordPositions[gridData.targetWord] = positions;

            if (gridData.isSolved)
            {
                guessedWords.Add(gridData.targetWord);
            }
        }

        Debug.Log($"Loaded {gridDataList.Count} grids from save file");
    }

    public List<string> GetGuessedWords()
    {
        return guessedWords.ToList();
    }

    public void SetGuessedWords(List<string> words)
    {
        Debug.Log($"Setting guessed words. Count: {words?.Count ?? 0}");
        if (words != null)
        {
            Debug.Log($"Words being set: {string.Join(", ", words)}");
            guessedWords = new HashSet<string>(words);
            ApplyGuessedWordsState();
        }
        else
        {
            Debug.LogWarning("Received null words list!");
            guessedWords = new HashSet<string>();
        }
    }

    public void AddGuessedWord(string word)
    {
        Debug.Log($"Adding guessed word: {word}");
        guessedWords.Add(word);
        Debug.Log($"Current guessed words: {string.Join(", ", guessedWords)}");
    }

    public bool IsWordGuessed(string word)
    {
        return guessedWords.Contains(word);
    }

    public List<Vector2Int> GetWordPath(string word)
    {
        if (solvedWordPositions.ContainsKey(word))
        {
            return solvedWordPositions[word];
        }
        return null;
    }

    public void OnWordGuessed(string word)
    {
        Debug.Log($"Word guessed: {word}");
        if (!guessedWords.Contains(word))
        {
            AddGuessedWord(word);
            if (solvedWordPositions.ContainsKey(word))
            {
                Debug.Log($"Showing solved word: {word}");
                GridManager.Instance.ShowSolvedWord(word, solvedWordPositions[word]);
            }
            else
            {
                Debug.LogWarning($"No positions found for newly guessed word: {word}");
            }
            SaveManager.Instance.SaveGame();
        }
    }
}



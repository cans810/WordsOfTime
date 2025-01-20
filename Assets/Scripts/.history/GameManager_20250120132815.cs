using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<string> eraList = new List<string>() { "Ancient Egypt", "Ancient Greece", "Medieval Europe", "Renaissance", "Industrial Revolution" };
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

            // Start playing music for the current era
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayEraMusic(CurrentEra);
            }
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
            OnPointsChanged?.Invoke();
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
        return CurrentPoints >= GetEraPrice(era); // Check if the player can afford the era
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
            "industrialrevolution" => "Industrial Revolution",
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

            case "ancientegypt":
                return 0; // First era, no points required
            case "medievaleurope":
                return 1000;
            case "renaissance":
                return 2000;
            case "industrialrevolution":
                return 3000;
            case "ancientgreece":
                return 4000;
            default:
                Debug.LogWarning($"Unknown era: {eraName}, returning 0 points");
                return 0;
    }
}


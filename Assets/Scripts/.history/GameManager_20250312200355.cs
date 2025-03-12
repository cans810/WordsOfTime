using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;

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

    [SerializeField] private List<string> eraList = new List<string>() { "Ancient Egypt", "Ancient Greece", "Medieval Europe", "Renaissance", "Industrial Revolution","Viking Age","Ottoman Empire","Feudal Japan"};
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
    public Dictionary<string, Dictionary<string, List<string>>> eraWordsPerLanguage { get; private set; } = 
        new Dictionary<string, Dictionary<string, List<string>>>();
    private Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> wordSentencesPerLanguage = 
        new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();

    // Store solved word indices per era
    private Dictionary<string, HashSet<int>> solvedWordsPerEra = new Dictionary<string, HashSet<int>>();

    // Store solved words per era (for cross-language support)
    private Dictionary<string, HashSet<string>> solvedBaseWordsPerEra = new Dictionary<string, HashSet<string>>();

    // Store hint usage separately (using English base words)
    private Dictionary<string, HashSet<int>> usedHints = new Dictionary<string, HashSet<int>>();

    public const int POINTS_PER_WORD = 250;
    public const int HINT_COST = 100;
    public const int SECOND_HINT_COST = 200;
    private const int GRID_SIZE = 6;

    public List<string> EraList => eraList;
    public List<Sprite> EraImages => eraImages;
    public int CurrentPoints
    {
        get { return currentPoints; }
        set
        {
            if (currentPoints != value)
            {
                currentPoints = value;
                OnPointsChanged?.Invoke(currentPoints);
                SaveManager.Instance.SaveGame();
            }
        }
    }
    public Dictionary<string, List<char>> InitialGrids => initialGrids;
    public Dictionary<string, List<Vector2Int>> SolvedWordPositions => solvedWordPositions;

    public delegate void PointsChangedHandler(int points);
    public event PointsChangedHandler OnPointsChanged;

    private Dictionary<string, List<string>> shuffledEraWords = new Dictionary<string, List<string>>();
    private bool hasShuffledWords = false;

    public Dictionary<string, int> eraPrices = new Dictionary<string, int>()
    {
        { "Ancient Egypt", 0 },        
        { "Medieval Europe", 0 },   
        { "Renaissance", 1000 },   
        { "Industrial Revolution", 2000 }, 
        { "Ancient Greece", 3000 },
        { "Viking Age", 4000 },
        { "Feudal Japan", 5000 },
        { "Ottoman Empire", 6000 }
    };

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
        "Industrial Revolution",
        "Ancient Greece",
        "Renaissance",
        "Viking Age",
        "Feudal Japan",
        "Ottoman Empire"
    };

    private HashSet<string> guessedWords = new HashSet<string>();

    private float musicSound;
    private bool notifications;

    private bool isMusicOn;
    private bool isSoundOn;
    private bool isVibrationOn = true; // Add vibration setting

    public HashSet<string> unlockedEras = new HashSet<string>() { "Ancient Egypt","Medieval Europe" }; // Start with Ancient Egypt unlocked

    // Add a dictionary to store word translations
    private Dictionary<string, Dictionary<string, string>> wordTranslations = new Dictionary<string, Dictionary<string, string>>();

    private Dictionary<string, HashSet<string>> solvedWordsPerLanguage = new Dictionary<string, HashSet<string>>();

    private const string AD_STATE_KEY = "AdState";
    public AdState adState = new AdState();

    // Add this field to track word guesses
    public int wordGuessCount = 0;
    private const int WORDS_BETWEEN_ADS = 3;
    private const float REWARDED_AD_COOLDOWN = 7200f; // 2 hours in seconds

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize with default settings
        currentSettings = new GameSettings();
        LoadSettings();  // Make sure we load settings in Awake
                
        // Initialize default unlocked eras
        unlockedEras = new HashSet<string>() { "Ancient Egypt", "Medieval Europe" };
        
        // Load saved language preference
        string savedLanguage = PlayerPrefs.GetString("Language", "en");
        SetLanguage(savedLanguage);
    }

    private void SelectRandomUnlockedEra()
    {
        List<string> unlockedErasList = new List<string>();
                
        foreach (string era in eraList)
        {
            bool isUnlocked = unlockedEras.Contains(era);
            if (isUnlocked)
            {
                unlockedErasList.Add(era);
            }
        }

        
        if (unlockedErasList.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, unlockedErasList.Count);
            string selectedEra = unlockedErasList[randomIndex];
            CurrentEra = selectedEra;
        }
        else
        {
            CurrentEra = "Ancient Egypt";
        }
    }

    private void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;

        
        // First load the words from JSON
        LoadWordsFromJson();

        // Always shuffle words on load
        ShuffleAllEraWords();

        // Wait for SaveManager to initialize and load save file
        if (SaveManager.Instance != null)
        {
            // Load the save file for game data
            LoadGame();
            
            // Generate grids after loading save
            GenerateAllGrids();
            
            // Select random era after loading save data
            SelectRandomUnlockedEra();
        }
        else
        {
            LoadWordsFromJson();
            ShuffleAllEraWords();
            GenerateAllGrids();
            SelectRandomUnlockedEra();
        }
                
        // Start playing music for current era
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(CurrentEra))
        {
            SoundManager.Instance.PlayEraMusic(CurrentEra);
        }

        // Debug print current state
        Debug.Log($"Current points: {CurrentPoints}");
        Debug.Log($"Current era: {CurrentEra}");
        Debug.Log($"Unlocked eras: {string.Join(", ", unlockedEras)}");
        Debug.Log($"Words loaded for current era: {(eraWordsPerLanguage.ContainsKey(currentLanguage) ? eraWordsPerLanguage[currentLanguage][CurrentEra].Count.ToString() : "0")}");

        StartCoroutine(UpdateAdCooldown());

        // Verify facts are loaded properly (especially for Android)
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("[Android Debug] Scheduling fact verification from GameManager");
            Invoke("VerifyFactsAfterLoadingDelayed", 5.0f);
        }
    }

    private IEnumerator UpdateAdCooldown()
    {
        while (true)
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long lastAdTime = SaveManager.Instance.Data.lastRewardedAdTimestamp;

            if (lastAdTime > 0)
            {
                long remainingTime = (long)REWARDED_AD_COOLDOWN - (currentTime - lastAdTime);

                // If cooldown has passed, reset the timestamp
                if (remainingTime <= 0)
                {
                    SaveManager.Instance.Data.lastRewardedAdTimestamp = 0;
                    SaveManager.Instance.SaveGame();
                }
            }

            yield return new WaitForSeconds(1); // Update every second
        }
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator LoadWordsFromJsonAndroid()
    {
        Debug.Log("[GameManager] Loading words from JSON on Android");
        
        // Load English words
        yield return StartCoroutine(LoadLanguageFileAndroid("en", "words.json"));
        
        // Load Turkish words
        yield return StartCoroutine(LoadLanguageFileAndroid("tr", "words_tr.json"));
        
        Debug.Log("[GameManager] Finished loading words from JSON on Android");
    }
    
    private IEnumerator LoadLanguageFileAndroid(string language, string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log($"[GameManager] Loading {language} words from: {filePath}");
        
        UnityWebRequest request = UnityWebRequest.Get(filePath);
        
        // This yield is now outside of any try-catch
        yield return request.SendWebRequest();
        
        try
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonContent = request.downloadHandler.text;
                Debug.Log($"[GameManager] Successfully loaded {language} words, JSON length: {jsonContent.Length}");
                LoadLanguageWords(jsonContent, language, false);
            }
            else
            {
                Debug.LogError($"[GameManager] Failed to load {language} words: {request.error}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] Error processing {language} words: {e.Message}");
        }
        finally
        {
            request.Dispose();
        }
    }
    #endif

    private void LoadWordsFromJson()
    {
        try
        {
            // Handle Android path differently
            #if UNITY_ANDROID && !UNITY_EDITOR
                Debug.Log("[GameManager] Starting coroutine to load words on Android");
                StartCoroutine(LoadWordsFromJsonAndroid());
            #else
                // Regular file loading for other platforms
                string enFilePath = Path.Combine(Application.streamingAssetsPath, "words.json");
                if (File.Exists(enFilePath))
                {
                    string enJsonContent = File.ReadAllText(enFilePath);
                    LoadLanguageWords(enJsonContent, "en", true);
                }

                string trFilePath = Path.Combine(Application.streamingAssetsPath, "words_tr.json");
                if (File.Exists(trFilePath))
                {
                    string trJsonContent = File.ReadAllText(trFilePath);
                    LoadLanguageWords(trJsonContent, "tr", true);
                }
            #endif

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

    private void LoadLanguageWords(string jsonContent, string language, bool isFromFile)
    {
        try 
        {
            var wordData = JsonUtility.FromJson<WordSetList>(jsonContent);
            
            if (!eraWordsPerLanguage.ContainsKey(language))
            {
                eraWordsPerLanguage[language] = new Dictionary<string, List<string>>();
                wordSentencesPerLanguage[language] = new Dictionary<string, Dictionary<string, List<string>>>();
            }

            foreach (var set in wordData.sets)
            {
                string era = set.era;
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

                    // Store translations for this word
                    string baseWord = wordEntry.translations.en;
                    if (!wordTranslations.ContainsKey(baseWord))
                    {
                        wordTranslations[baseWord] = new Dictionary<string, string>
                        {
                            { "en", wordEntry.translations.en },
                            { "tr", wordEntry.translations.tr }
                        };
                    }
                }

                Debug.Log($"Loaded {set.words.Length} words for {internalEra} in {language}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading {language} words: {e.Message}");
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
            {"Sanayi Devrimi", "Industrial Revolution"},
            {"Viking Çağı", "Viking Age"},
            {"Osmanlı İmparatorluğu", "Ottoman Empire"},
            {"Feodal Japonya", "Feudal Japan"}
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
        if (File.Exists(SaveManager.Instance.SavePath))
        {
            LoadGridsFromSave();
            return;
        }

        initialGrids.Clear();
        foreach (var era in eraWordsPerLanguage[currentLanguage].Keys)
        {
            List<string> words = new List<string>(eraWordsPerLanguage[currentLanguage][era]);
            
            foreach (var word in words)
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

        SaveManager.Instance.SaveGame();
    }

    private void ShuffleWords(List<string> words)
    {
        System.Random rng = new System.Random();
        int n = words.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string temp = words[k];
            words[k] = words[n];
            words[n] = temp;
        }
    }

    public void ShuffleAllEraWords()
    {
        Debug.Log("Performing initial word shuffle for all eras");
        System.Random rng = new System.Random();
        foreach (var language in eraWordsPerLanguage.Keys)
        {
            foreach (var era in eraWordsPerLanguage[language].Keys)
            {
                List<string> words = eraWordsPerLanguage[language][era];
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
                
                Debug.Log($"Shuffled words for {era} in {language}: {string.Join(", ", words)}");
            }
        }
        hasShuffledWords = true;
    }

    public List<string> GetCurrentEraWords()
    {
        if (eraWordsPerLanguage.ContainsKey(CurrentLanguage) && 
            eraWordsPerLanguage[CurrentLanguage].ContainsKey(CurrentEra))
        {
            return new List<string>(eraWordsPerLanguage[CurrentLanguage][CurrentEra]);
        }
        Debug.LogError($"No words found for language {CurrentLanguage} and era {CurrentEra}");
        return new List<string>();
    }

    public List<string> GetSentencesForWord(string word, string era)
    {
        if (wordSentencesPerLanguage.ContainsKey(currentLanguage) && 
            wordSentencesPerLanguage[currentLanguage].ContainsKey(era) && 
            wordSentencesPerLanguage[currentLanguage][era].ContainsKey(word))
        {
            var sentences = wordSentencesPerLanguage[currentLanguage][era][word];
            return sentences;
        }
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
        return $"_____ {(currentLanguage == "tr" ? "için cümle bulunamadı" : "sentence not found")}";
    }

    public Sprite getEraImage(string era)
    {
        int index = eraList.IndexOf(era);
        return index >= 0 && index < eraImages.Count ? eraImages[index] : null;
    }

    public void SelectEra(string eraName)
    {
        if (IsEraUnlocked(eraName))
        {
            SwitchEra(eraName);
        }
        else
        {
        }
    }

    public void AddPoints(int points)
    {
        CurrentPoints += points;
        OnPointsChanged?.Invoke(CurrentPoints);
        SaveManager.Instance.SaveGame();
    }

    public bool CanUseHint(int hintLevel, string word = null)
    {
        // If word is provided, check if it's already guessed
        if (word != null && (IsWordGuessed(word) || IsWordSolved(word)))
        {
            return false;
        }
        
        int cost = hintLevel == 1 ? HINT_COST : SECOND_HINT_COST;
        return currentPoints >= cost;
    }


    public void StoreSolvedWordPositions(string word, List<Vector2Int> positions)
    {
        if (string.IsNullOrEmpty(word) || positions == null || positions.Count == 0)
        {
            Debug.LogWarning($"[Android Debug] Cannot store solved word positions: Invalid parameters provided");
            return;
        }
        
        // Convert word to uppercase for consistency
        word = word.ToUpper();
        Debug.Log($"[Android Debug] Storing solved word positions for '{word}': {positions.Count} positions");
        
        // Store positions for this word
        solvedWordPositions[word] = new List<Vector2Int>(positions);
        
        // Try to get the base word
        string baseWord = GetBaseWord(word);
        
        // If a different base word was found, store positions for that too
        if (!string.IsNullOrEmpty(baseWord) && baseWord.ToUpper() != word)
        {
            Debug.Log($"[Android Debug] Also storing positions for base word '{baseWord.ToUpper()}'");
            solvedWordPositions[baseWord.ToUpper()] = new List<Vector2Int>(positions);
            
            // Store positions for translations in all supported languages
            foreach (string language in new string[] { "en", "tr" })
            {
                string translatedWord = GetTranslation(baseWord, language).ToUpper();
                if (!string.IsNullOrEmpty(translatedWord) && translatedWord != word && translatedWord != baseWord.ToUpper())
                {
                    Debug.Log($"[Android Debug] Also storing positions for {language} translation '{translatedWord}'");
                    solvedWordPositions[translatedWord] = new List<Vector2Int>(positions);
                }
            }
        }
        
        // Save the game to persist these changes
        SaveManager.Instance.SaveGame();
    }

    public bool IsWordSolved(string word)
    {
        // Get the base word for the current language
        string baseWord = GetBaseWord(word);
        
        // Check if the base word is solved in any language
        return solvedBaseWordsPerEra.ContainsKey(CurrentEra) && 
               solvedBaseWordsPerEra[CurrentEra].Contains(baseWord);
    }

    public List<Vector2Int> GetSolvedWordPositions(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogWarning("[Android Debug] Cannot get solved word positions: Word is null or empty");
            return null;
        }
        
        // Convert to uppercase for consistency
        string upperWord = word.ToUpper();
        Debug.Log($"[Android Debug] Getting solved positions for word: '{upperWord}'");
        
        // Try direct lookup first
        if (solvedWordPositions.ContainsKey(upperWord))
        {
            Debug.Log($"[Android Debug] Found positions for '{upperWord}' directly: {solvedWordPositions[upperWord].Count} positions");
            return solvedWordPositions[upperWord];
        }
        
        // Try without uppercasing
        if (solvedWordPositions.ContainsKey(word))
        {
            Debug.Log($"[Android Debug] Found positions for '{word}' (case sensitive): {solvedWordPositions[word].Count} positions");
            return solvedWordPositions[word];
        }
        
        // Try with the base word
        string baseWord = GetBaseWord(word);
        if (!string.IsNullOrEmpty(baseWord))
        {
            // Try uppercase base word
            string upperBaseWord = baseWord.ToUpper();
            if (solvedWordPositions.ContainsKey(upperBaseWord))
            {
                Debug.Log($"[Android Debug] Found positions using base word '{upperBaseWord}': {solvedWordPositions[upperBaseWord].Count} positions");
                return solvedWordPositions[upperBaseWord];
            }
            
            // Try with original case base word
            if (solvedWordPositions.ContainsKey(baseWord))
            {
                Debug.Log($"[Android Debug] Found positions using base word '{baseWord}' (case sensitive): {solvedWordPositions[baseWord].Count} positions");
                return solvedWordPositions[baseWord];
            }
            
            // Try with translations
            foreach (string language in new string[] { "en", "tr" })
            {
                string translatedWord = GetTranslation(baseWord, language);
                if (!string.IsNullOrEmpty(translatedWord))
                {
                    string upperTranslatedWord = translatedWord.ToUpper();
                    
                    // Try uppercase translation
                    if (solvedWordPositions.ContainsKey(upperTranslatedWord))
                    {
                        Debug.Log($"[Android Debug] Found positions using {language} translation '{upperTranslatedWord}': {solvedWordPositions[upperTranslatedWord].Count} positions");
                        return solvedWordPositions[upperTranslatedWord];
                    }
                    
                    // Try with original case translation
                    if (solvedWordPositions.ContainsKey(translatedWord))
                    {
                        Debug.Log($"[Android Debug] Found positions using {language} translation '{translatedWord}' (case sensitive): {solvedWordPositions[translatedWord].Count} positions");
                        return solvedWordPositions[translatedWord];
                    }
                }
            }
        }
        
        // On Android, try a fuzzy search through the keys as a last resort
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log($"[Android Debug] Performing fuzzy search for positions of '{upperWord}'");
            foreach (var entry in solvedWordPositions)
            {
                // Check if any key contains our word or our word contains any key
                if (entry.Key.Contains(upperWord) || upperWord.Contains(entry.Key))
                {
                    Debug.Log($"[Android Debug] Found potential fuzzy match with key '{entry.Key}': {entry.Value.Count} positions");
                    return entry.Value;
                }
                
                // Try a more liberal match - first few characters
                if (entry.Key.Length >= 3 && upperWord.Length >= 3 && 
                    entry.Key.Substring(0, 3) == upperWord.Substring(0, 3))
                {
                    Debug.Log($"[Android Debug] Found prefix match with key '{entry.Key}': {entry.Value.Count} positions");
                    return entry.Value;
                }
            }
        }
        
        Debug.LogWarning($"[Android Debug] No positions found for word '{word}' after trying all approaches");
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
                    grid[pos.x, pos.y].isSolved = true;
                    grid[pos.x, pos.y].GetComponent<Image>().raycastTarget = false;
                }
            }
        }
    }

    public void StoreSolvedWordIndex(string era, int wordIndex)
    {
        // Make sure we're storing using the proper key
        if (!solvedWordsPerEra.ContainsKey(era))
        {
            solvedWordsPerEra[era] = new HashSet<int>();
        }
        solvedWordsPerEra[era].Add(wordIndex);
        
        // Also store the base word for cross-language support
        // Only if the era is not already language-specific (doesn't contain _en or _tr)
        if (!era.Contains("_en") && !era.Contains("_tr")) 
        {
            // Get the word at this index in the English list
            if (eraWordsPerLanguage.ContainsKey("en") && eraWordsPerLanguage["en"].ContainsKey(era) &&
                wordIndex < eraWordsPerLanguage["en"][era].Count)
            {
                string baseWord = GetBaseWord(eraWordsPerLanguage["en"][era][wordIndex]);
                if (!solvedBaseWordsPerEra.ContainsKey(era))
                {
                    solvedBaseWordsPerEra[era] = new HashSet<string>();
                }
                solvedBaseWordsPerEra[era].Add(baseWord);
            }
        }
    }

    public HashSet<int> GetSolvedWordsForEra(string era)
    {
        string currentLanguageKey = era;
        
        // If we're using a specific language, check for language-specific solved words first
        if (CurrentLanguage == "tr" && !era.Contains("_tr"))
        {
            // Check for Turkish-specific solved indices
            string turkishKey = era + "_tr";
            if (solvedWordsPerEra.ContainsKey(turkishKey))
            {
                return new HashSet<int>(solvedWordsPerEra[turkishKey]);
            }
        }
        else if (CurrentLanguage == "en" && !era.Contains("_en"))
        {
            // Check for English-specific solved indices
            string englishKey = era + "_en";
            if (solvedWordsPerEra.ContainsKey(englishKey))
            {
                return new HashSet<int>(solvedWordsPerEra[englishKey]);
            }
        }
        
        // Fallback to the original era key if no language-specific data exists
        if (solvedWordsPerEra.ContainsKey(era))
        {
            return new HashSet<int>(solvedWordsPerEra[era]);
        }
        
        return new HashSet<int>();
    }

    public void SwitchEra(string newEra)
    {
        CurrentEra = newEra;
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEraMusic(newEra);
        }
        
        if (!solvedWordsPerEra.ContainsKey(newEra))
        {
            solvedWordsPerEra[newEra] = new HashSet<int>();
        }
        
        if (!solvedBaseWordsPerEra.ContainsKey(newEra))
        {
            solvedBaseWordsPerEra[newEra] = new HashSet<string>();
        }
        
        // We're no longer highlighting solved words on the grid
        // Update the display of solved words when switching eras
        /*
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UpdateSolvedWordsDisplay();
        }
        */
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
        // Always return true for default eras
        if (era == "Ancient Egypt" || era == "Medieval Europe")
        {
            return true;
        }
        return unlockedEras.Contains(era);
    }

    public void UnlockEra(string era)
    {
        if (!unlockedEras.Contains(era))
        {
            unlockedEras.Add(era);
            SaveManager.Instance.SaveGame(); // Save the unlocked status
        }
    }

    public bool CanUnlockEra(string era)
    {
        return !IsEraUnlocked(era) && CurrentPoints >= GetEraPrice(era);
    }

    // Store hint usage for a specific word and level
    public void StoreHintUsage(string word, int hintLevel)
    {
        string baseWord = GetBaseWord(word);
        string key = $"{CurrentEra}_{baseWord}";
        if (!usedHints.ContainsKey(key))
        {
            usedHints[key] = new HashSet<int>();
        }
        usedHints[key].Add(hintLevel);
        SaveManager.Instance.SaveGame();
    }

    // Get current hint level for a word
    public int GetHintLevel(string word)
    {
        string baseWord = GetBaseWord(word);
        string key = $"{CurrentEra}_{baseWord}";
        if (!usedHints.ContainsKey(key)) return 0;
        return usedHints[key].Count;
    }

    // Check if a specific hint level has been used for a word
    public bool HasUsedHint(string word, int hintLevel)
    {
        string baseWord = GetBaseWord(word);
        string key = $"{CurrentEra}_{baseWord}";
        return usedHints.ContainsKey(key) && usedHints[key].Contains(hintLevel);
    }

    // Check if a specific hint level is available for a word
    public bool IsHintAvailable(string word, int hintLevel)
    {
        // First check if the word has already been guessed
        if (IsWordGuessed(word) || IsWordSolved(word))
        {
            return false;
        }
        
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
    public void SetLanguage(string newLanguage)
    {
        if (currentLanguage != newLanguage)
        {
            
            // Update the current language
            currentLanguage = newLanguage;
            PlayerPrefs.SetString("Language", newLanguage);
            PlayerPrefs.Save();
        
            
            // Invoke the language changed event
            OnLanguageChanged?.Invoke();
        }
    }

    public int GetRequiredPointsForEra(string eraName)
    {
        string formattedEraName = eraName switch
        {
            "ancientegypt" => "Ancient Egypt",
            "ancientgreece" => "Ancient Greece",
            "medievaleurope" => "Medieval Europe",
            "renaissance" => "Renaissance",
            "industrialrevolution" => "Industrial Revolution",
            "vikingage" => "Viking Age",
            "ottomanempire" => "Ottoman Empire",
            "feudaljapan" => "Feudal Japan",
            _ => eraName
        };

        // Get price from eraPrices dictionary
        if (eraPrices.ContainsKey(formattedEraName))
        {
            Debug.Log($"Price for {formattedEraName}: {eraPrices[formattedEraName]}");
            return eraPrices[formattedEraName];
        }

        Debug.LogWarning($"Unknown era: {eraName} (formatted: {formattedEraName}), returning 0 points");
        return 0;
    }

    public void SetPoints(int points)
    {
        currentPoints = points;
        OnPointsChanged?.Invoke(currentPoints);
    }

    public GameSettings GetSettings()
    {
        // Return current settings state
        currentSettings.musicEnabled = isMusicOn;
        currentSettings.soundEnabled = isSoundOn;
        currentSettings.notificationsEnabled = notifications;
        currentSettings.vibrationEnabled = isVibrationOn; // Add vibration setting
        if (SoundManager.Instance != null)
        {
            currentSettings.musicVolume = SoundManager.Instance.MusicVolume;
            currentSettings.soundVolume = SoundManager.Instance.SoundVolume;
        }
        return currentSettings;
    }

    public void LoadSettings(GameSettings settings)
    {
        if (settings != null)
        {
            currentSettings = settings;
            isMusicOn = settings.musicEnabled;
            isSoundOn = settings.soundEnabled;
            notifications = settings.notificationsEnabled;
            isVibrationOn = settings.vibrationEnabled; // Add vibration setting
            
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.IsMusicOn = settings.musicEnabled;
                SoundManager.Instance.IsSoundOn = settings.soundEnabled;
                SoundManager.Instance.SetMusicVolume(settings.musicVolume);
                SoundManager.Instance.SetSoundVolume(settings.soundVolume);
            }
            
            // Update VibrationManager with current setting
            VibrationManager.SetVibrationEnabled(isVibrationOn);
            
            Debug.Log($"Loaded settings: Music={settings.musicEnabled}, Sound={settings.soundEnabled}, Notifications={settings.notificationsEnabled}, Vibration={settings.vibrationEnabled}");
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
        List<string> allGuessedWords = new List<string>();
        foreach (var era in solvedBaseWordsPerEra)
        {
            allGuessedWords.AddRange(era.Value);
        }
        return allGuessedWords;
    }

    public void SetGuessedWords(List<string> words)
    {
        Debug.Log($"Setting guessed words: {string.Join(", ", words)}");
        solvedBaseWordsPerEra.Clear();
        
        // Make sure all eras are initialized in the dictionary
        foreach (string era in eraList)
        {
            if (!solvedBaseWordsPerEra.ContainsKey(era))
            {
                solvedBaseWordsPerEra[era] = new HashSet<string>();
            }
        }
        
        foreach (string word in words)
        {
            // Try to find the era for this word
            string era = GetEraForWord(word);
            Debug.Log($"Found era '{era}' for word '{word}'");
            
            // Add the word to the appropriate era
            solvedBaseWordsPerEra[era].Add(word);
            
            // Also update the index-based tracking if possible
            if (eraWordsPerLanguage.ContainsKey("en") && eraWordsPerLanguage["en"].ContainsKey(era))
            {
                int wordIndex = eraWordsPerLanguage["en"][era].IndexOf(word);
                if (wordIndex >= 0)
                {
                    StoreSolvedWordIndex(era, wordIndex);
                    Debug.Log($"Stored word index {wordIndex} for word '{word}' in era '{era}'");
                }
                else
                {
                    Debug.LogWarning($"Could not find index for word '{word}' in era '{era}'");
                }
            }
        }
        
        // Log the results for debugging
        foreach (var era in solvedBaseWordsPerEra.Keys)
        {
            Debug.Log($"Era '{era}' has {solvedBaseWordsPerEra[era].Count} solved words: {string.Join(", ", solvedBaseWordsPerEra[era])}");
        }
    }

    private WordSetList LoadWordSetList()
    {
        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "words.json");
        
        if (!File.Exists(streamingAssetsPath))
        {
            Debug.LogError($"Could not find words.json in StreamingAssets folder: {streamingAssetsPath}");
            return null;
        }

        try
        {
            string jsonContent = File.ReadAllText(streamingAssetsPath);
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(jsonContent);
            
            if (wordSetList == null || wordSetList.sets == null)
            {
                Debug.LogError("Failed to parse words.json or no word sets found");
                return null;
            }

            Debug.Log($"Successfully loaded {wordSetList.sets.Length} word sets from StreamingAssets");
            return wordSetList;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading words.json from StreamingAssets: {e.Message}");
            return null;
        }
    }

    public string GetBaseWord(string word)
    {
        
        WordSetList wordSetList = LoadWordSetList();
        if (wordSetList == null)
        {
            return word;
        }
        
        // First check if it's already an English word
        foreach (WordSet wordSet in wordSetList.sets)
        {
            foreach (WordEntry wordEntry in wordSet.words)
            {
                if (string.Equals(wordEntry.translations.en, word, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"Word {word} is already in English");
                    return word;
                }
            }
        }
        
        // If not English, search for Turkish translation
        foreach (WordSet wordSet in wordSetList.sets)
        {
            foreach (WordEntry wordEntry in wordSet.words)
            {
                if (string.Equals(wordEntry.translations.tr, word, StringComparison.OrdinalIgnoreCase))
                {
                    string baseWord = wordEntry.translations.en;
                    Debug.Log($"Found exact match base word in {wordSet.era}: {baseWord} for {word}");
                    Debug.Log($"Matched: '{wordEntry.translations.tr}' with '{word}'");
                    Debug.Log($"English translation: '{wordEntry.translations.en}'");
                    return baseWord;
                }
            }
        }
        
        Debug.LogWarning($"No base word found for {word}, returning original");
        return word;
    }

    // Helper method to get era for a word
    private string GetEraForWord(string word)
    {
        // First try to find the word in the English word lists for each era
        if (eraWordsPerLanguage.ContainsKey("en"))
        {
            foreach (var era in eraList)
            {
                if (eraWordsPerLanguage["en"].ContainsKey(era) && 
                    eraWordsPerLanguage["en"][era].Contains(word))
                {
                    Debug.Log($"Found word '{word}' in era '{era}' (English word list)");
                    return era;
                }
            }
        }
        
        // If not found in English, try Turkish
        if (eraWordsPerLanguage.ContainsKey("tr"))
        {
            foreach (var era in eraList)
            {
                string mappedEra = MapTurkishEraName(era);
                if (eraWordsPerLanguage["tr"].ContainsKey(mappedEra) && 
                    eraWordsPerLanguage["tr"][mappedEra].Contains(word))
                {
                    Debug.Log($"Found word '{word}' in era '{era}' (Turkish word list)");
                    return era;
                }
            }
        }
        
        // As a fallback, try WordValidator
        foreach (var era in eraList)
        {
            if (WordValidator.GetWordsForEra(era).Contains(word))
            {
                Debug.Log($"Found word '{word}' in era '{era}' (WordValidator)");
                return era;
            }
        }
        
        Debug.LogWarning($"Could not find era for word '{word}', defaulting to '{eraList[0]}'");
        return eraList[0]; // Fallback to first era if not found
    }

    public void AddGuessedWord(string word)
    {
        Debug.Log($"Adding guessed word: {word}");
        guessedWords.Add(word);
        Debug.Log($"Current guessed words: {string.Join(", ", guessedWords)}");
    }

    public bool IsWordGuessed(string word)
    {
        string baseWord = GetBaseWord(word);
        foreach (var era in solvedBaseWordsPerEra.Keys)
        {
            if (solvedBaseWordsPerEra[era].Contains(baseWord))
            {
                return true;
            }
        }
        return false;
    }

    public List<Vector2Int> GetWordPath(string word)
    {
        if (solvedWordPositions.ContainsKey(word))
        {
            return solvedWordPositions[word];
        }
        return null;
    }

    public void OnWordGuessed()
    {
        // This method is now empty as WordGameManager handles the ad logic
        // We're keeping the method to avoid breaking any existing references
    }

    public float GetMusicSound()
    {
        return musicSound;
    }

    public bool IsNotificationsOn()
    {
        return notifications;
    }

    public void SetNotifications(bool value)
    {
        notifications = value;
    }

    public bool IsMusicOn()
    {
        Debug.Log($"Getting Music On: {isMusicOn}");
        return isMusicOn;
    }

    public void SetMusicOn(bool value)
    {
        isMusicOn = value;
        currentSettings.musicEnabled = value;  // Update settings object
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.IsMusicOn = value;
        }
    }

    public bool IsSoundOn()
    {
        Debug.Log($"Getting Sound On: {isSoundOn}");
        return isSoundOn;
    }

    public void SetSoundOn(bool value)
    {
        isSoundOn = value;
        currentSettings.soundEnabled = value;  // Update settings object
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.IsSoundOn = value;
        }
    }

    private void LoadGridsFromSave()
    {
        if (SaveManager.Instance.Data != null && SaveManager.Instance.Data.preGeneratedGrids != null)
        {
            LoadGridData(SaveManager.Instance.Data.preGeneratedGrids);
            Debug.Log("Loaded grids from save file.");
        }
        else
        {
            Debug.LogWarning("No grid data found in save file.");
        }
    }

    public List<HintData> GetUsedHintsData()
    {
        List<HintData> hintsData = new List<HintData>();
        foreach (var kvp in usedHints)
        {
            hintsData.Add(new HintData(kvp.Key, new List<int>(kvp.Value)));
        }
        return hintsData;
    }

    public void LoadUsedHintsData(List<HintData> hintsData)
    {
        usedHints.Clear();
        if (hintsData != null)
        {
            foreach (var data in hintsData)
            {
                usedHints[data.wordKey] = new HashSet<int>(data.hintLevels);
            }
        }
    }

    // Add these methods for SaveManager to use
    public HashSet<string> GetUnlockedEras()
    {
        // Ensure default eras are always included
        HashSet<string> eras = new HashSet<string>(unlockedEras);
        eras.Add("Ancient Egypt");
        eras.Add("Medieval Europe");
        return eras;
    }

    public void SetUnlockedEras(HashSet<string> eras)
    {
        // Ensure default eras are always unlocked
        unlockedEras = new HashSet<string>(eras);
        unlockedEras.Add("Ancient Egypt");
        unlockedEras.Add("Medieval Europe");
        Debug.Log($"SetUnlockedEras: Current unlocked eras: {string.Join(", ", unlockedEras)}");
    }

    public HashSet<string> GetSolvedBaseWordsForEra(string era)
    {
        if (solvedBaseWordsPerEra.ContainsKey(era))
        {
            return solvedBaseWordsPerEra[era];
        }
        return new HashSet<string>();
    }

    public string GetCurrentLanguageWord(string baseWord)
    {
        if (eraWordsPerLanguage.ContainsKey(currentLanguage) && 
            eraWordsPerLanguage[currentLanguage].ContainsKey(CurrentEra))
        {
            var wordList = eraWordsPerLanguage[currentLanguage][CurrentEra];
            var englishWordList = eraWordsPerLanguage["en"][CurrentEra];
            
            int wordIndex = englishWordList.IndexOf(baseWord);
            if (wordIndex >= 0 && wordIndex < wordList.Count)
            {
                return wordList[wordIndex];
            }
        }
        return baseWord;
    }

    public void StoreSolvedBaseWord(string era, string baseWord)
    {
        StoreSolvedBaseWord(era, baseWord, true);
    }

    public void StoreSolvedBaseWord(string era, string baseWord, bool shouldSave)
    {
        Debug.Log($"Storing solved base word: {baseWord} for era: {era}");
        
        if (!solvedBaseWordsPerEra.ContainsKey(era))
        {
            solvedBaseWordsPerEra[era] = new HashSet<string>();
        }
        
        solvedBaseWordsPerEra[era].Add(baseWord);
        
        // Also store translations for all supported languages
        foreach (string language in new[] { "en", "tr" })
        {
            string translatedWord = GetTranslation(baseWord, language);
            Debug.Log($"Storing translation for {language}: {translatedWord}");
            
            if (!solvedWordsPerLanguage.ContainsKey(language))
            {
                solvedWordsPerLanguage[language] = new HashSet<string>();
            }
            solvedWordsPerLanguage[language].Add(translatedWord);
        }
        
        // Only save if requested, to avoid recursive saving when loading
        if (shouldSave && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }

    private void LoadSettings()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            Debug.Log("=== Loading Settings ===");
            
            // Load game settings
            if (SaveManager.Instance.Data.settings != null)
            {
                currentSettings.soundEnabled = SaveManager.Instance.Data.settings.soundEnabled;
                currentSettings.musicEnabled = SaveManager.Instance.Data.settings.musicEnabled;
                currentSettings.notificationsEnabled = SaveManager.Instance.Data.settings.notificationsEnabled;
                currentSettings.soundVolume = SaveManager.Instance.Data.settings.soundVolume;
                currentSettings.musicVolume = SaveManager.Instance.Data.settings.musicVolume;
                
                Debug.Log($"Settings loaded: " +
                         $"Sound: {currentSettings.soundEnabled}, " +
                         $"Music: {currentSettings.musicEnabled}, " +
                         $"Notifications: {currentSettings.notificationsEnabled}, " +
                         $"Sound Volume: {currentSettings.soundVolume}, " +
                         $"Music Volume: {currentSettings.musicVolume}");
            }
            else
            {
                Debug.LogWarning("No settings found in save data, using defaults");
            }
            
            // Load guessed words
            if (SaveManager.Instance.Data.guessedWords != null)
            {
                guessedWords = new HashSet<string>(SaveManager.Instance.Data.guessedWords);
                Debug.Log($"Loaded {guessedWords.Count} guessed words");
            }
            else
            {
                Debug.LogWarning("No guessed words found in save data");
            }
            
            // Load unlocked eras
            if (SaveManager.Instance.Data.unlockedEras != null)
            {
                unlockedEras = new HashSet<string>(SaveManager.Instance.Data.unlockedEras);
                Debug.Log($"Loaded {unlockedEras.Count} unlocked eras: {string.Join(", ", unlockedEras)}");
            }
            else
            {
                Debug.LogWarning("No unlocked eras found in save data, using defaults");
                unlockedEras = new HashSet<string>() { "Ancient Egypt", "Medieval Europe" };
            }
            
            // Load points
            currentPoints = SaveManager.Instance.Data.points;
            Debug.Log($"Loaded points: {currentPoints}");
        }
        else
        {
            Debug.LogWarning("SaveManager or save data not available, using default settings");
            currentSettings = new GameSettings();
            guessedWords = new HashSet<string>();
            unlockedEras = new HashSet<string>() { "Ancient Egypt", "Medieval Europe" };
            currentPoints = 0;
        }
    }

    private void OnApplicationQuit()
    {
        SaveGameState();
    }

    public void SaveGameState()
    {
        // Save the current game state
        SaveManager.Instance.SaveGame();
        
        // Save ad state
        SaveAdState();
        
        Debug.Log("Game state saved on application quit");
    }

    public void SaveAdState()
    {
        string json = JsonUtility.ToJson(adState);
        PlayerPrefs.SetString(AD_STATE_KEY, json);
        PlayerPrefs.Save();
    }

    public AdState LoadAdState(AdState? newState = null)
    {
        if (newState != null)
        {
            adState = newState.Value;
            SaveAdState();
            return adState;
        }

        if (PlayerPrefs.HasKey(AD_STATE_KEY))
        {
            string json = PlayerPrefs.GetString(AD_STATE_KEY);
            adState = JsonUtility.FromJson<AdState>(json);
            
            // Use the saved timestamp from SaveManager instead of resetting
            long lastAdTime = SaveManager.Instance.Data.lastRewardedAdTimestamp;
            if (lastAdTime > 0)
            {
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long remainingTime = (long)REWARDED_AD_COOLDOWN - (currentTime - lastAdTime);
                
                adState.canWatch = remainingTime <= 0;
                if (!adState.canWatch)
                {
                    adState.nextAvailableTime = DateTime.Now.AddSeconds(remainingTime);
                }
            }
        }
        else
        {
            // Initialize with values from SaveManager if available
            long lastAdTime = SaveManager.Instance.Data.lastRewardedAdTimestamp;
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long remainingTime = (long)REWARDED_AD_COOLDOWN - (currentTime - lastAdTime);
            
            adState = new AdState
            {
                canWatch = remainingTime <= 0,
                nextAvailableTime = DateTime.Now.AddSeconds(Math.Max(0, remainingTime))
            };
        }
        
        return adState;
    }

    public string GetTranslation(string baseWord, string language)
    {
        if (wordTranslations.ContainsKey(baseWord) && 
            wordTranslations[baseWord].ContainsKey(language))
        {
            return wordTranslations[baseWord][language];
        }
        return baseWord;
    }

    public HashSet<string> GetAllSolvedBaseWords()
    {
        HashSet<string> allSolvedBaseWords = new HashSet<string>();
        
        // Combine all solved base words from all eras
        foreach (var era in EraList)
        {
            HashSet<string> eraSolvedWords = GetSolvedBaseWordsForEra(era);
            if (eraSolvedWords != null && eraSolvedWords.Count > 0)
            {
                allSolvedBaseWords.UnionWith(eraSolvedWords);
            }
        }
        
        return allSolvedBaseWords;
    }
    
    public string GetWordDifficulty(string word)
    {
        // Default difficulty is "normal"
        string difficulty = "normal";
        
        // Get the base word
        string baseWord = GetBaseWord(word);
        
        // Get the current era's words
        var eraWords = eraWordsPerLanguage[CurrentLanguage][CurrentEra];
        
        // Find the index of the word in the current era
        int wordIndex = -1;
        for (int i = 0; i < eraWords.Count; i++)
        {
            if (GetBaseWord(eraWords[i]) == baseWord)
            {
                wordIndex = i;
                break;
            }
        }
        
        // Determine difficulty based on word index
        if (wordIndex >= 0)
        {
            int totalWords = eraWords.Count;
            float percentile = (float)wordIndex / totalWords;
            
            if (percentile < 0.33f)
            {
                difficulty = "easy";
            }
            else if (percentile < 0.66f)
            {
                difficulty = "normal";
            }
            else
            {
                difficulty = "hard";
            }
        }
        
        return difficulty;
    }
    
    public void TriggerWordGuessed(string word)
    {
        // Add the word to guessed words
        AddGuessedWord(word);
        
        // Save the game state
        SaveManager.Instance.SaveGame();
    }

    // Add this method to verify facts after loading
    public void VerifyFactsAfterLoadingDelayed()
    {
        Debug.Log("[GameManager] Verifying facts after loading delay");
        WordValidator.VerifyFactsAfterLoading();
    }

    public void LoadGame()
    {
        Debug.Log("GameManager: Loading game");
        
        // Initialize state before loading
        solvedBaseWordsPerEra.Clear();
        solvedWordsPerLanguage.Clear();
        solvedWordPositions.Clear();
        
        // Load game data from SaveManager
        if (SaveManager.Instance != null)
        {
            // Load SaveData without updating GameManager to avoid circular references
            SaveManager.Instance.LoadGame(false);
            
            // Now access the SaveData directly
            currentPoints = SaveManager.Instance.Data.points;
            LoadSettings();
            wordGuessCount = SaveManager.Instance.Data.wordGuessCount;
            
            // Load solved base words per era directly from SaveData
            if (SaveManager.Instance.Data.solvedBaseWordsPerEra != null && 
                SaveManager.Instance.Data.solvedBaseWordsPerEra.Count > 0)
            {
                Debug.Log($"Loading solved base words for {SaveManager.Instance.Data.solvedBaseWordsPerEra.Count} eras directly in GameManager");
                foreach (var entry in SaveManager.Instance.Data.solvedBaseWordsPerEra)
                {
                    string era = entry.Key;
                    Debug.Log($"Era {era} has {entry.Value.Count} solved words: {string.Join(", ", entry.Value)}");
                    
                    // Initialize the HashSet if needed
                    if (!solvedBaseWordsPerEra.ContainsKey(era))
                    {
                        solvedBaseWordsPerEra[era] = new HashSet<string>();
                    }
                    
                    // Add each word directly to the HashSet without calling StoreSolvedBaseWord
                    foreach (string baseWord in entry.Value)
                    {
                        solvedBaseWordsPerEra[era].Add(baseWord);
                        Debug.Log($"Directly added solved base word '{baseWord}' for era '{era}'");
                        
                        // Also store translations for all supported languages
                        foreach (string language in new[] { "en", "tr" })
                        {
                            string translatedWord = GetTranslation(baseWord, language);
                            Debug.Log($"Storing translation for {language}: {translatedWord}");
                            
                            if (!solvedWordsPerLanguage.ContainsKey(language))
                            {
                                solvedWordsPerLanguage[language] = new HashSet<string>();
                            }
                            solvedWordsPerLanguage[language].Add(translatedWord);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("No solved base words found in save data");
            }
        }
    }

    public bool IsVibrationOn()
    {
        return isVibrationOn;
    }

    public void SetVibrationOn(bool value)
    {
        isVibrationOn = value;
        currentSettings.vibrationEnabled = value;  // Update settings object
        VibrationManager.SetVibrationEnabled(value);
    }
}
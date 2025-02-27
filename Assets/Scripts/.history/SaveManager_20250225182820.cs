using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // Add this line for ToList()
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine.Networking;

[System.Serializable]
public class UsedHintData
{
    public string wordKey;
    public List<int> hintLevels;

    public UsedHintData(string key, List<int> levels)
    {
        wordKey = key;
        hintLevels = levels;
    }
}

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    _instance = go.AddComponent<SaveManager>();
                }
            }
            return _instance;
        }
    }

    public SaveData Data { get; private set; }
    public string SavePath => Path.Combine(Application.persistentDataPath, "gamesave.json");
    private string TXT_SAVE_PATH => Path.Combine(Application.persistentDataPath, "gamesave.txt");
    private string BACKUP_SAVE_PATH => Path.Combine(Application.persistentDataPath, "gamesave_backup.json");

    // Cooldown period for daily spin (24 hours)
    private const int DAILY_SPIN_COOLDOWN_HOURS = 24;

    // Cooldown period for rewarded ads (2 hours)
    private const int REWARDED_AD_COOLDOWN_HOURS = 2;

    // Flag to track if we've already loaded the game
    private bool hasLoadedGame = false;
    
    // Flag to track if we're currently saving
    private bool isSaving = false;
    
    // Flag to track if we're currently loading
    private bool isLoading = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize with empty data
        Data = new SaveData();
        
        // Log device information for debugging
        LogDeviceInfo();
    }
    
    private void LogDeviceInfo()
    {
        Debug.Log($"Device Model: {SystemInfo.deviceModel}");
        Debug.Log($"Device Name: {SystemInfo.deviceName}");
        Debug.Log($"Device Type: {SystemInfo.deviceType}");
        Debug.Log($"Operating System: {SystemInfo.operatingSystem}");
        Debug.Log($"System Memory Size: {SystemInfo.systemMemorySize} MB");
        Debug.Log($"Persistent Data Path: {Application.persistentDataPath}");
        Debug.Log($"Is Android: {Application.platform == RuntimePlatform.Android}");
    }

    private void Start()
    {
        // Load game data on start
        StartCoroutine(LoadGameCoroutine());
        
        // Log the save path for debugging
        Debug.Log($"Save file location: {SavePath}");
        Debug.Log($"Application.persistentDataPath: {Application.persistentDataPath}");
    }

    public void SaveGame()
    {
        if (isSaving)
        {
            Debug.Log("Save operation already in progress, skipping");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("Cannot save game: GameManager.Instance is null");
            return;
        }

        StartCoroutine(SaveGameCoroutine());
    }
    
    private IEnumerator SaveGameCoroutine()
    {
        isSaving = true;
        Debug.Log($"Starting save operation to: {SavePath}");
        
        string jsonData = "";
        bool dataPreparationSuccessful = false;
        
        try
        {
            Data = new SaveData();
            
            // Save points and guessed words
            Data.points = GameManager.Instance.CurrentPoints;
            Data.guessedWords = GameManager.Instance.GetGuessedWords();
            
            // Save hint usage data
            Data.usedHintsData = GameManager.Instance.GetUsedHintsData();
            Debug.Log($"Saving {Data.usedHintsData.Count} hint records");
            
            // Save grid data with era information
            List<GridData> gridDataList;
            GameManager.Instance.SaveGridData(out gridDataList);
            foreach (var gridData in gridDataList)
            {
                gridData.era = GameManager.Instance.CurrentEra; // Add era information
            }
            Data.preGeneratedGrids = gridDataList;

            // Save solved words per era
            Data.solvedWords = GameManager.Instance.GetAllSolvedBaseWords().ToList();

            // Save game settings
            Data.settings = GameManager.Instance.GetSettings();

            // Save shuffled words
            Dictionary<string, List<string>> shuffledWordsDict = new Dictionary<string, List<string>>();
            foreach (var language in GameManager.Instance.eraWordsPerLanguage.Keys)
            {
                foreach (var era in GameManager.Instance.eraWordsPerLanguage[language].Keys)
                {
                    string key = $"{language}_{era}";
                    shuffledWordsDict[key] = new List<string>(GameManager.Instance.eraWordsPerLanguage[language][era]);
                }
            }
            Data.shuffledWords = shuffledWordsDict;

            // Save unlocked eras
            Data.unlockedEras = GameManager.Instance.GetUnlockedEras().ToList();

            // Save ad state by creating a new AdState
            var currentAdState = GameManager.Instance.LoadAdState();
            Data.adState = new AdState 
            {
                canWatch = currentAdState.canWatch,
                nextAvailableTime = currentAdState.nextAvailableTime
            };
            
            // Convert DateTime to long for serialization
            Data.adState.nextAvailableTimeTicks = Data.adState.nextAvailableTime.Ticks;
            
            Data.wordGuessCount = GameManager.Instance.wordGuessCount;

            // Save no ads state
            Data.noAdsBought = GameManager.Instance.NoAdsBought;

            // Record the time when the game was saved
            Data.lastClosedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // Add version info
            Data.gameVersion = Application.version;

            // Save as JSON instead of binary
            jsonData = JsonUtility.ToJson(Data, true);
            dataPreparationSuccessful = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            
            // Try PlayerPrefs as a fallback for critical data
            try {
                PlayerPrefs.SetInt("Points", Data.points);
                PlayerPrefs.SetInt("NoAdsBought", Data.noAdsBought ? 1 : 0);
                PlayerPrefs.Save();
                Debug.Log("Saved critical data to PlayerPrefs as fallback");
            } catch (Exception prefsEx) {
                Debug.LogError($"Failed to save to PlayerPrefs: {prefsEx.Message}");
            }
        }
        
        if (dataPreparationSuccessful)
        {
            // First save to a backup file
            yield return WriteTextToFileCoroutine(BACKUP_SAVE_PATH, jsonData);
            
            // Then save to the main file
            yield return WriteTextToFileCoroutine(SavePath, jsonData);
            
            // Save text file
            yield return SaveGameAsTextCoroutine();
            
            Debug.Log($"Game saved successfully to {SavePath}!");
            
            // Verify the file was created
            try
            {
                if (File.Exists(SavePath))
                {
                    Debug.Log($"Verified: Save file exists at {SavePath}");
                    Debug.Log($"File size: {new FileInfo(SavePath).Length} bytes");
                    
                    // Double-check by reading the file back
                    string verificationData = File.ReadAllText(SavePath);
                    Debug.Log($"Verification read successful, content length: {verificationData.Length}");
                }
                else
                {
                    Debug.LogError($"Failed to verify save file at {SavePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error verifying save file: {e.Message}");
            }
        }
        
        isSaving = false;
    }

    // Helper method to write text to file with proper error handling
    private IEnumerator WriteTextToFileCoroutine(string filePath, string content)
    {
        Debug.Log($"Writing to file: {filePath}, content length: {content.Length}");
        
        // Ensure directory exists
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"Created directory: {directory}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating directory {directory}: {e.Message}");
            }
            // Wait a frame to ensure directory creation completes
            yield return null;
        }

        // Special handling for Android
        if (Application.platform == RuntimePlatform.Android)
        {
            // On Android, use a temporary file and then move it
            string tempPath = filePath + ".tmp";
            
            bool writeSuccess = false;
            try
            {
                // Write to temp file
                using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.Write(content);
                    }
                }
                writeSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing to temp file {tempPath}: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                
                // Try PlayerPrefs as a fallback
                if (Path.GetFileName(filePath) == "gamesave.json")
                {
                    try {
                        PlayerPrefs.SetString("SaveGameBackup", content);
                        PlayerPrefs.Save();
                        Debug.Log("Saved content to PlayerPrefs as fallback");
                    } catch (Exception prefsEx) {
                        Debug.LogError($"Failed to save to PlayerPrefs: {prefsEx.Message}");
                    }
                }
            }
            
            // Wait a frame to ensure file is closed properly
            yield return null;
            
            if (writeSuccess)
            {
                try
                {
                    // If the main file exists, delete it first
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    
                    // Move temp file to final location
                    File.Move(tempPath, filePath);
                    
                    Debug.Log($"Successfully wrote to file on Android: {filePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error moving temp file to final location: {e.Message}");
                }
                
                yield return null;
            }
        }
        else
        {
            // For other platforms, write directly
            try
            {
                File.WriteAllText(filePath, content);
                Debug.Log($"Successfully wrote to file: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing to file {filePath}: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                
                // Try PlayerPrefs as a fallback
                if (Path.GetFileName(filePath) == "gamesave.json")
                {
                    try {
                        PlayerPrefs.SetString("SaveGameBackup", content);
                        PlayerPrefs.Save();
                        Debug.Log("Saved content to PlayerPrefs as fallback");
                    } catch (Exception prefsEx) {
                        Debug.LogError($"Failed to save to PlayerPrefs: {prefsEx.Message}");
                    }
                }
            }
        }
        
        // Wait a frame to ensure file operations complete
        yield return null;
        
        // Verify file was written
        try
        {
            if (File.Exists(filePath))
            {
                long fileSize = new FileInfo(filePath).Length;
                Debug.Log($"Verified file exists: {filePath}, size: {fileSize} bytes");
            }
            else
            {
                Debug.LogError($"Failed to verify file exists after writing: {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error verifying file: {e.Message}");
        }
    }

    private IEnumerator SaveGameAsTextCoroutine()
    {
        Debug.Log($"Saving text file to: {TXT_SAVE_PATH}");
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== Game Save - {DateTime.Now} ===");
        sb.AppendLine($"Points: {Data.points}");
        sb.AppendLine($"No Ads Bought: {Data.noAdsBought}");
        sb.AppendLine($"Word Guess Count: {Data.wordGuessCount}");
        
        sb.AppendLine("\n=== Unlocked Eras ===");
        foreach (var era in Data.unlockedEras)
        {
            sb.AppendLine($"- {era}");
        }
        
        sb.AppendLine("\n=== Solved Words ===");
        foreach (var word in Data.solvedWords)
        {
            sb.AppendLine($"- {word}");
        }
        
        sb.AppendLine("\n=== Guessed Words ===");
        foreach (var word in Data.guessedWords)
        {
            sb.AppendLine($"- {word}");
        }
        
        sb.AppendLine("\n=== Used Hints ===");
        foreach (var hint in Data.usedHintsData)
        {
            sb.AppendLine($"- Word: {hint.wordKey}, Levels: {string.Join(", ", hint.hintLevels)}");
        }
        
        sb.AppendLine("\n=== Settings ===");
        sb.AppendLine($"Sound Enabled: {Data.settings.soundEnabled}");
        sb.AppendLine($"Music Enabled: {Data.settings.musicEnabled}");
        sb.AppendLine($"Sound Volume: {Data.settings.soundVolume}");
        sb.AppendLine($"Music Volume: {Data.settings.musicVolume}");
        
        sb.AppendLine("\n=== Ad State ===");
        sb.AppendLine($"Can Watch: {Data.adState.canWatch}");
        sb.AppendLine($"Next Available: {Data.adState.nextAvailableTime}");
        
        yield return WriteTextToFileCoroutine(TXT_SAVE_PATH, sb.ToString());
        
        Debug.Log($"Text save file created at: {TXT_SAVE_PATH}");
    }

    public void LoadGame()
    {
        if (isLoading)
        {
            Debug.Log("Load operation already in progress, skipping");
            return;
        }
        
        StartCoroutine(LoadGameCoroutine());
    }
    
    private IEnumerator LoadGameCoroutine()
    {
        isLoading = true;
        Debug.Log($"Starting load operation from: {SavePath}");
        
        // Initialize with default data if we don't have any yet
        if (Data == null)
        {
            Data = new SaveData();
        }
        
        bool loadedSuccessfully = false;
        bool mainFileExists = false;
        bool backupFileExists = false;
        bool prefsBackupExists = false;
        
        // Check which sources exist
        try
        {
            mainFileExists = File.Exists(SavePath);
            backupFileExists = File.Exists(BACKUP_SAVE_PATH);
            prefsBackupExists = PlayerPrefs.HasKey("SaveGameBackup");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error checking save sources: {e.Message}");
        }
        
        // Try to load from main file
        if (mainFileExists)
        {
            Debug.Log($"Found main save file at: {SavePath}");
            bool loadResult = false;
            yield return LoadFromFileCoroutine(SavePath, result => loadResult = result);
            loadedSuccessfully = loadResult;
        }
        
        // If main file failed, try backup
        if (!loadedSuccessfully && backupFileExists)
        {
            Debug.Log($"Main save not found or failed, trying backup at: {BACKUP_SAVE_PATH}");
            bool loadResult = false;
            yield return LoadFromFileCoroutine(BACKUP_SAVE_PATH, result => loadResult = result);
            loadedSuccessfully = loadResult;
            
            // If backup loaded successfully, restore main file
            if (loadedSuccessfully)
            {
                string jsonData = JsonUtility.ToJson(Data, true);
                yield return WriteTextToFileCoroutine(SavePath, jsonData);
            }
        }
        
        // If both files failed, try PlayerPrefs
        if (!loadedSuccessfully && prefsBackupExists)
        {
            Debug.Log("No save files found or failed to load, trying PlayerPrefs backup");
            string jsonData = PlayerPrefs.GetString("SaveGameBackup");
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                bool prefsLoadSuccess = false;
                
                try
                {
                    SaveData loadedData = JsonUtility.FromJson<SaveData>(jsonData);
                    if (loadedData != null)
                    {
                        Data = loadedData;
                        // Convert long ticks back to DateTime
                        Data.adState.nextAvailableTime = new DateTime(Data.adState.nextAvailableTimeTicks);
                        prefsLoadSuccess = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading from PlayerPrefs: {e.Message}");
                }
                
                if (prefsLoadSuccess)
                {
                    // Apply the loaded data
                    yield return ApplyLoadedDataCoroutine();
                    
                    // Save to file system
                    yield return WriteTextToFileCoroutine(SavePath, jsonData);
                    loadedSuccessfully = true;
                }
            }
        }
        
        // If we couldn't load from any source, create a new game
        if (!loadedSuccessfully)
        {
            Debug.Log("No save data found, starting new game with shuffled words");
            Data = new SaveData();
            
            // Create initial text file
            yield return SaveGameAsTextCoroutine();
            
            // Initialize no ads state
            Data.noAdsBought = false;
            
            // Only shuffle words if this is the first time (no save file exists)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShuffleAllEraWords();
                // Immediately save the shuffled order
                yield return SaveGameCoroutine();
            }
        }
        
        // Try to recover from PlayerPrefs if available
        if (!loadedSuccessfully && PlayerPrefs.HasKey("Points"))
        {
            Debug.Log("Attempting to recover critical data from PlayerPrefs");
            Data = new SaveData();
            Data.points = PlayerPrefs.GetInt("Points", 0);
            Data.noAdsBought = PlayerPrefs.GetInt("NoAdsBought", 0) == 1;
            
            // Apply minimal data
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPoints(Data.points);
                GameManager.Instance.SetNoAdsBought(Data.noAdsBought);
            }
        }
        
        // Mark that we've loaded the game
        hasLoadedGame = true;
        isLoading = false;
    }
    
    private IEnumerator LoadFromFileCoroutine(string filePath, System.Action<bool> callback)
    {
        Debug.Log($"Loading from file: {filePath}");
        
        string jsonData = "";
        bool readSuccess = false;
        bool deserializeSuccess = false;
        
        try
        {
            // Special handling for Android
            if (Application.platform == RuntimePlatform.Android)
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        jsonData = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                jsonData = File.ReadAllText(filePath);
            }
            
            Debug.Log($"Successfully read save file from {filePath}, content length: {jsonData.Length}");
            
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError($"File content is empty: {filePath}");
                callback(false);
                yield break;
            }
            
            readSuccess = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading from file {filePath}: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            callback(false);
            yield break;
        }
        
        if (readSuccess)
        {
            try
            {
                // Deserialize the JSON data
                SaveData loadedData = JsonUtility.FromJson<SaveData>(jsonData);
                
                if (loadedData == null)
                {
                    Debug.LogError($"Failed to deserialize save data from {filePath}");
                    callback(false);
                    yield break;
                }
                
                // Update our data reference
                Data = loadedData;
                
                // Convert long ticks back to DateTime
                if (Data.adState.nextAvailableTimeTicks > 0)
                {
                    Data.adState.nextAvailableTime = new DateTime(Data.adState.nextAvailableTimeTicks);
                }
                else
                {
                    Data.adState.nextAvailableTime = DateTime.Now;
                }
                
                Debug.Log($"Successfully loaded save data from {filePath}");
                Debug.Log($"Loaded points: {Data.points}");
                Debug.Log($"Loaded {Data.solvedWords?.Count ?? 0} solved words");
                Debug.Log($"Loaded {Data.unlockedEras?.Count ?? 0} unlocked eras");
                
                deserializeSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deserializing save data from {filePath}: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                callback(false);
                yield break;
            }
        }
        
        // Apply the loaded data to the game state outside of any try-catch block
        if (deserializeSuccess)
        {
            yield return ApplyLoadedDataCoroutine();
            callback(true);
        }
        else
        {
            callback(false);
        }
    }
    
    private IEnumerator ApplyLoadedDataCoroutine()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("Cannot apply loaded data: GameManager.Instance is null");
            yield break;
        }
        
        // Load solved words
        if (Data.solvedWords != null)
        {
            GameManager.Instance.LoadSolvedBaseWords(new HashSet<string>(Data.solvedWords));
            yield return null; // Wait a frame to prevent freezing
        }

        // Load grid data with era information
        if (Data.preGeneratedGrids != null)
        {
            foreach (var gridData in Data.preGeneratedGrids)
            {
                if (!string.IsNullOrEmpty(gridData.era))
                {
                    GameManager.Instance.SwitchEra(gridData.era); // Set the era before loading grid
                }
            }
            GameManager.Instance.LoadGridData(Data.preGeneratedGrids);
            yield return null; // Wait a frame to prevent freezing
        }
        
        // Load hint usage data
        if (Data.usedHintsData != null)
        {
            Debug.Log($"Loading {Data.usedHintsData.Count} hint records");
            GameManager.Instance.LoadUsedHintsData(Data.usedHintsData);
            yield return null; // Wait a frame to prevent freezing
        }
        
        // Load saved shuffled words if they exist
        if (Data.shuffledWords != null && Data.shuffledWords.Count > 0)
        {
            Debug.Log("Loading saved word order");
            foreach (var kvp in Data.shuffledWords)
            {
                string[] parts = kvp.Key.Split('_');
                if (parts.Length == 2)
                {
                    string language = parts[0];
                    string era = parts[1];
                    if (GameManager.Instance.eraWordsPerLanguage.ContainsKey(language) &&
                        GameManager.Instance.eraWordsPerLanguage[language].ContainsKey(era))
                    {
                        GameManager.Instance.eraWordsPerLanguage[language][era] = new List<string>(kvp.Value);
                        Debug.Log($"Loaded word order for {era} in {language}: {string.Join(", ", kvp.Value)}");
                    }
                }
            }
            yield return null; // Wait a frame to prevent freezing
        }

        // Load unlocked eras
        if (Data.unlockedEras != null)
        {
            GameManager.Instance.SetUnlockedEras(new HashSet<string>(Data.unlockedEras));
        }
        else
        {
            // Initialize with default unlocked eras if none were saved
            HashSet<string> defaultEras = new HashSet<string> { "Ancient Egypt", "Medieval Europe" };
            GameManager.Instance.SetUnlockedEras(defaultEras);
        }
        yield return null; // Wait a frame to prevent freezing

        // Load ad state directly
        GameManager.Instance.LoadAdState(Data.adState);

        // Load no ads state
        GameManager.Instance.SetNoAdsBought(Data.noAdsBought);

        // Set points, guessed words, settings, and word guess count
        GameManager.Instance.SetPoints(Data.points);
        GameManager.Instance.SetGuessedWords(Data.guessedWords);
        GameManager.Instance.LoadSettings(Data.settings);
        GameManager.Instance.wordGuessCount = Data.wordGuessCount;
        
        Debug.Log("Successfully applied loaded data to game state");
    }

    public void DeleteSave()
    {
        StartCoroutine(DeleteSaveCoroutine());
    }
    
    private IEnumerator DeleteSaveCoroutine()
    {
        // Delete main save file
        if (File.Exists(SavePath))
        {
            try
            {
                File.Delete(SavePath);
                Debug.Log($"Deleted main save file: {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting main save file: {e.Message}");
            }
        }
        
        // Delete backup save file
        if (File.Exists(BACKUP_SAVE_PATH))
        {
            try
            {
                File.Delete(BACKUP_SAVE_PATH);
                Debug.Log($"Deleted backup save file: {BACKUP_SAVE_PATH}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting backup save file: {e.Message}");
            }
        }
        
        // Delete text save file
        if (File.Exists(TXT_SAVE_PATH))
        {
            try
            {
                File.Delete(TXT_SAVE_PATH);
                Debug.Log($"Deleted text save file: {TXT_SAVE_PATH}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting text save file: {e.Message}");
            }
        }
        
        yield return null;
        
        // Clear PlayerPrefs backup
        if (PlayerPrefs.HasKey("SaveGameBackup"))
        {
            try
            {
                PlayerPrefs.DeleteKey("SaveGameBackup");
                PlayerPrefs.DeleteKey("Points");
                PlayerPrefs.DeleteKey("NoAdsBought");
                PlayerPrefs.Save();
                Debug.Log("Cleared PlayerPrefs backup data");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error clearing PlayerPrefs: {e.Message}");
            }
        }
        
        Data = new SaveData();
        Debug.Log("Save files deleted");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("Application paused - saving game");
            SaveGame();
        }
        else
        {
            Debug.Log("Application resumed - reloading game");
            LoadGame();
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quitting - saving game");
        SaveGame();
    }

    public void Initialize()
    {
        // existing initialization code...
        
        if (Data.lastRewardedAdTimestamp == 0)
        {
            Data.lastRewardedAdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - REWARDED_AD_COOLDOWN_HOURS * 3600;
        }
        
        if (Data.wordGuessCount == 0)
        {
            Data.wordGuessCount = 0;
        }

        // Initialize lastDailySpinTimestamp if it's 0
        if (Data.lastDailySpinTimestamp == 0)
        {
            Data.lastDailySpinTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (DAILY_SPIN_COOLDOWN_HOURS * 3600);
        }
    }

    public void ResetDailySpinCooldown()
    {
        if (Data != null)
        {
            // Set the last spin timestamp to a time before the cooldown period
            Data.lastDailySpinTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (DAILY_SPIN_COOLDOWN_HOURS * 3600);
            SaveGame();
            Debug.Log("Daily spin cooldown reset");
        }
        else
        {
            Debug.LogError("SaveData is null, cannot reset cooldown");
        }
    }

    public void OpenSaveFileLocation()
    {
        string path = Application.persistentDataPath;
        Debug.Log($"Save file location: {path}");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(path);
        #elif UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", "/select," + TXT_SAVE_PATH);
        #elif UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", $"-R {TXT_SAVE_PATH}");
        #else
        Debug.Log($"Save file location: {path}");
        #endif
    }
}
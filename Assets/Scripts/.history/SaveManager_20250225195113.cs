using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // Add this line for ToList()
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_ANDROID
using UnityEngine.Networking;
#endif

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
    public string SavePath => Path.Combine(Application.persistentDataPath, "gamesave.bin");
    public string JsonSavePath => Path.Combine(Application.persistentDataPath, "gamesave.json");
    private string TXT_SAVE_PATH => Path.Combine(Application.persistentDataPath, "gamesave.txt");

    // Cooldown period for daily spin (24 hours)
    private const int DAILY_SPIN_COOLDOWN_HOURS = 24;

    // Cooldown period for rewarded ads (2 hours)
    private const int REWARDED_AD_COOLDOWN_HOURS = 2;


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Don't load game here anymore, let Start handle it
    }

    private void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log($"Running on Android device");
        Debug.Log($"Application.persistentDataPath: {Application.persistentDataPath}");
        Debug.Log($"JSON save path: {JsonSavePath}");
        
        // Verify file permissions using both methods
        VerifyAndroidFilePermissions();
        SimpleSaveSystem.VerifyFilePermissions();
        #endif
        
        LoadGame();
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    private void VerifyAndroidFilePermissions()
    {
        try
        {
            // Test if we can write to the directory
            string testFilePath = Path.Combine(Application.persistentDataPath, "test_permissions.txt");
            File.WriteAllText(testFilePath, "Testing file permissions");
            
            if (File.Exists(testFilePath))
            {
                Debug.Log("Successfully wrote test file, permissions OK");
                File.Delete(testFilePath);
            }
            else
            {
                Debug.LogError("Failed to write test file, permissions may be an issue");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error testing file permissions: {e.Message}");
        }
    }
    #endif

    public void SaveGame()
    {
        Debug.Log($"Saving game to: {SavePath}");
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
            foreach (var language in GameManager.Instance.eraWordsPerLanguage.Keys)
            {
                foreach (var era in GameManager.Instance.eraWordsPerLanguage[language].Keys)
                {
                    string key = $"{language}_{era}";
                    Data.shuffledWords[key] = new List<string>(GameManager.Instance.eraWordsPerLanguage[language][era]);
                }
            }
            
            // Prepare dictionary data for JSON serialization
            Data.PrepareForSerialization();

            // Save unlocked eras
            Data.unlockedEras = GameManager.Instance.GetUnlockedEras().ToList();

            // Save ad state
            Data.adState = GameManager.Instance.LoadAdState();
            // Prepare AdState for serialization
            Data.adState.PrepareForSerialization();
            
            Data.wordGuessCount = GameManager.Instance.wordGuessCount;

            // Save no ads state
            Data.noAdsBought = GameManager.Instance.NoAdsBought;

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Try both saving methods for Android
            try
            {
                // First try the SimpleSaveSystem (binary formatter)
                SimpleSaveSystem.Save(Data);
                Debug.Log("Game saved using SimpleSaveSystem (binary formatter)");
                
                // Also save using JSON as a backup
                string jsonData = JsonUtility.ToJson(Data, true);
                
                // Use a temporary file first to avoid corruption
                string tempPath = JsonSavePath + ".tmp";
                File.WriteAllText(tempPath, jsonData);
                
                // Verify the temp file was created successfully
                if (File.Exists(tempPath))
                {
                    // If the temp file is valid, replace the actual save file
                    if (File.Exists(JsonSavePath))
                    {
                        // Create a backup before replacing
                        string backupPath = JsonSavePath + ".bak";
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                        File.Copy(JsonSavePath, backupPath);
                        Debug.Log($"Created backup at: {backupPath}");
                        
                        File.Delete(JsonSavePath);
                    }
                    File.Move(tempPath, JsonSavePath);
                    
                    Debug.Log($"Game also saved as JSON for Android at: {JsonSavePath}");
                    Debug.Log($"File size: {new FileInfo(JsonSavePath).Length} bytes");
                }
                else
                {
                    Debug.LogError($"Failed to create temporary JSON save file at: {tempPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving file on Android: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
            #else
            // Save binary file for other platforms
            using (FileStream stream = new FileStream(SavePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, Data);
            }
            #endif
            
            // Save text file
            SaveGameAsText();
            
            Debug.Log($"Game saved successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    private void SaveGameAsText()
    {
        try
        {
            string fullPath = TXT_SAVE_PATH;
            Debug.Log($"Saving text file to: {fullPath}");
            
            using (StreamWriter writer = new StreamWriter(fullPath))
            {
                writer.WriteLine($"=== Game Save - {DateTime.Now} ===");
                writer.WriteLine($"Points: {Data.points}");
                writer.WriteLine($"No Ads Bought: {Data.noAdsBought}");
                writer.WriteLine($"Word Guess Count: {Data.wordGuessCount}");
                
                writer.WriteLine("\n=== Unlocked Eras ===");
                foreach (var era in Data.unlockedEras)
                {
                    writer.WriteLine($"- {era}");
                }
                
                writer.WriteLine("\n=== Solved Words ===");
                foreach (var word in Data.solvedWords)
                {
                    writer.WriteLine($"- {word}");
                }
                
                writer.WriteLine("\n=== Guessed Words ===");
                foreach (var word in Data.guessedWords)
                {
                    writer.WriteLine($"- {word}");
                }
                
                writer.WriteLine("\n=== Used Hints ===");
                foreach (var hint in Data.usedHintsData)
                {
                    writer.WriteLine($"- Word: {hint.wordKey}, Levels: {string.Join(", ", hint.hintLevels)}");
                }
                
                writer.WriteLine("\n=== Settings ===");
                writer.WriteLine($"Sound Enabled: {Data.settings.soundEnabled}");
                writer.WriteLine($"Music Enabled: {Data.settings.musicEnabled}");
                writer.WriteLine($"Sound Volume: {Data.settings.soundVolume}");
                writer.WriteLine($"Music Volume: {Data.settings.musicVolume}");
                
                writer.WriteLine("\n=== Ad State ===");
                writer.WriteLine($"Can Watch: {Data.adState.canWatch}");
                writer.WriteLine($"Next Available: {Data.adState.nextAvailableTime}");
            }
            
            Debug.Log($"Text save file created at: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save text file: {e.Message}");
        }
    }

    public void LoadGame()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log($"Loading game on Android");
        
        // First try to load using SimpleSaveSystem (binary formatter)
        try
        {
            Data = SimpleSaveSystem.Load();
            
            if (Data != null)
            {
                Debug.Log("Game loaded successfully using SimpleSaveSystem");
                
                // Process dictionary data after deserialization
                Data.ProcessAfterDeserialization();
                
                // Process AdState data
                Data.adState.ProcessAfterDeserialization();
                
                ProcessLoadedData();
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game using SimpleSaveSystem: {e.Message}");
            // Continue to try JSON method
        }
        
        // If SimpleSaveSystem failed, try the JSON method
        Debug.Log($"Trying to load game from JSON on Android: {JsonSavePath}");
        try
        {
            if (File.Exists(JsonSavePath))
            {
                Debug.Log($"JSON save file exists at: {JsonSavePath}");
                Debug.Log($"File size: {new FileInfo(JsonSavePath).Length} bytes");
                
                // Read the file with exception handling
                string jsonData = "";
                try
                {
                    jsonData = File.ReadAllText(JsonSavePath);
                    Debug.Log($"JSON data loaded, length: {jsonData.Length} characters");
                    
                    if (string.IsNullOrEmpty(jsonData))
                    {
                        Debug.LogError("JSON data is empty or null");
                        throw new Exception("Empty save file");
                    }
                }
                catch (Exception fileEx)
                {
                    Debug.LogError($"Error reading JSON file: {fileEx.Message}");
                    throw; // Re-throw to be caught by outer try/catch
                }
                
                // Try to deserialize with detailed error handling
                try
                {
                    Data = JsonUtility.FromJson<SaveData>(jsonData);
                    
                    if (Data == null)
                    {
                        Debug.LogError("Failed to deserialize save data from JSON");
                        throw new Exception("Deserialization returned null");
                    }
                }
                catch (Exception jsonEx)
                {
                    Debug.LogError($"JSON deserialization error: {jsonEx.Message}");
                    throw; // Re-throw to be caught by outer try/catch
                }
                
                Debug.Log("Successfully deserialized JSON data");
                
                // Process dictionary data after JSON deserialization
                Data.ProcessAfterDeserialization();
                
                // Process AdState data
                Data.adState.ProcessAfterDeserialization();
                
                ProcessLoadedData();
                Debug.Log("Game loaded successfully from JSON");
            }
            else
            {
                Debug.Log($"No save file found at: {JsonSavePath}, starting new game");
                InitializeNewGame();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game on Android: {e.Message}\nStack trace: {e.StackTrace}");
            
            // Try to recover from backup if it exists
            string backupPath = JsonSavePath + ".bak";
            if (File.Exists(backupPath))
            {
                Debug.Log("Attempting to recover from backup file...");
                try
                {
                    string backupData = File.ReadAllText(backupPath);
                    Data = JsonUtility.FromJson<SaveData>(backupData);
                    
                    if (Data != null)
                    {
                        Debug.Log("Successfully recovered from backup file");
                        Data.ProcessAfterDeserialization();
                        Data.adState.ProcessAfterDeserialization();
                        ProcessLoadedData();
                        
                        // Save the recovered data to the main file
                        SaveGame();
                        return;
                    }
                }
                catch (Exception backupEx)
                {
                    Debug.LogError($"Failed to recover from backup: {backupEx.Message}");
                }
            }
            
            // If all else fails, create a new save
            Debug.Log("Creating new save data after failed load");
            Data = new SaveData();
            InitializeNewGame();
        }
        #else
        Debug.Log($"Loading game from: {SavePath}");
        try
        {
            if (File.Exists(SavePath))
            {
                using (FileStream stream = new FileStream(SavePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    Data = (SaveData)formatter.Deserialize(stream);
                }
                
                ProcessLoadedData();
            }
            else
            {
                Debug.Log("No save file found, starting new game with shuffled words");
                InitializeNewGame();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            Data = new SaveData();
        }
        #endif
    }

    private void ProcessLoadedData()
    {
        if (GameManager.Instance != null)
        {
            // Load solved words
            if (Data.solvedWords != null)
            {
                GameManager.Instance.LoadSolvedBaseWords(new HashSet<string>(Data.solvedWords));
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
            }
            
            // Load hint usage data
            if (Data.usedHintsData != null)
            {
                Debug.Log($"Loading {Data.usedHintsData.Count} hint records");
                GameManager.Instance.LoadUsedHintsData(Data.usedHintsData);
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

            // Load ad state directly
            GameManager.Instance.LoadAdState(true, Data.adState);

            // Load no ads state
            GameManager.Instance.SetNoAdsBought(Data.noAdsBought);
        }

        GameManager.Instance.SetPoints(Data.points);
        GameManager.Instance.SetGuessedWords(Data.guessedWords);
        GameManager.Instance.LoadSettings(Data.settings);
        GameManager.Instance.wordGuessCount = Data.wordGuessCount;
    }

    private void InitializeNewGame()
    {
        Data = new SaveData();
        
        // Ensure AdState is properly initialized
        Data.adState = new AdState { canWatch = true, nextAvailableTime = DateTime.Now };
        Data.adState.PrepareForSerialization();
        
        // Create initial text file
        SaveGameAsText();
        
        // Initialize no ads state
        Data.noAdsBought = false;
        
        // Only shuffle words if this is the first time (no save file exists)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShuffleAllEraWords();
            // Immediately save the shuffled order
            SaveGame();
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
        
        if (File.Exists(JsonSavePath))
        {
            File.Delete(JsonSavePath);
        }
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Also delete SimpleSaveSystem files
        SimpleSaveSystem.DeleteSave();
        #endif
        
        Data = new SaveData();
        Debug.Log("Save files deleted");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
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
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    // Method to test the SimpleSaveSystem directly
    public void TestSimpleSaveSystem()
    {
        try
        {
            Debug.Log("Testing SimpleSaveSystem...");
            
            // Create test data
            SaveData testData = new SaveData();
            testData.points = 999;
            testData.wordGuessCount = 888;
            testData.noAdsBought = true;
            
            // Save test data
            SimpleSaveSystem.Save(testData);
            Debug.Log("Test data saved successfully");
            
            // Load test data
            SaveData loadedData = SimpleSaveSystem.Load();
            
            if (loadedData != null)
            {
                Debug.Log($"Test data loaded successfully. Points: {loadedData.points}, WordGuessCount: {loadedData.wordGuessCount}, NoAdsBought: {loadedData.noAdsBought}");
                
                if (loadedData.points == 999 && loadedData.wordGuessCount == 888 && loadedData.noAdsBought == true)
                {
                    Debug.Log("SimpleSaveSystem TEST PASSED: Data integrity verified");
                }
                else
                {
                    Debug.LogError("SimpleSaveSystem TEST FAILED: Data integrity check failed");
                }
            }
            else
            {
                Debug.LogError("SimpleSaveSystem TEST FAILED: Loaded data is null");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"SimpleSaveSystem TEST FAILED: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
    #endif
}
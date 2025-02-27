using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // Add this line for ToList()
using System.Runtime.Serialization.Formatters.Binary;

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
        LoadGame();
    }

    public void SaveGame()
    {
        Debug.Log($"Saving game to: {SavePath}");
        try
        {
            // Initialize Data if it doesn't exist yet
            if (Data == null)
            {
                Data = new SaveData();
            }
            
            // Save points and guessed words
            Data.points = GameManager.Instance.CurrentPoints;
            Data.guessedWords = GameManager.Instance.GetAllSolvedBaseWords().ToList();
            Debug.Log($"Saving {Data.guessedWords.Count} guessed words (using GetAllSolvedBaseWords): {string.Join(", ", Data.guessedWords)}");
            
            // Save hint usage data
            Data.usedHintsData = GameManager.Instance.GetUsedHintsData();
            Debug.Log($"Saving {Data.usedHintsData.Count} hint records");
            
            // Save grid data
            List<GridData> gridDataList;
            GameManager.Instance.SaveGridData(out gridDataList);
            Data.preGeneratedGrids = gridDataList;

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

            // Save unlocked eras
            Data.unlockedEras = GameManager.Instance.GetUnlockedEras().ToList();
            Debug.Log($"Saving unlocked eras: {string.Join(", ", Data.unlockedEras)}");

            // Save ad state by creating a new AdState
            var currentAdState = GameManager.Instance.LoadAdState();
            Data.adState = new AdState 
            {
                canWatch = currentAdState.canWatch,
                nextAvailableTime = currentAdState.nextAvailableTime
            };
            Data.wordGuessCount = GameManager.Instance.wordGuessCount;

            // Save no ads state
            Data.noAdsBought = GameManager.Instance.NoAdsBought;

            using (FileStream stream = new FileStream(SavePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, Data);
            }
            
            Debug.Log($"Game saved successfully! Current points: {Data.points}, Guessed words: {Data.guessedWords.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    public void LoadGame()
    {
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

                if (GameManager.Instance == null)
                {
                    Debug.LogError("GameManager.Instance is null during LoadGame! Cannot load saved game state.");
                    return;
                }

                Debug.Log($"===== LOADING GAME STATE =====");
                Debug.Log($"Loaded from save file - Points: {Data.points}, Words: {(Data.guessedWords != null ? Data.guessedWords.Count : 0)}");
                
                // Load game settings first
                if (Data.settings != null)
                {
                    GameManager.Instance.LoadSettings(Data.settings);
                    Debug.Log("Game settings loaded");
                }
                
                // Load hint usage data
                if (Data.usedHintsData != null)
                {
                    GameManager.Instance.LoadUsedHintsData(Data.usedHintsData);
                    Debug.Log($"Loaded {Data.usedHintsData.Count} hint records");
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
                                Debug.Log($"Loaded word order for {era} in {language}");
                            }
                        }
                    }
                }

                // Load unlocked eras
                if (Data.unlockedEras != null)
                {
                    GameManager.Instance.SetUnlockedEras(new HashSet<string>(Data.unlockedEras));
                    Debug.Log($"Loaded {Data.unlockedEras.Count} unlocked eras");
                }
                else
                {
                    // Initialize with default unlocked eras if none were saved
                    HashSet<string> defaultEras = new HashSet<string> { "Ancient Egypt", "Medieval Europe" };
                    GameManager.Instance.SetUnlockedEras(defaultEras);
                    Debug.Log("Using default unlocked eras");
                }

                // Load ad state directly
                GameManager.Instance.LoadAdState(Data.adState);
                Debug.Log("Ad state loaded");

                // Load no ads state
                GameManager.Instance.SetNoAdsBought(Data.noAdsBought);
                Debug.Log($"No ads state loaded: {Data.noAdsBought}");
                
                // Load grid data
                if (Data.preGeneratedGrids != null)
                {
                    GameManager.Instance.LoadGridData(Data.preGeneratedGrids);
                    Debug.Log($"Loaded {Data.preGeneratedGrids.Count} grid data items");
                }
                
                GameManager.Instance.wordGuessCount = Data.wordGuessCount;
                Debug.Log($"Word guess count loaded: {Data.wordGuessCount}");
                
                // Load points AFTER everything else is set up
                Debug.Log($"Setting points to: {Data.points}");
                GameManager.Instance.SetPoints(Data.points);
                
                // Verify points were loaded correctly
                Debug.Log($"Current points after loading: {GameManager.Instance.CurrentPoints}");
                
                // Load guessed words LAST to ensure all eras are initialized
                if (Data.guessedWords != null && Data.guessedWords.Count > 0)
                {
                    Debug.Log($"Setting {Data.guessedWords.Count} guessed words: {string.Join(", ", Data.guessedWords)}");
                    GameManager.Instance.SetGuessedWords(Data.guessedWords);
                    
                    // Verify guessed words were loaded correctly
                    List<string> currentGuessedWords = GameManager.Instance.GetAllSolvedBaseWords().ToList();
                    Debug.Log($"Guessed words after loading: {currentGuessedWords.Count} - {string.Join(", ", currentGuessedWords)}");
                }
                else
                {
                    Debug.Log("No guessed words found in save data");
                }
                
                Debug.Log($"Game loaded successfully! Points: {Data.points}, Guessed words: {(Data.guessedWords != null ? Data.guessedWords.Count : 0)}");
                Debug.Log($"===== GAME STATE LOADED =====");
            }
            else
            {
                Debug.Log("No save file found, starting new game with shuffled words");
                Data = new SaveData();
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
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}\nStack trace: {e.StackTrace}");
            Data = new SaveData();
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Data = new SaveData();
            Debug.Log("Save file deleted");
        }
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
}
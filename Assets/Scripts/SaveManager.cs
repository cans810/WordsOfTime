using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // Add this line for ToList()

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

    public SaveData saveData { get; private set; }  // Make it publicly readable but privately settable
    public string SavePath => Path.Combine(Application.persistentDataPath, "gamesave.json");

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
            saveData = new SaveData();
            
            // Save points and guessed words
            saveData.points = GameManager.Instance.CurrentPoints;
            saveData.guessedWords = GameManager.Instance.GetGuessedWords();
            
            // Save hint usage data
            saveData.usedHintsData = GameManager.Instance.GetUsedHintsData();
            Debug.Log($"Saving {saveData.usedHintsData.Count} hint records");
            
            // Save grid data
            List<GridData> gridDataList;
            GameManager.Instance.SaveGridData(out gridDataList);
            saveData.preGeneratedGrids = gridDataList;

            // Save game settings
            saveData.settings = GameManager.Instance.GetSettings();

            // Save shuffled words
            foreach (var language in GameManager.Instance.eraWordsPerLanguage.Keys)
            {
                foreach (var era in GameManager.Instance.eraWordsPerLanguage[language].Keys)
                {
                    string key = $"{language}_{era}";
                    saveData.shuffledWords[key] = new List<string>(GameManager.Instance.eraWordsPerLanguage[language][era]);
                }
            }

            // Save unlocked eras
            saveData.unlockedEras = GameManager.Instance.GetUnlockedEras().ToList();

            string json = JsonUtility.ToJson(saveData, true);  // Added true for pretty print
            File.WriteAllText(SavePath, json);
            
            Debug.Log($"Game saved successfully!");
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
                string json = File.ReadAllText(SavePath);
                saveData = JsonUtility.FromJson<SaveData>(json);

                if (GameManager.Instance != null)
                {
                    // Load hint usage data
                    if (saveData.usedHintsData != null)
                    {
                        Debug.Log($"Loading {saveData.usedHintsData.Count} hint records");
                        GameManager.Instance.LoadUsedHintsData(saveData.usedHintsData);
                    }
                    
                    // Load saved shuffled words if they exist
                    if (saveData.shuffledWords != null && saveData.shuffledWords.Count > 0)
                    {
                        Debug.Log("Loading saved word order");
                        foreach (var kvp in saveData.shuffledWords)
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
                    if (saveData.unlockedEras != null)
                    {
                        GameManager.Instance.SetUnlockedEras(new HashSet<string>(saveData.unlockedEras));
                    }
                    else
                    {
                        // Initialize with default unlocked eras if none were saved
                        HashSet<string> defaultEras = new HashSet<string> { "Ancient Egypt", "Medieval Europe" };
                        GameManager.Instance.SetUnlockedEras(defaultEras);
                    }
                }

                GameManager.Instance.SetPoints(saveData.points);
                GameManager.Instance.SetGuessedWords(saveData.guessedWords);
                GameManager.Instance.LoadGridData(saveData.preGeneratedGrids);
                GameManager.Instance.LoadSettings(saveData.settings);
            }
            else
            {
                Debug.Log("No save file found, starting new game with shuffled words");
                saveData = new SaveData();
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
            Debug.LogError($"Failed to load game: {e.Message}");
            saveData = new SaveData();
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            saveData = new SaveData();
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
}
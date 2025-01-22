using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SaveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    _instance = go.AddComponent<SaveManager>();
                }
            }
            return _instance;
        }
    }

    private SaveData saveData;
    private string SavePath => Path.Combine(Application.persistentDataPath, "gamesave.json");

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadGame();
    }

    public void SaveGame()
    {
        try
        {
            Debug.Log("Starting game save...");
            saveData = new SaveData();
            
            // Save points
            saveData.points = GameManager.Instance.CurrentPoints;
            
            // Save settings
            saveData.settings = GameManager.Instance.GetSettings();

            // Save guessed words
            saveData.guessedWords = GameManager.Instance.GetGuessedWords();
            Debug.Log($"Saving guessed words: {string.Join(", ", saveData.guessedWords)}");

            // Save grid data
            List<GridData> gridDataList;
            GameManager.Instance.SaveGridData(out gridDataList);
            saveData.preGeneratedGrids = gridDataList;

            string json = JsonUtility.ToJson(saveData);
            File.WriteAllText(SavePath, json);
            
            Debug.Log($"Game saved successfully! Save file: {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    public void LoadGame()
    {
        try
        {
            Debug.Log($"Attempting to load game from: {SavePath}");
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                saveData = JsonUtility.FromJson<SaveData>(json);

                Debug.Log($"Loaded save data - Guessed words count: {saveData.guessedWords?.Count ?? 0}");
                if (saveData.guessedWords != null)
                {
                    Debug.Log($"Loaded guessed words: {string.Join(", ", saveData.guessedWords)}");
                }

                // Load points
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPoints(saveData.points);
                    GameManager.Instance.SetGuessedWords(saveData.guessedWords);
                    GameManager.Instance.LoadGridData(saveData.preGeneratedGrids);
                }
                else
                {
                    Debug.LogError("GameManager instance not found during load!");
                }

                Debug.Log("Game loaded successfully!");
            }
            else
            {
                Debug.Log("No save file found, starting new game");
                saveData = new SaveData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}\nStack trace: {e.StackTrace}");
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
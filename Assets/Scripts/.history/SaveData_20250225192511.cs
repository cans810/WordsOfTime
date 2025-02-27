using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HintData
{
    public string wordKey;
    public List<int> hintLevels;

    public HintData(string key, List<int> levels)
    {
        wordKey = key;
        hintLevels = levels;
    }
}

[Serializable]
public class SaveData
{
    public int points;
    public GameSettings settings = new GameSettings();
    public List<GridData> preGeneratedGrids = new List<GridData>();
    public List<string> guessedWords = new List<string>();
    public bool notifications;
    
    // For JSON serialization, we need to handle dictionaries differently
    [NonSerialized]
    public Dictionary<string, List<string>> shuffledWords = new Dictionary<string, List<string>>();
    
    // Serializable version of shuffledWords
    public List<ShuffledWordEntry> shuffledWordsList = new List<ShuffledWordEntry>();
    
    public List<HintData> usedHintsData = new List<HintData>();
    public List<string> unlockedEras = new List<string>();
    public AdState adState;
    
    // For JSON serialization of AdState.nextAvailableTime
    public string adStateNextAvailableTime;
    
    public int gamesPlayedSinceLastAd;
    public long lastRewardedAdTimestamp;
    public long lastDailySpinTimestamp;
    public int wordGuessCount;
    public long lastClosedTime;
    public bool noAdsBought;
    public List<string> solvedWords = new List<string>();

    public SaveData()
    {
        guessedWords = new List<string>();
        preGeneratedGrids = new List<GridData>();
        settings = new GameSettings();
        usedHintsData = new List<HintData>();
        shuffledWords = new Dictionary<string, List<string>>();
        shuffledWordsList = new List<ShuffledWordEntry>();
        unlockedEras = new List<string>();
        adState = new AdState { canWatch = true, nextAvailableTime = DateTime.Now };
        gamesPlayedSinceLastAd = 0;
        lastRewardedAdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        lastDailySpinTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        wordGuessCount = 0;
        lastClosedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        noAdsBought = false;
    }
    
    // Convert dictionary to list before serialization
    public void PrepareForSerialization()
    {
        shuffledWordsList.Clear();
        foreach (var kvp in shuffledWords)
        {
            shuffledWordsList.Add(new ShuffledWordEntry { key = kvp.Key, words = kvp.Value });
        }
        
        // Save DateTime as string for JSON serialization
        adStateNextAvailableTime = adState.nextAvailableTime.ToString("o"); // ISO 8601 format
    }
    
    // Convert list back to dictionary after deserialization
    public void ProcessAfterDeserialization()
    {
        shuffledWords.Clear();
        foreach (var entry in shuffledWordsList)
        {
            shuffledWords[entry.key] = entry.words;
        }
        
        // Restore DateTime from string after JSON deserialization
        if (!string.IsNullOrEmpty(adStateNextAvailableTime))
        {
            try
            {
                DateTime parsedTime = DateTime.Parse(adStateNextAvailableTime);
                adState.nextAvailableTime = parsedTime;
            }
            catch
            {
                adState.nextAvailableTime = DateTime.Now;
            }
        }
    }
}

[Serializable]
public class ShuffledWordEntry
{
    public string key;
    public List<string> words = new List<string>();
}

[Serializable]
public class GameSettings
{
    public bool soundEnabled = true;
    public bool musicEnabled = true;
    public bool notificationsEnabled = true;
    public float soundVolume = 1f;
    public float musicVolume = 1f;
}

[Serializable]
public class GridData
{
    public string era;
    public string targetWord;
    public List<string> letters = new List<string>();
    public int gridSize;
    public List<Vector2IntSerializable> correctWordPositions = new List<Vector2IntSerializable>();
    public bool isSolved;
}

[Serializable]
public struct Vector2IntSerializable
{
    public int x;
    public int y;

    public Vector2IntSerializable(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

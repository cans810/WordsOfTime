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

    // Parameterless constructor for JSON deserialization
    public HintData() 
    {
        hintLevels = new List<int>();
    }
}

[Serializable]
public class StringListPair
{
    public string key;
    public List<string> value;

    public StringListPair(string key, List<string> value)
    {
        this.key = key;
        this.value = value;
    }

    // Parameterless constructor for JSON deserialization
    public StringListPair() 
    {
        value = new List<string>();
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
    
    // Replace Dictionary with serializable list
    [SerializeField]
    private List<StringListPair> _shuffledWordsList = new List<StringListPair>();
    
    // Property to convert between Dictionary and List
    public Dictionary<string, List<string>> shuffledWords 
    {
        get 
        {
            Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
            foreach (var pair in _shuffledWordsList)
            {
                dict[pair.key] = pair.value;
            }
            return dict;
        }
        set 
        {
            _shuffledWordsList.Clear();
            foreach (var kvp in value)
            {
                _shuffledWordsList.Add(new StringListPair(kvp.Key, kvp.Value));
            }
        }
    }
    
    public List<HintData> usedHintsData = new List<HintData>();
    public List<string> unlockedEras = new List<string>();
    public AdState adState;
    public int gamesPlayedSinceLastAd;
    public long lastRewardedAdTimestamp;
    public long lastDailySpinTimestamp;
    public int wordGuessCount;
    public long lastClosedTime;
    public bool noAdsBought;
    public List<string> solvedWords = new List<string>();
    public string gameVersion = "1.0";

    public SaveData()
    {
        guessedWords = new List<string>();
        preGeneratedGrids = new List<GridData>();
        settings = new GameSettings();
        usedHintsData = new List<HintData>();
        _shuffledWordsList = new List<StringListPair>();
        unlockedEras = new List<string>();
        adState = new AdState { canWatch = true, nextAvailableTime = DateTime.Now };
        gamesPlayedSinceLastAd = 0;
        lastRewardedAdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        lastDailySpinTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        wordGuessCount = 0;
        lastClosedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        noAdsBought = false;
        gameVersion = Application.version;
    }
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

    // Parameterless constructor for JSON deserialization
    public GridData()
    {
        letters = new List<string>();
        correctWordPositions = new List<Vector2IntSerializable>();
    }
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

[Serializable]
public class AdState
{
    public bool canWatch;
    public DateTime nextAvailableTime;

    // Parameterless constructor for JSON deserialization
    public AdState()
    {
        canWatch = true;
        nextAvailableTime = DateTime.Now;
    }
} 
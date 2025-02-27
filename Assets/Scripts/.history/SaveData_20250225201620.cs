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
    
    // Dictionary to store shuffled words for each era and language
    private Dictionary<string, List<string>> _shuffledWords;
    
    // Property to handle serialization of dictionary
    public List<StringListPair> serializedShuffledWords;
    
    // Property to access the dictionary
    public Dictionary<string, List<string>> shuffledWords
    {
        get
        {
            if (_shuffledWords == null)
            {
                _shuffledWords = new Dictionary<string, List<string>>();
                if (serializedShuffledWords != null)
                {
                    foreach (var pair in serializedShuffledWords)
                    {
                        _shuffledWords[pair.key] = pair.value;
                    }
                }
            }
            return _shuffledWords;
        }
        set
        {
            _shuffledWords = value;
            serializedShuffledWords = new List<StringListPair>();
            if (_shuffledWords != null)
            {
                foreach (var kvp in _shuffledWords)
                {
                    serializedShuffledWords.Add(new StringListPair { key = kvp.Key, value = kvp.Value });
                }
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
        points = 0;
        guessedWords = new List<string>();
        usedHintsData = new List<HintData>();
        preGeneratedGrids = new List<GridData>();
        solvedWords = new List<string>();
        settings = new GameSettings();
        unlockedEras = new List<string>();
        adState = new AdState
        {
            canWatch = true,
            nextAvailableTime = DateTime.Now.AddHours(-2) // Default to available
        };
        wordGuessCount = 0;
        noAdsBought = false;
        lastClosedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        lastRewardedAdTimestamp = 0;
        lastDailySpinTimestamp = 0;
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
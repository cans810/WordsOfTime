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
    public Dictionary<string, List<string>> shuffledWords = new Dictionary<string, List<string>>();
    public List<HintData> usedHintsData = new List<HintData>();
    public List<string> unlockedEras = new List<string>();
    public AdState adState;
    public int gamesPlayedSinceLastAd;
    public long lastRewardedAdTimestamp;
    public long lastDailySpinTimestamp;
    public int wordGuessCount;
    public long lastClosedTime;
    public List<string> solvedWords = new List<string>();
    public Dictionary<string, List<Vector2IntSerializable>> solvedWordPositions = new Dictionary<string, List<Vector2IntSerializable>>();
    public Dictionary<string, List<string>> solvedBaseWordsPerEra = new Dictionary<string, List<string>>();

    public SaveData()
    {
        guessedWords = new List<string>();
        preGeneratedGrids = new List<GridData>();
        settings = new GameSettings();
        usedHintsData = new List<HintData>();
        shuffledWords = new Dictionary<string, List<string>>();
        unlockedEras = new List<string>();
        adState = new AdState { canWatch = true, nextAvailableTime = DateTime.Now };
        gamesPlayedSinceLastAd = 0;
        lastRewardedAdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        lastDailySpinTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        wordGuessCount = 0;
        solvedWords = new List<string>();
        lastClosedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        solvedWordPositions = new Dictionary<string, List<Vector2IntSerializable>>();
        solvedBaseWordsPerEra = new Dictionary<string, List<string>>();
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<string> EraList = new List<string>();
    public string CurrentEra { get; set; } = "";
    private int currentEraIndex = -1;
    public List<Sprite> eraImages = new List<Sprite>();
    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    public List<string> unsolvedWordsInCurrentEra;
    public Dictionary<string, List<char>> InitialGrids { get; private set; } = new Dictionary<string, List<char>>();
    private const int GRID_SIZE = 6;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadWordSets();
        GenerateAllGrids();
        StartWithRandomEra();
    }

    private void GenerateAllGrids()
    {
        foreach (var era in wordSetsWithSentences.Keys)
        {
            foreach (var word in wordSetsWithSentences[era].Keys)
            {
                if (!InitialGrids.ContainsKey(word))
                {
                    InitialGrids[word] = GenerateGridForWord(word);
                }
            }
        }
    }

    private List<char> GenerateGridForWord(string word)
    {
        List<char> grid = new List<char>(new char[GRID_SIZE * GRID_SIZE]);
        List<int> availablePositions = new List<int>();
        for (int i = 0; i < GRID_SIZE * GRID_SIZE; i++)
        {
            availablePositions.Add(i);
        }

        List<int> wordPositions = PlaceWordAdjacently(word, availablePositions);
        
        for (int i = 0; i < word.Length; i++)
        {
            grid[wordPositions[i]] = word[i];
            availablePositions.Remove(wordPositions[i]);
        }

        foreach (int pos in availablePositions)
        {
            grid[pos] = (char)Random.Range('A', 'Z' + 1);
        }

        return grid;
    }

    private List<int> PlaceWordAdjacently(string word, List<int> availablePositions)
    {
        List<int> wordPositions = new List<int>();
        List<int> shuffledPositions = new List<int>(availablePositions);
        
        // Shuffle starting positions more thoroughly
        for (int i = shuffledPositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = shuffledPositions[i];
            shuffledPositions[i] = shuffledPositions[j];
            shuffledPositions[j] = temp;
        }

        // Try multiple random starting positions
        foreach (int startPos in shuffledPositions)
        {
            wordPositions.Clear();
            if (TryPlaceWordFromPosition(word, startPos, availablePositions, wordPositions))
            {
                Debug.Log($"Placed word '{word}' starting at position {startPos}");
                return new List<int>(wordPositions);
            }
        }

        Debug.LogWarning($"Failed to place word '{word}' adjacently, using random placement");
        return PlaceWordRandomly(word, availablePositions);
    }

    private bool TryPlaceWordFromPosition(string word, int startPos, List<int> availablePositions, List<int> wordPositions)
    {
        wordPositions.Add(startPos);
        
        for (int i = 1; i < word.Length; i++)
        {
            List<int> adjacentPositions = GetAdjacentPositions(wordPositions[i - 1]);
            bool foundValidPosition = false;

            foreach (int pos in adjacentPositions)
            {
                if (availablePositions.Contains(pos) && !wordPositions.Contains(pos))
                {
                    wordPositions.Add(pos);
                    foundValidPosition = true;
                    break;
                }
            }

            if (!foundValidPosition) return false;
        }

        return true;
    }

    private List<int> GetAdjacentPositions(int position)
    {
        List<int> adjacent = new List<int>();
        int row = position / GRID_SIZE;
        int col = position % GRID_SIZE;

        int[] dr = { -1, 0, 1, 0 };
        int[] dc = { 0, 1, 0, -1 };

        for (int i = 0; i < 4; i++)
        {
            int newRow = row + dr[i];
            int newCol = col + dc[i];
            
            if (newRow >= 0 && newRow < GRID_SIZE && newCol >= 0 && newCol < GRID_SIZE)
            {
                adjacent.Add(newRow * GRID_SIZE + newCol);
            }
        }

        return adjacent;
    }

    private List<int> PlaceWordRandomly(string word, List<int> availablePositions)
    {
        List<int> positions = new List<int>();
        List<int> tempAvailable = new List<int>(availablePositions);
        
        for (int i = 0; i < word.Length && tempAvailable.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, tempAvailable.Count);
            positions.Add(tempAvailable[randomIndex]);
            tempAvailable.RemoveAt(randomIndex);
        }

        return positions;
    }

    private void LoadWordSets()
    {
        string json = System.IO.File.ReadAllText(Application.dataPath + "/words.json");
        WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

        wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();
        foreach (var wordSet in wordSetList.sets)
        {
            var wordDict = new Dictionary<string, List<string>>();
            foreach (var wordEntry in wordSet.words)
            {
                wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences);
            }
            wordSetsWithSentences[wordSet.era] = wordDict;
        }
    }

    private void StartWithRandomEra()
    {
        if (EraList.Count == 0) return;
        currentEraIndex = Random.Range(0, EraList.Count);
        CurrentEra = EraList[currentEraIndex];
        ResetUnsolvedWordsForEra(CurrentEra);
    }

    public void MoveToNextEra()
    {
        currentEraIndex++;
        if (currentEraIndex < EraList.Count)
        {
            CurrentEra = EraList[currentEraIndex];
            ResetUnsolvedWordsForEra(CurrentEra);
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    public string GetNextWord()
    {
        if (unsolvedWordsInCurrentEra == null || unsolvedWordsInCurrentEra.Count == 0)
        {
            SceneManager.LoadScene("MainMenuScene");
            return null;
        }

        int randomIndex = Random.Range(0, unsolvedWordsInCurrentEra.Count);
        string nextWord = unsolvedWordsInCurrentEra[randomIndex];
        unsolvedWordsInCurrentEra.RemoveAt(randomIndex);
        return nextWord;
    }

    public void SelectEra(string eraName)
    {
        CurrentEra = eraName;
        currentEraIndex = EraList.IndexOf(eraName);
        ResetUnsolvedWordsForEra(CurrentEra);
    }

    private void ResetUnsolvedWordsForEra(string era)
    {
        if (wordSetsWithSentences.ContainsKey(era))
        {
            unsolvedWordsInCurrentEra = new List<string>(wordSetsWithSentences[era].Keys);
        }
    }

    public Sprite getEraImage(string era)
    {
        int index = EraList.IndexOf(era);
        return index >= 0 && index < eraImages.Count ? eraImages[index] : null;
    }
}
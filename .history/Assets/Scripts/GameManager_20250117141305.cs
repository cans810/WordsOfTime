using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour{
    public static GameManager Instance { get; private set; }

    public List<string> EraList = new List<string>();
    public string CurrentEra { get; set; } = "";
    private int currentEraIndex = -1;

    public List<Sprite> eraImages = new List<Sprite>();

    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    public List<string> unsolvedWordsInCurrentEra;

    public Dictionary<string, List<char>> InitialGrids { get; private set; } = new Dictionary<string, List<char>>();
    public Dictionary<string, bool> GridsGenerated { get; private set; } = new Dictionary<string, bool>();
    private const int GRID_SIZE = 6; // Match your GridManager's grid size

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
        GenerateAllGrids(); // New method to generate all grids at startup
        StartWithRandomEra();
    }

    private void StartWithRandomEra()
    {
        if(EraList.Count == 0)
        {
        Debug.LogError("Era List is empty! Add eras to the list to continue");
        return;
        }
        currentEraIndex = Random.Range(0, EraList.Count); 
        CurrentEra = EraList[currentEraIndex]; 
        ResetUnsolvedWordsForEra(CurrentEra);
        Debug.Log($"Started with random era: {CurrentEra}");
    }


    private void GenerateAllGrids()
    {
        foreach (var era in wordSetsWithSentences.Keys)
        {
            foreach (var word in wordSetsWithSentences[era].Keys)
            {
                if (!InitialGrids.ContainsKey(word))
                {
                    List<char> grid = GenerateGridForWord(word);
                    InitialGrids.Add(word, grid);
                    GridsGenerated[word] = true;
                }
            }
        }
        Debug.Log($"Generated grids for {InitialGrids.Count} words");
    }

    private List<char> GenerateGridForWord(string word)
    {
        List<char> grid = new List<char>(new char[GRID_SIZE * GRID_SIZE]);
        
        // Place the word's letters first
        List<int> availablePositions = new List<int>();
        for (int i = 0; i < GRID_SIZE * GRID_SIZE; i++)
        {
            availablePositions.Add(i);
        }

        // Place word letters adjacently
        List<int> wordPositions = PlaceWordAdjacently(word, availablePositions);
        
        // Place the word's letters
        for (int i = 0; i < word.Length; i++)
        {
            grid[wordPositions[i]] = word[i];
            availablePositions.Remove(wordPositions[i]);
        }

        // Fill remaining spaces with random letters
        foreach (int pos in availablePositions)
        {
            grid[pos] = (char)UnityEngine.Random.Range('A', 'Z' + 1);
        }

        return grid;
    }

    private List<int> PlaceWordAdjacently(string word, List<int> availablePositions)
    {
        List<int> wordPositions = new List<int>();
        
        // Shuffle available positions for random starting point
        List<int> shuffledPositions = new List<int>(availablePositions);
        for (int i = shuffledPositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = shuffledPositions[i];
            shuffledPositions[i] = shuffledPositions[j];
            shuffledPositions[j] = temp;
        }

        // Try each position as a starting point
        foreach (int startPos in shuffledPositions)
        {
            wordPositions.Clear();
            if (TryPlaceWordFromPosition(word, startPos, availablePositions, wordPositions))
            {
                return new List<int>(wordPositions);
            }
        }

        Debug.LogWarning($"Could not place word '{word}' adjacently, falling back to random placement");
        return PlaceWordRandomly(word, availablePositions);
    }

    private bool TryPlaceWordFromPosition(string word, int startPos, List<int> availablePositions, List<int> wordPositions)
    {
        wordPositions.Add(startPos);
        
        // For each remaining letter in the word
        for (int i = 1; i < word.Length; i++)
        {
            List<int> adjacentPositions = GetAdjacentPositions(wordPositions[i - 1]);
            
            // Shuffle adjacent positions for randomness
            for (int j = adjacentPositions.Count - 1; j > 0; j--)
            {
                int k = Random.Range(0, j + 1);
                int temp = adjacentPositions[j];
                adjacentPositions[j] = adjacentPositions[k];
                adjacentPositions[k] = temp;
            }

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

            if (!foundValidPosition)
            {
                return false; // Could not place this letter, this path won't work
            }
        }

        return true;
    }

    private List<int> GetAdjacentPositions(int position)
    {
        List<int> adjacent = new List<int>();
        int row = position / GRID_SIZE;
        int col = position % GRID_SIZE;

        // Define possible directions (up, right, down, left)
        int[] dr = { -1, 0, 1, 0 };
        int[] dc = { 0, 1, 0, -1 };

        // Check all four directions in random order
        List<int> directions = new List<int> { 0, 1, 2, 3 };
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }

        foreach (int dir in directions)
        {
            int newRow = row + dr[dir];
            int newCol = col + dc[dir];
            
            if (newRow >= 0 && newRow < GRID_SIZE && 
                newCol >= 0 && newCol < GRID_SIZE)
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

        for (int i = 0; i < word.Length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, tempAvailable.Count);
            positions.Add(tempAvailable[randomIndex]);
            tempAvailable.RemoveAt(randomIndex);
        }

        return positions;
    }


    private void LoadWordSets()
    {
        string filePath = Application.dataPath + "/words.json";
        Debug.Log($"Attempting to load words from: {filePath}");

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

                if (wordSetList != null && wordSetList.sets != null && wordSetList.sets.Length > 0)
                {
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
                else
                {
                    Debug.LogError("Failed to parse JSON: WordSetList or sets array is null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing JSON file: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"words.json file not found at path: {filePath}");
        }
    }

    public void InitializeUnsolvedWords()
    {
        if (unsolvedWordsInCurrentEra == null)
        {
            unsolvedWordsInCurrentEra = new List<string>(WordValidator.GetWordsForEra(CurrentEra));
        }
    }


    public void MoveToNextEra()
    {
        currentEraIndex++;
        if (currentEraIndex < EraList.Count)
        {
            CurrentEra = EraList[currentEraIndex];
            ResetUnsolvedWordsForEra(CurrentEra); // Reset words when moving to a new era
            Debug.Log($"Moved to next era: {CurrentEra}");
        }
        else
        {
            Debug.Log("All eras completed!");
            SceneManager.LoadScene("MainMenuScene"); 
        }
    }

    public string GetNextWord()
    {
        if (unsolvedWordsInCurrentEra == null || unsolvedWordsInCurrentEra.Count == 0)
        {
            Debug.Log("All words solved in this era. Returning to main menu.");
            SceneManager.LoadScene("MainMenuScene");  // Go to the main menu.
            return null; // Indicate no more words.  Important!
        }

        int randomIndex = Random.Range(0, unsolvedWordsInCurrentEra.Count);
        string nextWord = unsolvedWordsInCurrentEra[randomIndex];
        unsolvedWordsInCurrentEra.RemoveAt(randomIndex);
        return nextWord;
    }

    public void SelectEra(string eraName) // Make SelectEra public so UI can use it
    {
        CurrentEra = eraName;
        currentEraIndex = EraList.IndexOf(eraName);  // Set correct index!
        ResetUnsolvedWordsForEra(CurrentEra); // Reset when selecting an era
        Debug.Log($"Selected era: {CurrentEra}");
    }

    private void ResetUnsolvedWordsForEra(string era)
    {
        if (wordSetsWithSentences.ContainsKey(era))
        {
            unsolvedWordsInCurrentEra = new List<string>(wordSetsWithSentences[era].Keys);
            Debug.Log($"Unsolved words reset for {era}.  Count: {unsolvedWordsInCurrentEra.Count}");
        }
        else
        {
            Debug.LogError($"Era {era} not found in word sets!");
            unsolvedWordsInCurrentEra = new List<string>(); // Or handle the error differently
        }
    }

    public Sprite getEraImage(string era)
    {
        if (era.Equals("Ancient Egypt"))
        {
            return eraImages[0];
        }
        else if (era.Equals("Medieval Europe"))
        {
            return eraImages[1];
        }
        else if (era.Equals("Ancient Rome"))
        {
            return eraImages[2];
        }
        else if (era.Equals("Renaissance"))
        {
            return eraImages[3];
        }
        else if (era.Equals("Industrial Revolution"))
        {
            return eraImages[4];
        }
        else if (era.Equals("Ancient Greece"))
        {
            return eraImages[5];
        }
        return null;
    }
}
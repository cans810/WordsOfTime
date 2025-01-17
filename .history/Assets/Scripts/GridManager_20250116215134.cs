using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 5;
    [SerializeField] private float cellSize = 150f;
    [SerializeField] private float spacing = 10f;

    [Header("References")]
    [SerializeField] public GameObject letterTilePrefab;
    [SerializeField] public RectTransform gridContainer;

    private LetterTile[,] grid;
    private Vector2 startPosition;
    private List<LetterTile> selectedTiles = new List<LetterTile>();

    private string targetWord; 
    private List<char> lettersToPlace; 

    public WordGameManager WordGameManager;

    public static GridManager Instance { get; private set; }
    public bool IsSelecting { get; private set; }

    [Header("Selection Line Settings")]
    [SerializeField] private LineRenderer lineRendererPrefab;
    private LineRenderer lineRendererInstance;

    private Dictionary<string, List<Vector2Int>> solvedWordPositions = new Dictionary<string, List<Vector2Int>>();

    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();

    private Dictionary<string, bool> gridsGenerated = new Dictionary<string, bool>(); // Track generated grids

    private Dictionary<string, List<char>> initialGrids = new Dictionary<string, List<char>>();
    private Dictionary<string, bool> gridsGenerated = new Dictionary<string, bool>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGrid();
        //  Don't call SelectTargetWord or PopulateGrid here. 
        // These will be called when WordGameManager starts a new game/era.

        WordGameManager = GameObject.Find("WordGameManager").GetComponent<WordGameManager>();

        if (lineRendererInstance == null && lineRendererPrefab != null)
        {
            lineRendererInstance = Instantiate(lineRendererPrefab, transform);
            lineRendererInstance.positionCount = 2;
            lineRendererInstance.gameObject.SetActive(false);
        }
        else if (lineRendererPrefab == null)
        {
            Debug.LogError("LineRenderer prefab is not assigned in the Inspector!");
        }
    }

    public void SetupNewPuzzle(string word)
    {
        targetWord = word;


        if (!initialGrids.ContainsKey(targetWord))
        {
            GenerateAndStoreInitialGrid();
        }
        RestoreInitialGrid(targetWord);
    }

    private void ClearGridForSolvedWord()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                grid[x, y].SetLetter('\0', new Vector2Int(x, y)); // Clear the letter
                grid[x, y].ResetTile();       // Reset tile state (color, etc.)
            }
        }
    }

    private void RestoreInitialGrid(string word)
    {

        if (!initialGrids.TryGetValue(word, out var initialLetters))
        {
            Debug.LogError($"Initial grid for '{word}' not found.");
            return;
        }

        int index = 0;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {

                grid[x, y].SetLetter(initialLetters[index], new Vector2Int(x, y));

                grid[x, y].isSolved = WordGameManager.Instance.solvedWordsInCurrentEra.Contains(WordGameManager.Instance.currentEraWords.IndexOf(word));



                if (grid[x, y].isSolved)
                {
                    grid[x, y].SetSolvedColor();
                }
                else
                {
                    grid[x, y].ResetTile();
                }

                index++;
            }
        }
    }

    private List<Vector2Int> SortPositionsByWord(List<Vector2Int> positions, string word)
    {
        List<Vector2Int> sortedPositions = new List<Vector2Int>(positions);

        sortedPositions.Sort((a, b) =>
        {
            int indexA = word.IndexOf(grid[a.x, a.y].Letter);
            int indexB = word.IndexOf(grid[b.x, b.y].Letter);


            if (indexA != -1 && indexB != -1)
            {
                return indexA.CompareTo(indexB);
            }
            else if (indexA != -1)
            {
                return -1;
            }
            else if (indexB != -1)
            {
                return 1;
            }
            else
            {
                return 0;
            }


        });
        return sortedPositions;
    }

    private void GenerateAndStoreInitialGrid()
    {
        if (gridsGenerated.ContainsKey(targetWord)) return;


        ClearLetters();
        PlaceWordAdjacent();
        FillRemainingSpaces();

        List<char> initialLetters = new List<char>();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                initialLetters.Add(grid[x, y].Letter);
            }
        }

        if (initialGrids.ContainsKey(targetWord))
        {
            initialGrids[targetWord] = initialLetters; // Update existing entry
        }
        else
        {
            initialGrids.Add(targetWord, initialLetters);
        }


        List<Vector2Int> solvedPositions = new List<Vector2Int>();
        for (int i = 0; i < targetWord.Length; i++)
        {
            char letter = targetWord[i];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    if (grid[x, y].Letter == letter)
                    {
                        solvedPositions.Add(new Vector2Int(x, y));
                        //No need to break or goto here, as we're storing all positions.
                    }
                }
            }

        }

        if (solvedWordPositions.ContainsKey(targetWord))
        {
            solvedWordPositions[targetWord] = solvedPositions;
        }
        else
        {
            solvedWordPositions.Add(targetWord, solvedPositions);
        }

        gridsGenerated[targetWord] = true; // Mark grid as generated
    }

    public void ClearGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                grid[x, y].SetLetter('\0', new Vector2Int(x, y)); // Clear the letter
                grid[x, y].ResetTile();       // Reset tile state (color, etc.)
            }
        }
    }

    private void RestoreSolvedWord(string word)
    {
        if (solvedWordPositions.TryGetValue(word, out List<Vector2Int> positions))
        {
            // IMPORTANT: Sort the positions based on their index in the word
            positions.Sort((a, b) => {
                int indexA = -1;
                int indexB = -1;

                for (int x = 0; x < gridSize; x++)
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        if (new Vector2Int(x, y) == a) indexA = word.IndexOf(grid[x,y].Letter);
                        if (new Vector2Int(x, y) == b) indexB = word.IndexOf(grid[x,y].Letter);
                    }
                }

                return indexA.CompareTo(indexB);
            });


            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int pos = positions[i];
                grid[pos.x, pos.y].SetLetter(word[i], pos, word); // Set the correct letter from the word
                grid[pos.x, pos.y].SetSolvedColor();
            }
        }
        else
        {
            Debug.LogError($"Solved word '{word}' not found in the dictionary!");
        }
    }

    private void HighlightSolvedWord()
    {
        List<LetterTile> wordTiles = new List<LetterTile>();

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] != null && grid[x, y].Letter != '\0' && targetWord.Contains(grid[x, y].Letter))
                {
                    wordTiles.Add(grid[x, y]);
                }
            }
        }

        foreach (var tile in wordTiles)
        {
            tile.SetSolvedColor();
        }
    }

    private void InitializeGrid()
    {
        grid = new LetterTile[gridSize, gridSize];
        float totalWidth = (gridSize * cellSize) + ((gridSize - 1) * spacing);
        float totalHeight = totalWidth;

        gridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);

        startPosition = new Vector2(
            -(totalWidth / 2) + (cellSize / 2),
            (totalHeight / 2) - (cellSize / 2)
        );

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                CreateTile(new Vector2Int(x, y));
                Debug.Log($"Tile created at ({x}, {y}): {grid[x, y]}"); // Check for null
            }
        }
    }

    public string SelectTargetWord()
    {
        targetWord = GameManager.Instance.GetNextWord();

        if (targetWord == null)
        {
            return null; // Important: Propagate the null back.
        }

        string sentence = WordValidator.GetSentenceForWord(targetWord, GameManager.Instance.CurrentEra);

        ResetGridForNewWord(); // Reset grid before setting up WordGameManager

        WordGameManager.SetupGame(targetWord, sentence); // Set up WordGameManager with the new word and sentence. 
        lettersToPlace = new List<char>(targetWord.ToCharArray());

        return targetWord;
    }

    private void PopulateGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] == null)
                {
                    CreateTile(new Vector2Int(x, y));
                }
            }
        }

        PlaceWordAdjacent();
        FillRemainingSpaces();
    }

    private void CreateTile(Vector2Int gridPos)
    {
        Vector2 position = new Vector2(
            startPosition.x + (gridPos.x * (cellSize + spacing)),
            startPosition.y - (gridPos.y * (cellSize + spacing))
        );

        GameObject tileObj = Instantiate(letterTilePrefab, gridContainer);
        RectTransform rectTransform = tileObj.GetComponent<RectTransform>();

        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(cellSize, cellSize);

        LetterTile tile = tileObj.GetComponent<LetterTile>();
        grid[gridPos.x, gridPos.y] = tile;
    }

    private void PlaceWordAdjacent()
    {
        List<Vector2Int> potentialStartPositions = new List<Vector2Int>();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                potentialStartPositions.Add(new Vector2Int(x, y));
            }
        }

        Shuffle(potentialStartPositions); // Shuffle for randomness

        bool wordPlaced = false;
        foreach (Vector2Int startPos in potentialStartPositions)
        {
            if (TryPlaceWord(startPos))
            {
                wordPlaced = true;

                // Store solved word positions:
                if (solvedWordPositions.ContainsKey(targetWord))
                {
                    solvedWordPositions[targetWord].Clear(); // Clear old positions if word was reset
                }
                else
                {
                    solvedWordPositions.Add(targetWord, new List<Vector2Int>());
                }


                List<Vector2Int> currentWordPositions = new List<Vector2Int>();
                for (int i = 0; i < targetWord.Length; i++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        for (int y = 0; y < gridSize; y++)
                        {
                            if (grid[x, y].Letter == targetWord[i])
                            {
                                currentWordPositions.Add(new Vector2Int(x, y));
                                break; // Letter found, move to next letter in word
                            }
                        }
                    }
                }
                solvedWordPositions[targetWord] = currentWordPositions;



                break; // Exit loop once word is placed
            }
        }

        if (!wordPlaced)
        {
            Debug.LogError("Failed to place word! Consider increasing grid size or using shorter words.");
            // Here you might want to implement some fallback logic
            // like regenerating the grid or skipping the word.
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private bool TryPlaceWord(Vector2Int startPos)
    {
        if (string.IsNullOrEmpty(targetWord))
        {
            Debug.LogError("Target word is null or empty!");
            return false;
        }

        Vector2Int currentPos = startPos;


        for (int i = 0; i < targetWord.Length; i++) // Iterate through each letter of the target word
        {
            //Check bounds and if the current grid position is available
            if (currentPos.x < 0 || currentPos.x >= gridSize || currentPos.y < 0 || currentPos.y >= gridSize || grid[currentPos.x, currentPos.y].Letter != '\0')
            {

                //Placement failed. Clear placed letters and return false.

                return false;
            }

            grid[currentPos.x, currentPos.y].SetLetter(targetWord[i], currentPos);


            if (i < targetWord.Length - 1)
            {

                List<Vector2Int> validPositions = GetValidAdjacentPositions(currentPos);

                if (validPositions.Count == 0)
                {

                    return false;
                }

                currentPos = validPositions[UnityEngine.Random.Range(0, validPositions.Count)];

            }


        }

        return true;

    }

    private void FillRemainingSpaces()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].Letter == '\0') // Only fill empty tiles
                {
                    char randomLetter = (char)UnityEngine.Random.Range('A', 'Z' + 1);
                    grid[x, y].SetLetter(randomLetter, new Vector2Int(x, y));
                }
            }
        }
    }


    // Drag-based Selection Logic
    public void StartWordSelection(LetterTile tile)
    {
        IsSelecting = true;
        ClearSelection();
        AddToSelection(tile);

        // Initialize the LineRenderer
        lineRendererInstance.gameObject.SetActive(true);
        lineRendererInstance.positionCount = 1;
        lineRendererInstance.SetPosition(0, tile.transform.position);
    }


    public void AddToSelection(LetterTile tile)
    {
        if (selectedTiles.Count == 0 || IsAdjacent(selectedTiles[selectedTiles.Count - 1], tile))
        {
            if (!selectedTiles.Contains(tile))
            {
                selectedTiles.Add(tile);
                tile.SetSelected(true);
                UpdateLineRenderer(tile.transform.position);
                WordGameManager.UpdateCurrentWord(GetCurrentWord());
            }
        }
    }

    public void EndWordSelection()
    {
        if (IsSelecting)
        {
            IsSelecting = false;
            SubmitWord();

            // Disable the LineRenderer and reset its positions
            lineRendererInstance.gameObject.SetActive(false);
            lineRendererInstance.positionCount = 0;
        }
    }

    void UpdateLineRenderer(Vector3 position)
    {
    if (lineRendererInstance != null)
    {
    // Set positions for each selected tile
    lineRendererInstance.positionCount = selectedTiles.Count;
    for (int i = 0; i < selectedTiles.Count; i++)
    {
    lineRendererInstance.SetPosition(i, selectedTiles[i].transform.position);
    }
    }
    }

    private string GetCurrentWord()
    {
        return string.Join("", selectedTiles.ConvertAll(tile => tile.Letter.ToString()));
    }

    private void SubmitWord()
    {
        string currentWord = GetCurrentWord();
        if (currentWord.Equals(targetWord, StringComparison.OrdinalIgnoreCase))
        {
            // Mark the word as solved in WordGameManager FIRST.
            WordGameManager.Instance.solvedWordsInCurrentEra.Add(WordGameManager.Instance.currentWordIndex);

            foreach (var tile in selectedTiles)
            {
                tile.SetSolvedColor();
                tile.isSolved = true; 
            }


            WordGameManager.HandleCorrectWord();  // Then handle the correct word logic
        }
        else
        {
            WordGameManager.HandleIncorrectWord();
        }

        ClearSelection();
    }


    private void ClearSelection()
    {
        foreach (var tile in selectedTiles)
        {
            tile.SetSelected(false);
        }

        selectedTiles.Clear();
    }

    private bool IsAdjacent(LetterTile tile1, LetterTile tile2)
    {
        Vector2Int pos1 = tile1.GetGridPosition();
        Vector2Int pos2 = tile2.GetGridPosition();

        return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y) == 1;
    }

    private List<Vector2Int> GetValidAdjacentPositions(Vector2Int currentPos)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.up };

        foreach (var dir in directions)
        {
            Vector2Int newPos = currentPos + dir;
            if (newPos.x >= 0 && newPos.x < gridSize &&
                newPos.y >= 0 && newPos.y < gridSize &&
                grid[newPos.x, newPos.y].Letter == '\0')
            {
                validPositions.Add(newPos);
            }
        }

        return validPositions;
    }

    public void ResetGridForNewWord()
    {
        Debug.Log("Resetting grid for new word...");

        try
        {           
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    grid[x, y].ResetTile(); //  Just reset the tile state (selection, etc.)
                }
            }

            selectedTiles.Clear();
            WordGameManager.Instance.ClearCurrentWord(); 

            // NO CALL TO SetupNewPuzzle HERE!

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ResetGridForNewWord: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ClearLetters() {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].Letter != '\0')
                {
                    grid[x, y].SetLetter('\0', new Vector2Int(x, y));
                }
            }
        }
    }

    private bool IsPartOfTargetWord(Vector2Int pos)
    {
        // Check if this position has adjacent letters that form part of the target word
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.up };
        
        foreach (var dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (IsValidPosition(newPos) && 
                grid[newPos.x, newPos.y].Letter != '\0' && 
                targetWord.Contains(grid[newPos.x, newPos.y].Letter))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
    }
}
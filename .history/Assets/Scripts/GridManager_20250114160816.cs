using System.Collections.Generic;
using UnityEngine;

[Header("Grid Settings")]
    [SerializeField] private int gridSize = 5;
    [SerializeField] private float cellSize = 150f;
    [SerializeField] private float spacing = 10f;

    [Header("References")]
    [SerializeField] private GameObject letterTilePrefab;
    [SerializeField] private RectTransform gridContainer;

    private LetterTile[,] grid;
    private Vector2 startPosition;
    private List<LetterTile> selectedTiles = new List<LetterTile>();

    private string targetWord; // The word the player must guess
    private List<char> lettersToPlace; // Letters from the target word

    public WordGameManager WordGameManager;

    public static GridManager Instance { get; private set; }
    public bool IsSelecting { get; private set; }

    private List<WordSet> wordSets; // List to hold the word sets for each era
    private int currentEraIndex = 0; // Keeps track of current word progression in the selected era
    private int currentWordIndex = 0; // Keeps track of the current word in the selected era

    [Header("Selection Line Settings")]
    [SerializeField] private LineRenderer lineRendererPrefab;
    private LineRenderer lineRendererInstance;
    private Vector3 lineStartPoint;

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
        LoadWordSetsFromJSON();
        SelectTargetWord();
        InitializeGrid();
        PopulateGrid();
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

    // Load word sets from the JSON file
    private void LoadWordSetsFromJSON()
    {
        string json = File.ReadAllText("path_to_your_json_file.json");
        wordSets = JsonUtility.FromJson<WordSetsWrapper>(json).sets;
    }

    private void SelectTargetWord()
    {
        // Get the selected era from the GameManager or elsewhere
        string selectedEra = GameManager.Instance.EraSelected;
        WordSet selectedEraSet = wordSets.Find(set => set.era == selectedEra);

        if (selectedEraSet == null || selectedEraSet.words.Count == 0)
        {
            Debug.LogError($"No words found for the selected era: {selectedEra}");
            return;
        }

        // Get the next word in the progression
        targetWord = selectedEraSet.words[currentWordIndex].word.ToUpper();
        string sentence = selectedEraSet.words[currentWordIndex].sentences[0]; // You can randomize sentence if you want

        WordGameManager.SetupGame(targetWord, sentence);
        lettersToPlace = new List<char>(targetWord.ToCharArray());
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
        Vector2Int currentPos = GetRandomEmptyPosition();
        if (currentPos == Vector2Int.one * -1)
        {
            Debug.LogError("Failed to find a valid starting position for the target word.");
            return;
        }

        grid[currentPos.x, currentPos.y].SetLetter(targetWord[0], currentPos);

        for (int i = 1; i < targetWord.Length; i++)
        {
            List<Vector2Int> validPositions = GetValidAdjacentPositions(currentPos);
            if (validPositions.Count == 0)
            {
                Debug.LogError($"No valid positions for letter '{targetWord[i]}' at index {i}. Aborting word placement.");
                return;
            }

            currentPos = validPositions[Random.Range(0, validPositions.Count)];
            grid[currentPos.x, currentPos.y].SetLetter(targetWord[i], currentPos);
        }
    }

    private void FillRemainingSpaces()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].Letter == '\0') // Only fill empty tiles
                {
                    char randomLetter = (char)Random.Range('A', 'Z' + 1);
                    grid[x, y].SetLetter(randomLetter, new Vector2Int(x, y));
                }
            }
        }
    }

    // Proceed to the next word after the current one is guessed correctly
    public void NextWord()
    {
        currentWordIndex++;
        if (currentWordIndex >= wordSets[currentEraIndex].words.Count)
        {
            Debug.Log("All words in this era have been completed.");
            // Transition to the next era or finish the game.
        }
        else
        {
            SelectTargetWord(); // Load the next word
            PopulateGrid(); // Refresh the grid with the new word
        }
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
        if (currentWord.Equals(targetWord, System.StringComparison.OrdinalIgnoreCase))
        {
            WordGameManager.HandleCorrectWord();
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

    private Vector2Int GetRandomEmptyPosition()
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].Letter == '\0') emptyPositions.Add(new Vector2Int(x, y));
            }
        }

        if (emptyPositions.Count == 0)
        {
            Debug.LogWarning("No empty positions available for starting the word placement.");
            return Vector2Int.one * -1;
        }

        return emptyPositions[Random.Range(0, emptyPositions.Count)];
    }

}
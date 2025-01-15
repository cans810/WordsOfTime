using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
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

    private string targetWord; 
    private List<char> lettersToPlace; 

    public WordGameManager WordGameManager;

    public static GridManager Instance { get; private set; }
    public bool IsSelecting { get; private set; }

    [Header("Selection Line Settings")]
    [SerializeField] private LineRenderer lineRendererPrefab;
    private LineRenderer lineRendererInstance;


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
        SelectTargetWord();
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

        // Shuffle the list for truly random starting positions
        Shuffle(potentialStartPositions);

        int maxAttempts = 50; // Or adjust as needed
        bool wordPlaced = false;


        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Get a random empty position or use shuffled positions
            Vector2Int startPos = potentialStartPositions[attempt % potentialStartPositions.Count]; //Fixes the issue when attempts exceed available start positions


            if (TryPlaceWord(startPos))
            {
                wordPlaced = true;
                break;
            }

            ClearLetters(); // Clear placed letters before next attempt
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
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private bool TryPlaceWord(Vector2Int startPos)
    {
        Vector2Int currentPos = startPos;
        grid[currentPos.x, currentPos.y].SetLetter(targetWord[0], currentPos);

        for (int i = 1; i < targetWord.Length; i++)
        {
            List<Vector2Int> validPositions = GetValidAdjacentPositions(currentPos);
            if (validPositions.Count == 0)
            {
                // Backtrack: Clear placed letters and try a different path
                for (int j = i-1; j >=0; j--) { // Fixed backtracking loop condition
                        grid[currentPos.x, currentPos.y].SetLetter('\0', currentPos);
                        validPositions = GetValidAdjacentPositions(currentPos); //Update positions for going back a letter
                        if (validPositions.Count > 0) {

                        currentPos = validPositions[0]; //Fixed position for backtracking, now it goes back one by one
                        }

                }
                return false; // No valid adjacent positions
            }

            currentPos = validPositions[Random.Range(0, validPositions.Count)];
            grid[currentPos.x, currentPos.y].SetLetter(targetWord[i], currentPos);
        }

        return true; // Word successfully placed
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

    public void ResetGridForNewWord()
    {
        // 1. Clear existing letters and reset tiles:
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                grid[x, y].SetLetter('\0', new Vector2Int(x,y)); // Clear letter
                grid[x, y].SetSelected(false); // Ensure deselected
            }
        }

        selectedTiles.Clear(); // Important: Clear the selection list

        // 2. Place the new word and fill remaining spaces:
        PlaceWordAdjacent();
        FillRemainingSpaces();

        WordGameManager.Instance.ClearCurrentWord(); // Reset displayed word in WordGameManager
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

}
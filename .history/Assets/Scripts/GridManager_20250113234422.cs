using System.Collections.Generic;
using System.Linq;
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

    [SerializeField] private KeyCode submitKey = KeyCode.Return;

    private string targetWord; // The word the player must guess
    private List<char> lettersToPlace; // Letters from the target word

    public WordGameManager WordGameManager;

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

    public void StartWordSelection(LetterTile tile)
    {
        IsSelecting = true;
        ClearSelection();
        AddToSelection(tile);
    }

    public void AddToSelection(LetterTile tile)
    {
        // Only add if the tile is adjacent to the last selected tile
        if (selectedTiles.Count == 0 || IsAdjacent(selectedTiles[selectedTiles.Count - 1], tile))
        {
            // Don't add if already selected
            if (!selectedTiles.Contains(tile))
            {
                selectedTiles.Add(tile);
                tile.SetSelected(true);
                
                string currentWord = GetCurrentWord();
                WordGameManager.UpdateCurrentWord(currentWord);
                Debug.Log($"Current word: {currentWord}");
            }
        }
    }

    public void EndWordSelection()
    {
        if (IsSelecting)
        {
            IsSelecting = false;
            SubmitWord();
        }
    }

    private bool IsAdjacent(LetterTile tile1, LetterTile tile2)
    {
        Vector2Int pos1 = tile1.GetGridPosition();
        Vector2Int pos2 = tile2.GetGridPosition();
        
        int xDiff = Mathf.Abs(pos1.x - pos2.x);
        int yDiff = Mathf.Abs(pos1.y - pos2.y);
        
        // Adjacent if the tiles are next to each other horizontally, vertically
        return (xDiff == 1 && yDiff == 0) || (xDiff == 0 && yDiff == 1);
    }

    private void ClearSelection()
    {
        foreach (var tile in selectedTiles)
        {
            tile.SetSelected(false);
        }
        selectedTiles.Clear();
    }

    private string GetCurrentWord()
    {
        return string.Join("", selectedTiles.ConvertAll(t => t.Letter.ToString()));
    }

    private void SubmitWord()
    {
        string currentWord = GetCurrentWord();
        
        if (currentWord.Equals(targetWord, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Correct word found!");
            // Handle success
            WordGameManager.HandleCorrectWord();
        }
        else
        {
            Debug.Log("Incorrect word");
            // Handle failure
            WordGameManager.HandleIncorrectWord();
        }
        
        ClearSelection();
    }

    private void Start()
    {
        InitializeGrid();
        SelectTargetWord();
        PopulateGrid();
        LetterTile.OnTileSelected += HandleTileSelected;

        Debug.Log($"Target Word: {targetWord}");

        WordGameManager = GameObject.Find("WordGameManager").GetComponent<WordGameManager>();
    }


    private void Update()
    {
        if (Input.GetKeyDown(submitKey))
        {
            SubmitWord();
        }
    }

    private void OnDestroy()
    {
        LetterTile.OnTileSelected -= HandleTileSelected;
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
    }

    private void PopulateGrid()
    {
        // First, create all tiles
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

        // Place the word with flexible adjacent placement
        PlaceWordAdjacent();

        // Fill remaining spaces
        FillRemainingSpaces();
    }

    private List<Vector2Int> GetValidAdjacentPositions(Vector2Int currentPos)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // right
            new Vector2Int(0, -1),  // down
            new Vector2Int(0, 1),   // up
            new Vector2Int(-1, 0),  // left
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = currentPos + dir;
            // Check if position is within grid bounds and tile is empty
            if (newPos.x >= 0 && newPos.x < gridSize && 
                newPos.y >= 0 && newPos.y < gridSize && 
                grid[newPos.x, newPos.y].Letter == '\0')
            {
                validPositions.Add(newPos);
            }
        }

        return validPositions;
    }

    private void PlaceWordAdjacent()
    {
        // Start from middle of the grid
        Vector2Int currentPos = new Vector2Int(gridSize / 2, gridSize / 2);
        
        // Place first letter
        grid[currentPos.x, currentPos.y].SetLetter(targetWord[0], currentPos);
        Debug.Log($"Placed {targetWord[0]} at ({currentPos.x}, {currentPos.y})");

        // Place remaining letters
        for (int i = 1; i < targetWord.Length; i++)
        {
            List<Vector2Int> validPositions = GetValidAdjacentPositions(currentPos);
            
            if (validPositions.Count == 0)
            {
                Debug.LogError($"No valid positions found for letter {targetWord[i]}");
                return;
            }

            // Choose first valid position (you could randomize this choice if desired)
            currentPos = validPositions[0];
            grid[currentPos.x, currentPos.y].SetLetter(targetWord[i], currentPos);
            Debug.Log($"Placed {targetWord[i]} at ({currentPos.x}, {currentPos.y})");
        }
    }

    private void FillRemainingSpaces()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                LetterTile tile = grid[x, y];
                if (tile.Letter == '\0')
                {
                    char randomLetter = (char)Random.Range('A', 'Z' + 1);
                    tile.SetLetter(randomLetter, new Vector2Int(x, y));
                }
            }
        }
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
        grid[gridPos.x, gridPos.y] = tile;  // Ensure the tile is placed in the correct grid position

        // Log to confirm tile creation
        Debug.Log($"Tile created at {gridPos.x}, {gridPos.y}");
    }

    private void HandleTileSelected(LetterTile tile)
    {
        if (selectedTiles.Contains(tile))
        {
            selectedTiles.Remove(tile);
            tile.SetSelected(false);
        }
        else
        {
            selectedTiles.Add(tile);
            tile.SetSelected(true);
        }

        string currentWord = string.Join("", selectedTiles.ConvertAll(t => t.Letter.ToString()));
        Debug.Log("Current Word: " + currentWord);
        WordGameManager.UpdateCurrentWord(currentWord);
    }

    private void SelectTargetWord()
    {
        string era = GameManager.Instance.EraSelected;
        List<string> words = WordValidator.GetWordsForEra(era);

        if (words.Count == 0)
        {
            Debug.LogError($"No words found for the selected era: {era}");
            return;
        }

        // Select random word and get its sentence
        targetWord = words[Random.Range(0, words.Count)].ToUpper();
        string sentence = WordValidator.GetSentenceForWord(targetWord, era);
        
        // Initialize the word game manager with the word and sentence
        WordGameManager.Instance.SetupGame(targetWord, sentence);
        
        // Convert word to char array for grid population
        lettersToPlace = new List<char>(targetWord.ToCharArray());
        
        Debug.Log($"Target Word: {targetWord}");
    }


    private void SubmitWord()
    {
        string currentWord = string.Join("", selectedTiles.ConvertAll(t => t.Letter.ToString()));

        if (currentWord.Equals(targetWord, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"Correct! The word '{currentWord}' matches the target word '{targetWord}'.");
            // Handle success logic, e.g., progress to the next round
        }
        else
        {
            Debug.Log($"Incorrect! The word '{currentWord}' does not match the target word '{targetWord}'.");
            // Handle failure logic, e.g., clear selected tiles
        }

        // Clear the selected tiles regardless of correctness
        foreach (var tile in selectedTiles)
        {
            tile.SetSelected(false);
        }

        selectedTiles.Clear();
    }
}

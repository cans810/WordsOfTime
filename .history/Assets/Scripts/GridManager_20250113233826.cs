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

        // Place the word starting from top-left for simplicity and debugging
        PlaceWordInGrid();

        // Fill remaining positions with random letters
        FillRemainingSpaces();
        
        Debug.Log($"Grid population completed. Target word: {targetWord}");
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

        // Place the word in a snake pattern
        PlaceWordInSnakePattern();

        // Fill remaining spaces
        FillRemainingSpaces();
    }

    private void PlaceWordInSnakePattern()
    {
        // Start from middle-left of the grid for better visibility
        int currentX = 0;
        int currentY = gridSize / 2;
        bool goingRight = true;
        bool hitEdge = false;

        for (int i = 0; i < targetWord.Length; i++)
        {
            // Place current letter
            grid[currentX, currentY].SetLetter(targetWord[i], new Vector2Int(currentX, currentY));
            Debug.Log($"Placed {targetWord[i]} at ({currentX}, {currentY})");

            // If we're not at the last letter, determine next position
            if (i < targetWord.Length - 1)
            {
                if (goingRight)
                {
                    // If we can move right, do so
                    if (currentX + 1 < gridSize && !hitEdge)
                    {
                        currentX++;
                    }
                    else
                    {
                        // We hit the right edge, move up and start going left
                        hitEdge = true;
                        currentY--;
                        goingRight = false;
                    }
                }
                else
                {
                    // If we can move left, do so
                    if (currentX - 1 >= 0 && !hitEdge)
                    {
                        currentX--;
                    }
                    else
                    {
                        // We hit the left edge, move up and start going right
                        hitEdge = true;
                        currentY--;
                        goingRight = true;
                    }
                }
            }
        }
    }

    private void FillRemainingSpaces()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                LetterTile tile = grid[x, y];
                if (tile.Letter == '\0' || char.IsWhiteSpace(tile.Letter))
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

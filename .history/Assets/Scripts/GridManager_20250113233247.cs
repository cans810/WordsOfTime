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

        // Choose a random starting position for the word
        int startX = Random.Range(0, gridSize);
        int startY = Random.Range(0, gridSize);
        
        // Try to place horizontally first, if not possible, place vertically
        bool placedHorizontally = TryPlaceWordHorizontally(startX, startY);
        if (!placedHorizontally)
        {
            TryPlaceWordVertically(startX, startY);
        }

        // Fill remaining positions with random letters
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].Letter == '\0') // If position is empty
                {
                    char randomLetter = (char)Random.Range('A', 'Z' + 1);
                    grid[x, y].SetLetter(randomLetter, new Vector2Int(x, y));
                }
            }
        }
    }

    private bool TryPlaceWordHorizontally(int startX, int startY)
    {
        // Check if word fits horizontally
        if (startX + targetWord.Length > gridSize)
        {
            // If word doesn't fit starting at startX, try to adjust the starting position
            startX = gridSize - targetWord.Length;
            if (startX < 0) return false; // Word is too long to fit horizontally
        }

        // Place the word horizontally
        for (int i = 0; i < targetWord.Length; i++)
        {
            grid[startX + i, startY].SetLetter(targetWord[i], new Vector2Int(startX + i, startY));
        }
        return true;
    }

    private bool TryPlaceWordVertically(int startX, int startY)
    {
        // Check if word fits vertically
        if (startY + targetWord.Length > gridSize)
        {
            // If word doesn't fit starting at startY, try to adjust the starting position
            startY = gridSize - targetWord.Length;
            if (startY < 0) return false; // Word is too long to fit vertically
        }

        // Place the word vertically
        for (int i = 0; i < targetWord.Length; i++)
        {
            grid[startX, startY + i].SetLetter(targetWord[i], new Vector2Int(startX, startY + i));
        }
        return true;
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

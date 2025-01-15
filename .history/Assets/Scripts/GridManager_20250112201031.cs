using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    private string targetWord; // The word to guess
    private List<char> lettersToPlace; // Letters from the target word

    private void Start()
    {
        InitializeGrid();
        SelectTargetWord();
        PopulateGrid();
        LetterTile.OnTileSelected += HandleTileSelected;
    }

    private void Update()
    {
        if (Input.GetKeyDown(submitKey))
        {
            if (selectedTiles.Count >= 3)
            {
                Debug.Log("Submitting word...");
                WordGameManager.Instance.ValidateWord(selectedTiles);
                selectedTiles.Clear(); // Clear the selection after submitting
            }
            else
            {
                Debug.Log("Need at least 3 letters!");
            }
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

    private void SelectTargetWord()
    {
        string era = GameManager.Instance.EraSelected; // Get the selected era
        Debug.Log(era)
        List<string> words = WordValidator.GetWordsForEra(era); // Get word list for the era
        targetWord = words[Random.Range(0, words.Count)].ToUpper(); // Pick a random word
        lettersToPlace = new List<char>(targetWord.ToCharArray()); // Convert the word into a char list
        Debug.Log($"Target Word: {targetWord}");
    }

    private void PopulateGrid()
    {
        List<Vector2Int> availablePositions = new List<Vector2Int>();

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                availablePositions.Add(new Vector2Int(x, y));
            }
        }

        // Place target word letters randomly in the grid
        foreach (char letter in lettersToPlace)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2Int pos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);

            CreateTile(pos, letter);
        }

        // Fill remaining tiles with random letters
        foreach (Vector2Int pos in availablePositions)
        {
            char randomLetter = (char)Random.Range('A', 'Z' + 1);
            CreateTile(pos, randomLetter);
        }
    }

    private void CreateTile(Vector2Int gridPos, char letter)
    {
        // Calculate world position
        Vector2 position = new Vector2(
            startPosition.x + (gridPos.x * (cellSize + spacing)),
            startPosition.y - (gridPos.y * (cellSize + spacing))
        );

        // Instantiate tile
        GameObject tileObj = Instantiate(letterTilePrefab, gridContainer);
        RectTransform rectTransform = tileObj.GetComponent<RectTransform>();

        // Set position and size
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(cellSize, cellSize);

        // Get and setup LetterTile component
        LetterTile tile = tileObj.GetComponent<LetterTile>();
        grid[gridPos.x, gridPos.y] = tile;

        tile.SetLetter(letter, gridPos);
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

        // Update the displayed word every time selection changes
        string currentWord = string.Join("", selectedTiles.ConvertAll(t => t.Letter.ToString()));
        WordGameManager.Instance.UpdateCurrentWord(currentWord);
        Debug.Log("Current Word: " + currentWord);
    }
}

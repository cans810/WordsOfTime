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

    [Header("Game Settings")]
    [SerializeField] private string targetWord = "UNITY"; // Word to guess

    private LetterTile[,] grid;
    private Vector2 startPosition;
    private List<LetterTile> selectedTiles = new List<LetterTile>();

    [SerializeField] private KeyCode submitKey = KeyCode.Return;

    private void Start()
    {
        InitializeGrid();
        PopulateGrid();
        LetterTile.OnTileSelected += HandleTileSelected;
    }

    private void Update()
    {
        if (Input.GetKeyDown(submitKey))
        {
            if (selectedTiles.Count >= 3)
            {
                WordGameManager.Instance.ValidateWord(selectedTiles);
                selectedTiles.Clear(); // Clear the selection after submitting
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

    private void PopulateGrid()
    {
        // Shuffle positions for the target word
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                availablePositions.Add(new Vector2Int(x, y));
            }
        }
        ShuffleList(availablePositions);

        // Place target word letters
        for (int i = 0; i < targetWord.Length; i++)
        {
            Vector2Int position = availablePositions[i];
            CreateTile(position, targetWord[i]);
        }

        // Fill remaining grid with random letters
        for (int i = targetWord.Length; i < availablePositions.Count; i++)
        {
            Vector2Int position = availablePositions[i];
            char randomLetter = (char)Random.Range('A', 'Z' + 1);
            CreateTile(position, randomLetter);
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

        // Assign the letter
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
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}

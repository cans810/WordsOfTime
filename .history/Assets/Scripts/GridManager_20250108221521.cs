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

    private void Start()
    {
        InitializeGrid();
        PopulateGrid();
        LetterTile.OnTileSelected += HandleTileSelected;
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
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                CreateTile(new Vector2Int(x, y));
            }
        }
    }

    private void CreateTile(Vector2Int gridPos)
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

        // Temporarily set random letter
        char randomLetter = (char)Random.Range('A', 'Z' + 1);
        tile.SetLetter(randomLetter, gridPos);
    }

    private void HandleTileSelected(LetterTile tile)
    {
        if (tile == null || tile.isSelected) return;

        if (!selectedTiles.Contains(tile))
        {
            tile.SetSelected(true);
            selectedTiles.Add(tile);
        }
        else
        {
            tile.SetSelected(false);
            selectedTiles.Remove(tile);
        }

        // Debug: Print the current word being formed
        string currentWord = string.Join("", selectedTiles.ConvertAll(t => t.Letter.ToString()));
        Debug.Log("Current Word: " + currentWord);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 6;
    [SerializeField] private float cellSize = 150f;
    [SerializeField] private float spacing = 10f;

    [Header("References")]
    [SerializeField] public GameObject letterTilePrefab;
    [SerializeField] public RectTransform gridContainer;
    [SerializeField] private LineRenderer lineRendererPrefab;
    private LineRenderer lineRendererInstance;

    private Dictionary<string, GameObject> wordGrids = new Dictionary<string, GameObject>();
    public LetterTile[,] grid;
    private string currentWord;
    private Vector2 startPosition;
    private List<LetterTile> selectedTiles = new List<LetterTile>();
    private List<LetterTile> highlightedTiles = new List<LetterTile>();
    private bool isSelecting = false;

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

    public void SetupNewPuzzle(string word)
    {
        // Deactivate current grid if it exists
        if (!string.IsNullOrEmpty(currentWord) && wordGrids.ContainsKey(currentWord))
        {
            wordGrids[currentWord].SetActive(false);
        }

        // If grid for this word already exists, activate it
        if (wordGrids.ContainsKey(word))
        {
            wordGrids[word].SetActive(true);
            grid = GetGridFromWordGrid(wordGrids[word]);
            currentWord = word;
            return;
        }

        // Create new grid if it doesn't exist
        GameObject wordGrid = new GameObject($"WordGrid_{word}");
        wordGrid.transform.SetParent(gridContainer, false);
        RectTransform wordGridRect = wordGrid.AddComponent<RectTransform>();
        wordGridRect.anchoredPosition = Vector2.zero;
        wordGridRect.sizeDelta = gridContainer.sizeDelta;

        grid = new LetterTile[gridSize, gridSize];
        List<char> gridData = GameManager.Instance.InitialGrids[word];
        if (gridData == null)
        {
            Debug.LogError($"No pre-generated grid found for word: {word}");
            return;
        }

        // Calculate start position for this grid
        float totalWidth = (gridSize * cellSize) + ((gridSize - 1) * spacing);
        float totalHeight = totalWidth;
        startPosition = new Vector2(
            -(totalWidth / 2) + (cellSize / 2),
            (totalHeight / 2) - (cellSize / 2)
        );

        // Create new grid
        for (int i = 0; i < gridData.Count; i++)
        {
            int row = i / gridSize;
            int col = i % gridSize;
            Vector2Int position = new Vector2Int(col, row);
            
            LetterTile tile = CreateTile(position, wordGrid.transform);
            if (tile != null)
            {
                tile.SetLetter(gridData[i], position);
                grid[col, row] = tile;
            }
        }

        // Store the new grid
        wordGrids[word] = wordGrid;
        currentWord = word;
    }

    private LetterTile CreateTile(Vector2Int gridPos, Transform parent)
    {
        Vector2 position = new Vector2(
            startPosition.x + (gridPos.x * (cellSize + spacing)),
            startPosition.y - (gridPos.y * (cellSize + spacing))
        );

        GameObject tileObj = Instantiate(letterTilePrefab, parent);
        RectTransform rectTransform = tileObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
        return tileObj.GetComponent<LetterTile>();
    }

    private LetterTile[,] GetGridFromWordGrid(GameObject wordGrid)
    {
        LetterTile[,] result = new LetterTile[gridSize, gridSize];
        LetterTile[] tiles = wordGrid.GetComponentsInChildren<LetterTile>();
        
        foreach (var tile in tiles)
        {
            Vector2Int pos = tile.GetGridPosition();
            result[pos.x, pos.y] = tile;
        }
        
        return result;
    }

    public void ClearAllGrids()
    {
        foreach (var wordGrid in wordGrids.Values)
        {
            Destroy(wordGrid);
        }
        wordGrids.Clear();
        grid = new LetterTile[gridSize, gridSize];
        selectedTiles.Clear();
        highlightedTiles.Clear();
        currentWord = null;
    }

    public void StartWordSelection(LetterTile tile)
    {
        if (!isSelecting)
        {
            isSelecting = true;
            selectedTiles.Clear();
            selectedTiles.Add(tile);
            tile.SetSelected(true);

            if (lineRendererInstance == null)
            {
                lineRendererInstance = Instantiate(lineRendererPrefab);
            }
            UpdateLineRenderer();
        }
    }

    public bool IsSelecting()
    {
        return isSelecting;
    }

    public void AddToSelection(LetterTile tile)
    {
        if (isSelecting && !selectedTiles.Contains(tile))
        {
            Vector2Int lastPos = selectedTiles[selectedTiles.Count - 1].GetGridPosition();
            Vector2Int newPos = tile.GetGridPosition();

            // Check if the new tile is adjacent to the last selected tile
            if (IsAdjacent(lastPos, newPos))
            {
                selectedTiles.Add(tile);
                tile.SetSelected(true);
                UpdateLineRenderer();
            }
        }
    }

    private bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        return Mathf.Abs(pos1.x - pos2.x) <= 1 && Mathf.Abs(pos1.y - pos2.y) <= 1;
    }

    public void EndWordSelection()
    {
        if (isSelecting)
        {
            string selectedWord = GetSelectedWord();
            WordGameManager.Instance.CheckWord(selectedWord, selectedTiles);
            
            isSelecting = false;
            if (lineRendererInstance != null)
            {
                lineRendererInstance.positionCount = 0;
            }
        }
    }

    private string GetSelectedWord()
    {
        string word = "";
        foreach (var tile in selectedTiles)
        {
            word += tile.GetLetter();
        }
        return word;
    }

    private void UpdateLineRenderer()
    {
        if (lineRendererInstance != null && selectedTiles.Count > 0)
        {
            lineRendererInstance.positionCount = selectedTiles.Count;
            for (int i = 0; i < selectedTiles.Count; i++)
            {
                Vector3 worldPos = selectedTiles[i].transform.position;
                worldPos.z = lineRendererInstance.transform.position.z;
                lineRendererInstance.SetPosition(i, worldPos);
            }
        }
    }

    public void ResetGridForNewWord()
    {
        foreach (var tile in selectedTiles)
        {
            tile.SetSelected(false);
        }
        selectedTiles.Clear();
        isSelecting = false;
        if (lineRendererInstance != null)
        {
            lineRendererInstance.positionCount = 0;
        }
    }

    public void HighlightFirstLetter(char letter)
    {
        highlightedTiles.Clear();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] != null && grid[x, y].GetLetter() == letter && !grid[x, y].isSolved)
                {
                    highlightedTiles.Add(grid[x, y]);
                }
            }
        }

        if (highlightedTiles.Count > 0)
        {
            StartCoroutine(HighlightTilesCoroutine());
        }
    }

    private IEnumerator HighlightTilesCoroutine()
    {
        float elapsedTime = 0f;
        Color highlightColor = new Color(0f, 0.5f, 1f, 0.5f);
        
        Dictionary<LetterTile, Color> originalColors = new Dictionary<LetterTile, Color>();
        foreach (var tile in highlightedTiles)
        {
            if (!tile.isSolved)
            {
                originalColors[tile] = tile.GetCurrentColor();
            }
        }

        while (elapsedTime < WordGameManager.HINT_HIGHLIGHT_DURATION)
        {
            float alpha = Mathf.PingPong(elapsedTime * 4f, 1f);
            
            foreach (var tile in highlightedTiles)
            {
                if (!tile.isSolved)
                {
                    Color flashColor = Color.Lerp(originalColors[tile], highlightColor, alpha);
                    tile.SetHighlightColor(flashColor);
                    tile.PreserveLetterDisplay();
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        foreach (var tile in highlightedTiles)
        {
            if (!tile.isSolved)
            {
                tile.SetHighlightColor(originalColors[tile]);
                tile.ResetHighlight();
            }
        }
        highlightedTiles.Clear();
    }

    // ... rest of your existing methods remain the same ...
}
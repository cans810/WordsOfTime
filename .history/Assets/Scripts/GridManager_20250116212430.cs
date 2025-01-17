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
        // Initialize the grid UI
        InitializeGrid();

        // Link WordGameManager
        WordGameManager = GameObject.Find("WordGameManager").GetComponent<WordGameManager>();

        // Setup line renderer
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

    // MAIN CHANGE:
    // Instead of random generation, retrieve the layout from GameManager's precomputed dictionary
    public void SetupNewPuzzle(string era, string word)
    {
        this.targetWord = word;

        // Attempt to retrieve the pre-generated layout
        if (GameManager.Instance.preGeneratedLayouts.TryGetValue(era, out var eraDict))
        {
            if (eraDict.TryGetValue(word, out var layout))
            {
                // We have the layout => restore it onto our grid
                RestoreLayoutToGrid(layout);
            }
            else
            {
                Debug.LogError($"No pre-generated layout found for word '{word}' in era '{era}'!");
            }
        }
        else
        {
            Debug.LogError($"No dictionary found for era '{era}' in preGeneratedLayouts!");
        }
    }

    private void RestoreLayoutToGrid(List<char> layout)
    {
        // Safety check
        if (layout == null || layout.Count == 0)
        {
            Debug.LogError("Layout is empty, cannot restore to grid!");
            return;
        }

        int index = 0;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                char letter = layout[index++];
                grid[x, y].SetLetter(letter, new Vector2Int(x, y));
                grid[x, y].ResetTile(); // reset visuals in case it was used before

                // If letter is part of an already solved word, we can color it as solved,
                // but in many games you'd do that logic in WordGameManager or after user solves it.
            }
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
        grid[gridPos.x, gridPos.y] = tile;
    }

    // Clears the entire grid visually and logically
    public void ClearGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                grid[x, y].SetLetter('\0', new Vector2Int(x, y));
                grid[x, y].ResetTile();
            }
        }
    }

    // =============== DRAG & SELECTION LOGIC ===============
    public void StartWordSelection(LetterTile tile)
    {
        IsSelecting = true;
        ClearSelection();
        AddToSelection(tile);

        // Initialize LineRenderer
        if (lineRendererInstance != null)
        {
            lineRendererInstance.gameObject.SetActive(true);
            lineRendererInstance.positionCount = 1;
            lineRendererInstance.SetPosition(0, tile.transform.position);
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
                UpdateLineRenderer();
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

            // Disable the LineRenderer
            lineRendererInstance.gameObject.SetActive(false);
            lineRendererInstance.positionCount = 0;
        }
    }

    private void UpdateLineRenderer()
    {
        if (lineRendererInstance != null && selectedTiles.Count > 0)
        {
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
            // Mark the word as solved
            WordGameManager.Instance.solvedWordsInCurrentEra.Add(WordGameManager.Instance.currentWordIndex);
            foreach (var tile in selectedTiles)
            {
                tile.SetSolvedColor();
                tile.isSolved = true;
            }
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
}

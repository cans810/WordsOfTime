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

    private string targetWord; // The word the player must guess
    private List<char> lettersToPlace; // Letters from the target word

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

        targetWord = words[Random.Range(0, words.Count)].ToUpper();
        string sentence = WordValidator.GetSentenceForWord(targetWord, era);

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
        if (currentPos == Vector2Int.one * -1) return;

        grid[currentPos.x, currentPos.y].SetLetter(targetWord[0], currentPos);

        Vector2Int previousPos = currentPos;
        for (int i = 1; i < targetWord.Length; i++)
        {
            List<Vector2Int> validPositions = GetValidAdjacentPositions(currentPos);
            if (validPositions.Count == 0) return;

            currentPos = validPositions[Random.Range(0, validPositions.Count)];
            grid[currentPos.x, currentPos.y].SetLetter(targetWord[i], currentPos);
            previousPos = currentPos;
        }
    }

    private void FillRemainingSpaces()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].Letter == '\0')
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

        lineRendererInstance = Instantiate(lineRendererPrefab, transform);
        lineRendererInstance.positionCount = 1;
        lineRendererInstance.SetPosition(0, tile.transform.position);
    }

    public void AddToSelection(LetterTile tile)
{
    if (!selectedTiles.Contains(tile))
    {
        selectedTiles.Add(tile);
        UpdateLineRenderer(tile.transform.position); // Use tile's position
    }
}


    public void EndWordSelection()
    {
        if (IsSelecting)
        {
            IsSelecting = false;
            SubmitWord();

            if (lineRendererInstance != null)
            {
                Destroy(lineRendererInstance.gameObject);
                lineRendererInstance = null;
            }
        }
    }

    void UpdateLineRenderer(Vector3 position)
    {
        if (lineRendererInstance != null)
        {
            // Add the position to the LineRenderer
            lineRendererInstance.positionCount++;
            lineRendererInstance.SetPosition(lineRendererInstance.positionCount - 1, position);
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

        return emptyPositions.Count > 0 ? emptyPositions[Random.Range(0, emptyPositions.Count)] : Vector2Int.one * -1;
    }
}

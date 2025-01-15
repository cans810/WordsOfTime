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
    [SerializeField] private LineRenderer linePrefab;

    private LetterTile[,] grid;
    private Vector2 startPosition;
    private List<LetterTile> selectedTiles = new List<LetterTile>();
    private List<LineRenderer> activeLines = new List<LineRenderer>();

    private string targetWord;
    private List<char> lettersToPlace;

    public WordGameManager WordGameManager { get; private set; }
    public bool IsSelecting { get; private set; }

    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        InitializeGrid();
        SelectTargetWord();
        PopulateGrid();
        Debug.Log($"Target Word: {targetWord}");
        WordGameManager = GameObject.Find("WordGameManager").GetComponent<WordGameManager>();
    }

    private void InitializeGrid()
    {
        grid = new LetterTile[gridSize, gridSize];
        float totalSize = (gridSize * cellSize) + ((gridSize - 1) * spacing);

        gridContainer.sizeDelta = new Vector2(totalSize, totalSize);
        startPosition = new Vector2(-(totalSize / 2) + (cellSize / 2), (totalSize / 2) - (cellSize / 2));
    }

    private void SelectTargetWord()
    {
        string era = GameManager.Instance.EraSelected;
        List<string> words = WordValidator.GetWordsForEra(era);

        if (words.Count == 0)
        {
            Debug.LogError($"No words found for era: {era}");
            return;
        }

        targetWord = words[Random.Range(0, words.Count)].ToUpper();
        WordGameManager.Instance.SetupGame(targetWord, WordValidator.GetSentenceForWord(targetWord, era));
        lettersToPlace = new List<char>(targetWord.ToCharArray());
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

        PlaceWordAdjacent();
        FillRemainingSpaces();
    }

    private void CreateTile(Vector2Int position)
    {
        Vector2 tilePosition = new Vector2(
            startPosition.x + (position.x * (cellSize + spacing)),
            startPosition.y - (position.y * (cellSize + spacing))
        );

        GameObject tileObj = Instantiate(letterTilePrefab, gridContainer);
        RectTransform rectTransform = tileObj.GetComponent<RectTransform>();

        rectTransform.anchoredPosition = tilePosition;
        rectTransform.sizeDelta = new Vector2(cellSize, cellSize);

        LetterTile tile = tileObj.GetComponent<LetterTile>();
        grid[position.x, position.y] = tile;
    }

    private void PlaceWordAdjacent()
    {
        ClearLines();
        Vector2Int currentPos = GetRandomEmptyPosition();

        if (currentPos == Vector2Int.one * -1)
        {
            Debug.LogError("No valid starting position for word placement.");
            return;
        }

        grid[currentPos.x, currentPos.y].SetLetter(targetWord[0], currentPos);

        Vector2Int previousPos = currentPos;

        for (int i = 1; i < targetWord.Length; i++)
        {
            List<Vector2Int> validPositions = GetValidAdjacentPositions(currentPos);

            if (validPositions.Count == 0)
            {
                Debug.LogError($"No valid position for letter {targetWord[i]}.");
                return;
            }

            currentPos = validPositions[Random.Range(0, validPositions.Count)];
            grid[currentPos.x, currentPos.y].SetLetter(targetWord[i], currentPos);

            DrawLineBetween(previousPos, currentPos);
            previousPos = currentPos;
        }
    }

    private void DrawLineBetween(Vector2Int from, Vector2Int to)
    {
        LineRenderer line = Instantiate(linePrefab, gridContainer);
        line.positionCount = 2;
        line.SetPosition(0, GridToWorldPosition(from));
        line.SetPosition(1, GridToWorldPosition(to));
        activeLines.Add(line);
    }

    private Vector3 GridToWorldPosition(Vector2Int position)
    {
        Vector3 localPosition = new Vector3(
            startPosition.x + position.x * (cellSize + spacing),
            startPosition.y - position.y * (cellSize + spacing),
            0
        );

        return gridContainer.TransformPoint(localPosition);
    }

    private void FillRemainingSpaces()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].Letter == '\0')
                {
                    grid[x, y].SetLetter((char)Random.Range('A', 'Z' + 1), new Vector2Int(x, y));
                }
            }
        }
    }

    private Vector2Int GetRandomEmptyPosition()
    {
        var emptyPositions = new List<Vector2Int>();

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].Letter == '\0')
                    emptyPositions.Add(new Vector2Int(x, y));
            }
        }

        return emptyPositions.Count > 0 ? emptyPositions[Random.Range(0, emptyPositions.Count)] : Vector2Int.one * -1;
    }

    private List<Vector2Int> GetValidAdjacentPositions(Vector2Int currentPos)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.up };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = currentPos + dir;

            if (newPos.x >= 0 && newPos.x < gridSize && newPos.y >= 0 && newPos.y < gridSize &&
                grid[newPos.x, newPos.y].Letter == '\0')
            {
                positions.Add(newPos);
            }
        }

        return positions;
    }

    private void ClearLines()
    {
        foreach (var line in activeLines)
        {
            Destroy(line.gameObject);
        }

        activeLines.Clear();
    }
}

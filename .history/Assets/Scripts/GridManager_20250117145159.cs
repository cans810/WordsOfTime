using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 6;
    [SerializeField] private float cellSize = 150f;
    [SerializeField] private float spacing = 10f;

    [Header("References")]
    [SerializeField] public GameObject letterTilePrefab;
    [SerializeField] public RectTransform gridContainer;
    [SerializeField] private LineRenderer lineRendererPrefab;
    private LineRenderer lineRendererInstance;

    public LetterTile[,] grid;
    private Vector2 startPosition;
    private List<LetterTile> selectedTiles = new List<LetterTile>();
    private string targetWord;
    public WordGameManager WordGameManager;
    public static GridManager Instance { get; private set; }
    public bool IsSelecting { get; private set; }

    private Dictionary<string, List<LetterTile>> solvedWordTiles = new Dictionary<string, List<LetterTile>>();

    private Dictionary<string, List<Vector2Int>> solvedWordPositions = new Dictionary<string, List<Vector2Int>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeGrid();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGrid();
        WordGameManager = GameObject.Find("WordGameManager").GetComponent<WordGameManager>();

        if (lineRendererInstance == null && lineRendererPrefab != null)
        {
            lineRendererInstance = Instantiate(lineRendererPrefab, transform);
            lineRendererInstance.positionCount = 2;
            lineRendererInstance.gameObject.SetActive(false);
        }

        if (WordGameManager.Instance != null && !string.IsNullOrEmpty(WordGameManager.Instance.targetWord))
        {
            SetupNewPuzzle(WordGameManager.Instance.targetWord);
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            WordGameManager = GameObject.Find("WordGameManager").GetComponent<WordGameManager>();
            
            if (WordGameManager.Instance != null && !string.IsNullOrEmpty(WordGameManager.Instance.targetWord))
            {
                SetupNewPuzzle(WordGameManager.Instance.targetWord);
            }
        }
    }

    public void SetupNewPuzzle(string word)
    {
        if (string.IsNullOrEmpty(word)) return;

        targetWord = word;
        
        if (!GameManager.Instance.InitialGrids.ContainsKey(word))
        {
            Debug.LogError($"No pre-generated grid found for word: {word}");
            return;
        }

        List<char> gridData = GameManager.Instance.InitialGrids[word];
        bool isWordSolved = WordGameManager.Instance.IsWordSolved(word);
        
        int index = 0;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y].SetLetter(gridData[index], new Vector2Int(x, y));
                    index++;
                }
            }
        }

        // Restore solved state if the word was previously solved
        if (isWordSolved && solvedWordPositions.ContainsKey(word))
        {
            foreach (Vector2Int pos in solvedWordPositions[word])
            {
                grid[pos.x, pos.y].SetSolvedColor();
                grid[pos.x, pos.y].isSolved = true;
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
        grid[gridPos.x, gridPos.y] = tileObj.GetComponent<LetterTile>();
    }

    public void StartWordSelection(LetterTile tile)
    {
        IsSelecting = true;
        ClearSelection();
        AddToSelection(tile);
        lineRendererInstance.gameObject.SetActive(true);
        lineRendererInstance.positionCount = 1;
        lineRendererInstance.SetPosition(0, tile.transform.position);
    }

    public void AddToSelection(LetterTile tile)
    {
        if (selectedTiles.Count == 0 || IsAdjacent(selectedTiles[selectedTiles.Count - 1], tile))
        {
            if (!selectedTiles.Contains(tile))
            {
                selectedTiles.Add(tile);
                tile.SetSelected(true);
                UpdateLineRenderer(tile.transform.position);
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
            lineRendererInstance.gameObject.SetActive(false);
            lineRendererInstance.positionCount = 0;
        }
    }

    private void UpdateLineRenderer(Vector3 position)
    {
        if (lineRendererInstance != null)
        {
            lineRendererInstance.positionCount = selectedTiles.Count;
            for (int i = 0; i < selectedTiles.Count; i++)
            {
                lineRendererInstance.SetPosition(i, selectedTiles[i].transform.position);
            }
        }
    }

    private string GetCurrentWord() => string.Join("", selectedTiles.ConvertAll(tile => tile.Letter.ToString()));

    private void SubmitWord()
    {
        string currentWord = GetCurrentWord();
        if (currentWord.Equals(targetWord, StringComparison.OrdinalIgnoreCase))
        {
            // Store the positions of the solved word
            List<Vector2Int> positions = new List<Vector2Int>();
            foreach (var tile in selectedTiles)
            {
                positions.Add(tile.GetGridPosition());
            }
            solvedWordPositions[targetWord] = positions;
            
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

    public void ResetGridForNewWord()
    {
        try
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    grid[x, y].ResetTile(); // Just reset the tile state (selection, etc.)
                }
            }

            selectedTiles.Clear();
            WordGameManager.Instance.ClearCurrentWord(); 
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ResetGridForNewWord: {e.Message}\n{e.StackTrace}");
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

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
            Debug.Log("GridManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetupNewPuzzle(string word)
    {
        Debug.Log($"Setting up puzzle for word: {word}");
        
        // Clear existing grid if any
        if (grid != null)
        {
            foreach (var tile in grid)
            {
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }
        }

        // Deactivate current grid if it exists
        if (!string.IsNullOrEmpty(currentWord) && wordGrids.ContainsKey(currentWord))
        {
            wordGrids[currentWord].SetActive(false);
        }

        currentWord = word;
        grid = new LetterTile[gridSize, gridSize];

        // Generate grid data if it doesn't exist
        if (!GameManager.Instance.InitialGrids.ContainsKey(word))
        {
            Debug.Log($"Generating new grid data for word: {word}");
            List<char> newGridData = GenerateGridData(word);
            GameManager.Instance.InitialGrids[word] = newGridData;
        }

        // Get the grid data
        List<char> currentGridData = GameManager.Instance.InitialGrids[word];

        // Create grid container if needed
        GameObject wordGrid;
        if (wordGrids.ContainsKey(word))
        {
            wordGrid = wordGrids[word];
            wordGrid.SetActive(true);
        }
        else
        {
            wordGrid = new GameObject($"WordGrid_{word}");
            wordGrid.transform.SetParent(gridContainer, false);
            RectTransform wordGridRect = wordGrid.AddComponent<RectTransform>();
            wordGridRect.anchoredPosition = Vector2.zero;
            wordGridRect.sizeDelta = gridContainer.sizeDelta;
            wordGrids[word] = wordGrid;
        }

        // Calculate grid positions
        float totalWidth = (gridSize * cellSize) + ((gridSize - 1) * spacing);
        float totalHeight = totalWidth;
        startPosition = new Vector2(
            -(totalWidth / 2) + (cellSize / 2),
            (totalHeight / 2) - (cellSize / 2)
        );

        // Create tiles
        for (int i = 0; i < currentGridData.Count; i++)
        {
            int row = i / gridSize;
            int col = i % gridSize;
            Vector2Int position = new Vector2Int(col, row);
            
            LetterTile tile = CreateTile(position, wordGrid.transform);
            if (tile != null)
            {
                tile.SetLetter(currentGridData[i], position);
                grid[col, row] = tile;
            }
        }

        // Check if word was already guessed and restore the solved state
        if (GameManager.Instance.IsWordGuessed(word))
        {
            List<Vector2Int> wordPath = GameManager.Instance.GetWordPath(word);
            if (wordPath != null)
            {
                foreach (Vector2Int pos in wordPath)
                {
                    if (pos.x >= 0 && pos.x < gridSize && 
                        pos.y >= 0 && pos.y < gridSize)
                    {
                        var tile = grid[pos.x, pos.y];
                        if (tile != null)
                        {
                            tile.SetSolvedColor();
                            tile.isSolved = true;
                            tile.GetComponent<Image>().raycastTarget = false;
                        }
                    }
                }
            }
        }

        WordGameManager.Instance.UpdateCurrentWord(currentWord);
    }

    private List<char> GenerateGridData(string word)
    {
        List<char> gridData = new List<char>();
        int totalCells = gridSize * gridSize;
        
        // Initialize grid with empty spaces
        for (int i = 0; i < totalCells; i++)
        {
            gridData.Add(' ');
        }
        
        // Place the word in a snake pattern
        System.Random random = new System.Random();
        bool placed = false;
        int maxAttempts = 100;
        int attempts = 0;

        while (!placed && attempts < maxAttempts)
        {
            // Clear the grid
            for (int i = 0; i < totalCells; i++)
            {
                gridData[i] = ' ';
            }

            // Pick a random starting position
            int startRow = random.Next(gridSize);
            int startCol = random.Next(gridSize);
            List<Vector2Int> path = new List<Vector2Int>();
            path.Add(new Vector2Int(startCol, startRow));

            bool canPlaceWord = true;
            int currentIndex = 0;

            // Try to place each letter of the word
            while (currentIndex < word.Length - 1 && canPlaceWord)
            {
                Vector2Int currentPos = path[path.Count - 1];
                List<Vector2Int> possibleMoves = new List<Vector2Int>();

                // Check all possible directions (up, right, down, left)
                Vector2Int[] directions = new Vector2Int[]
                {
                    new Vector2Int(0, -1),  // up
                    new Vector2Int(1, 0),   // right
                    new Vector2Int(0, 1),   // down
                    new Vector2Int(-1, 0)   // left
                };

                foreach (Vector2Int dir in directions)
                {
                    Vector2Int newPos = currentPos + dir;
                    if (IsValidPosition(newPos) && !path.Contains(newPos))
                    {
                        possibleMoves.Add(newPos);
                    }
                }

                if (possibleMoves.Count > 0)
                {
                    // Choose a random valid move
                    Vector2Int nextPos = possibleMoves[random.Next(possibleMoves.Count)];
                    path.Add(nextPos);
                    currentIndex++;
                }
                else
                {
                    canPlaceWord = false;
                }
            }

            if (canPlaceWord)
            {
                // Place the word along the path
                for (int i = 0; i < word.Length; i++)
                {
                    Vector2Int pos = path[i];
                    gridData[pos.y * gridSize + pos.x] = char.ToUpper(word[i]);
                }
                placed = true;
            }

            attempts++;
        }

        if (!placed)
        {
            Debug.LogWarning($"Failed to place word {word} in snake pattern after {maxAttempts} attempts");
        }

        // Fill remaining empty spaces with random letters
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZÇĞİÖŞÜ"; // Include Turkish characters
        for (int i = 0; i < totalCells; i++)
        {
            if (gridData[i] == ' ')
            {
                gridData[i] = alphabet[random.Next(alphabet.Length)];
            }
        }
        
        return gridData;
    }

    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
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
            
            // Initialize forming word with first letter
            WordGameManager.Instance.UpdateCurrentWord(currentWord);
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
                
                // Update the forming word with the new letter
                string currentWord = GetSelectedWord();
                WordGameManager.Instance.UpdateCurrentWord(currentWord);
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
            // Update the forming word after each letter is added
            WordGameManager.Instance.UpdateCurrentWord(word);
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
        // Reset the forming word
        WordGameManager.Instance.UpdateCurrentWord("");
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

    public void ShowSolvedWord(string word, List<Vector2Int> path)
    {
        if (word == currentWord && path != null)
        {
            foreach (Vector2Int pos in path)
            {
                int index = pos.y * gridSize + pos.x;
                if (grid[pos.x, pos.y] != null)
                {
                    grid[pos.x, pos.y].GetComponent<Image>().color = Color.yellow;
                }
            }
        }

        WordGameManager.Instance.UpdateCurrentWord(currentWord);
    }

    public List<LetterTile> GetSelectedTiles()
    {
        return selectedTiles;
    }

    // ... rest of your existing methods remain the same ...
}
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
            CalculateDynamicCellSize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetupNewPuzzle(string word)
    {
        Debug.Log($"Setting up puzzle for word: {word}");
        
        CalculateDynamicCellSize();
        
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

        // Fill remaining empty spaces with random letters based on language
        string alphabet = GameManager.Instance.CurrentLanguage == "tr" 
            ? "ABCDEFGHIJKLMNOPRSTUVYZÇĞİÖŞÜ"  // Turkish alphabet
            : "ABCDEFGHIJKLMNOPQRSTUVWXYZ";        // English alphabet

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

    public List<LetterTile> GetSelectedTiles()
    {
        return selectedTiles;
    }

    public List<LetterTile> GetAllTiles()
    {
        List<LetterTile> tiles = new List<LetterTile>();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] != null)
                {
                    tiles.Add(grid[x, y]);
                }
            }
        }
        return tiles;
    }

    private void CalculateDynamicCellSize()
    {
        if (gridContainer == null) return;

        // Get the RectTransform of the grid container
        RectTransform containerRect = gridContainer.GetComponent<RectTransform>();
        if (containerRect == null) return;

        // Get the container's width and height
        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;

        // Calculate available space considering spacing between cells
        float availableWidth = containerWidth - (spacing * (gridSize - 1));
        float availableHeight = containerHeight - (spacing * (gridSize - 1));

        // Calculate cell size based on the smaller of width or height to maintain square cells
        cellSize = Mathf.Min(
            availableWidth / gridSize,
            availableHeight / gridSize
        );

        // Set minimum cell size (adjust these values based on your needs)
        float minCellSize = 80f;  // Minimum size in pixels
        float maxCellSize = 120f; // Maximum size in pixels

        // Clamp the cell size between min and max values
        cellSize = Mathf.Clamp(cellSize, minCellSize, maxCellSize);

        Debug.Log($"Dynamic cell size calculated: {cellSize} (Container: {containerWidth}x{containerHeight})");
    }

    private IEnumerator GenerateGridCoroutine(List<char> letters)
    {
        // Calculate dynamic cell size before generating grid
        CalculateDynamicCellSize();

        // Use a longer delay to make the wave effect very obvious
        const float WAVE_DELAY = 0.15f;
        int index = 0;

        // Center the grid in the container
        float totalGridWidth = (gridSize * cellSize) + (spacing * (gridSize - 1));
        float totalGridHeight = (gridSize * cellSize) + (spacing * (gridSize - 1));
        
        float startX = -(totalGridWidth / 2) + (cellSize / 2);
        float startY = (totalGridHeight / 2) - (cellSize / 2);

        // Debug log to verify the method is being called
        Debug.Log($"Generating grid with {letters.Count} letters, grid size: {gridSize}x{gridSize}, wave delay: {WAVE_DELAY}s");

        // First, create all tile objects but make them completely invisible
        GameObject[,] tileObjects = new GameObject[gridSize, gridSize];
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (index < letters.Count)
                {
                    GameObject letterTileObj = Instantiate(letterTilePrefab, gridContainer);
                    RectTransform rectTransform = letterTileObj.GetComponent<RectTransform>();
                    
                    // Calculate position
                    float xPos = startX + (x * (cellSize + spacing));
                    float yPos = startY - (y * (cellSize + spacing));
                    rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                    rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                    
                    // Make it completely invisible initially
                    letterTileObj.SetActive(false);
                    
                    LetterTile letterTile = letterTileObj.GetComponent<LetterTile>();
                    letterTile.SetLetter(letters[index], new Vector2Int(x, y));
                    grid[x, y] = letterTile;
                    tileObjects[x, y] = letterTileObj;
                    
                    index++;
                }
            }
        }
        
        // Now reveal tiles in diagonal waves from top-left to bottom-right
        // For a grid of size N, there are 2N-1 diagonal waves
        for (int wave = 0; wave < gridSize * 2 - 1; wave++)
        {
            Debug.Log($"Starting wave {wave}");
            
            // Get all positions in this diagonal wave
            List<Vector2Int> wavePositions = new List<Vector2Int>();
            
            for (int x = 0; x <= wave; x++)
            {
                int y = wave - x;
                if (x < gridSize && y < gridSize)
                {
                    wavePositions.Add(new Vector2Int(x, y));
                }
            }
            
            Debug.Log($"Wave {wave} has {wavePositions.Count} positions");
            
            // Animate all tiles in this wave simultaneously
            foreach (Vector2Int pos in wavePositions)
            {
                if (pos.x < gridSize && pos.y < gridSize && tileObjects[pos.x, pos.y] != null)
                {
                    GameObject tileObj = tileObjects[pos.x, pos.y];
                    tileObj.SetActive(true);
                    tileObj.transform.localScale = Vector3.zero;
                    
                    // Animate the tile appearing with a bounce effect
                    StartCoroutine(AnimateTileWithBounce(tileObj));
                }
            }
            
            // Wait before starting the next wave - this is crucial for the wave effect
            yield return new WaitForSeconds(WAVE_DELAY);
        }
    }
    
    private IEnumerator AnimateTileWithBounce(GameObject tileObj)
    {
        // Animate the tile appearing
        float elapsedTime = 0f;
        float animDuration = 0.25f;
        
        // Add a bounce effect
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.6f, 1.3f), // Bigger overshoot for more visible bounce
            new Keyframe(0.8f, 0.95f), // Slight undershoot
            new Keyframe(1f, 1f)
        );
        
        while (elapsedTime < animDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / animDuration);
            
            // Use the animation curve for a bounce effect
            float scale = curve.Evaluate(progress);
            tileObj.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        // Ensure final scale is exactly 1
        tileObj.transform.localScale = Vector3.one;
    }

    // Modify the existing InitializeGrid method to use the coroutine
    public void InitializeGrid(List<char> letters)
    {
        ClearGrid();
        StartCoroutine(GenerateGridCoroutine(letters));
    }

    private void ClearGrid()
    {
        // Clear existing tiles
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }

        grid = new LetterTile[gridSize, gridSize];
    }

    // Add this method to recalculate size when screen is resized
    private void OnRectTransformDimensionsChange()
    {
        // Ensure grid is initialized
        if (grid == null || grid.Length == 0)
        {
            Debug.LogWarning("Grid is not initialized yet.");
            return;
        }

        CalculateDynamicCellSize();
        
        // Update existing tiles if any
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // Check if grid[x, y] exists
                if (grid[x, y] == null)
                {
                    Debug.LogWarning($"Grid cell [{x}, {y}] is null.");
                    continue;
                }

                // Get the RectTransform component
                RectTransform rectTransform = grid[x, y].GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogWarning($"RectTransform not found on grid cell [{x}, {y}].");
                    continue;
                }

                // Update the cell size and position
                rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                
                float totalGridWidth = (gridSize * cellSize) + (spacing * (gridSize - 1));
                float totalGridHeight = (gridSize * cellSize) + (spacing * (gridSize - 1));
                float startX = -(totalGridWidth / 2) + (cellSize / 2);
                float startY = (totalGridHeight / 2) - (cellSize / 2);
                
                float xPos = startX + (x * (cellSize + spacing));
                float yPos = startY - (y * (cellSize + spacing));
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            }
        }
    }
}
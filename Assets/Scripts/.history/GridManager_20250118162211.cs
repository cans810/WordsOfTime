using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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
    [SerializeField] private List<Button> buttons = new List<Button>();
    private List<Button> currentSelectedButtons = new List<Button>();

    public GameObject buttonPrefab; // Reference to your button prefab
    public Transform gridContainer; // Reference to the container of your buttons
    private Button[] allButtons;

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

    private void Start()
    {
        if (buttons.Count == 0)
        {
            buttons = GetComponentsInChildren<Button>().ToList();
            Debug.Log($"Found {buttons.Count} buttons");
        }

        // Get all buttons in the grid
        allButtons = GetComponentsInChildren<Button>();
        Debug.Log($"Found {allButtons.Length} buttons in grid");
    }

    public void SetupNewPuzzle(string word)
    {
        Debug.Log($"Setting up puzzle for word: {word}");
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

        // After creating the grid, restore solved states
        if (GameManager.Instance.IsWordSolved(word))
        {
            var solvedPositions = GameManager.Instance.GetSolvedWordPositions(word);
            if (solvedPositions != null)
            {
                foreach (Vector2Int pos in solvedPositions)
                {
                    if (pos.x >= 0 && pos.x < gridSize && 
                        pos.y >= 0 && pos.y < gridSize)
                    {
                        grid[pos.x, pos.y].SetSolvedColor();
                    }
                }
            }
        }
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
        Debug.Log($"HighlightFirstLetter called with letter: {letter}");
        
        if (allButtons == null || allButtons.Length == 0)
        {
            Debug.LogError("No buttons found in grid!");
            // Try to find buttons again
            allButtons = GetComponentsInChildren<Button>();
        }

        Debug.Log($"Searching through {allButtons?.Length ?? 0} buttons");

        foreach (Button btn in allButtons)
        {
            if (btn == null)
            {
                Debug.LogError("Found null button in array");
                continue;
            }

            TextMeshProUGUI textComponent = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent == null)
            {
                Debug.LogError($"No TextMeshProUGUI found on button {btn.name}");
                continue;
            }

            Debug.Log($"Button text: '{textComponent.text}'");

            if (textComponent.text.Length > 0 && char.ToUpper(textComponent.text[0]) == char.ToUpper(letter))
            {
                Debug.Log($"Found matching letter on button: {textComponent.text}");
                Image buttonImage = btn.GetComponent<Image>();
                if (buttonImage != null)
                {
                    Debug.Log("Setting button color to yellow");
                    buttonImage.color = new Color(1f, 0.92f, 0.016f, 1f); // Bright yellow
                    StartCoroutine(FlashButton(buttonImage));
                }
                else
                {
                    Debug.LogError($"No Image component found on button {btn.name}");
                }
            }
        }
    }

    private IEnumerator FlashButton(Image buttonImage)
    {
        Color startColor = new Color(1f, 0.92f, 0.016f, 1f); // Bright yellow
        Color endColor = Color.white;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;

            // Flash effect
            float alpha = Mathf.PingPong(normalizedTime * 4, 1f);
            buttonImage.color = Color.Lerp(startColor, endColor, alpha);

            yield return null;
        }

        buttonImage.color = endColor;
    }

    // Debug method - call this to print info about all buttons
    public void PrintButtonInfo()
    {
        Debug.Log("=== Button Debug Info ===");
        if (allButtons == null)
        {
            Debug.LogError("allButtons is null!");
            return;
        }

        foreach (Button btn in allButtons)
        {
            if (btn == null)
            {
                Debug.LogError("Found null button!");
                continue;
            }

            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
            Image image = btn.GetComponent<Image>();
            Debug.Log($"Button '{btn.name}': Text='{text?.text ?? "null"}', HasImage={image != null}");
        }
    }

    // Method to update currently selected buttons
    public void UpdateSelectedButtons(List<Button> selected)
    {
        currentSelectedButtons = selected ?? new List<Button>();
    }

    public void OnButtonSelected(Button button)
    {
        if (!currentSelectedButtons.Contains(button))
        {
            currentSelectedButtons.Add(button);
        }
    }

    public void ClearSelectedButtons()
    {
        currentSelectedButtons.Clear();
    }

    public string GetButtonText(Button button)
    {
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        return buttonText != null ? buttonText.text : "";
    }
}
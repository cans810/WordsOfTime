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

    [SerializeField] private KeyCode submitKey = KeyCode.Return;

    private string targetWord; // The word the player must guess
    private List<char> lettersToPlace; // Letters from the target word

    public WordGameManager WordGameManager;

    private void Start()
    {
        InitializeGrid();
        SelectTargetWord();
        PopulateGrid();
        LetterTile.OnTileSelected += HandleTileSelected;

        Debug.Log($"Target Word: {targetWord}");

        WordGameManager = GameObject.Find("Game")
    }


    private void Update()
    {
        if (Input.GetKeyDown(submitKey))
        {
            SubmitWord();
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

        // Populate the grid with random letters, ensuring targetWord letters are included
        char randomLetter = Random.Range(0, 2) == 0 && targetWord.Length > 0 
            ? targetWord[Random.Range(0, targetWord.Length)] 
            : (char)Random.Range('A', 'Z' + 1);

        tile.SetLetter(randomLetter, gridPos);
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

        string currentWord = string.Join("", selectedTiles.ConvertAll(t => t.Letter.ToString()));
        Debug.Log("Current Word: " + currentWord);
        UpdateCurrentWord
    }

    private void SelectTargetWord()
    {
        string era = GameManager.Instance.EraSelected; // Get the selected era
        Debug.Log(era);
        List<string> words = WordValidator.GetWordsForEra(era); // Get word list for the era
        targetWord = words[Random.Range(0, words.Count)].ToUpper(); // Pick a random word
        lettersToPlace = new List<char>(targetWord.ToCharArray()); // Convert the word into a char list
        Debug.Log($"Target Word: {targetWord}");
    }

    private void SubmitWord()
    {
        string currentWord = string.Join("", selectedTiles.ConvertAll(t => t.Letter.ToString()));

        if (currentWord.Equals(targetWord, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"Correct! The word '{currentWord}' matches the target word '{targetWord}'.");
            // Handle success logic, e.g., progress to the next round
        }
        else
        {
            Debug.Log($"Incorrect! The word '{currentWord}' does not match the target word '{targetWord}'.");
            // Handle failure logic, e.g., clear selected tiles
        }

        // Clear the selected tiles regardless of correctness
        foreach (var tile in selectedTiles)
        {
            tile.SetSelected(false);
        }

        selectedTiles.Clear();
    }
}

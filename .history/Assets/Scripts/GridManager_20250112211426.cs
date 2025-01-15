using System.Collections.Generic;
using System.Linq;
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

        WordGameManager = GameObject.Find("WordGameManager").GetComponent<WordGameManager>();
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
        List<Vector2Int> availablePositions = new List<Vector2Int>();

        // First, collect all grid positions.
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                availablePositions.Add(new Vector2Int(x, y));

                // Ensure that the grid is populated with tiles at each position
                if (grid[x, y] == null)
                {
                    CreateTile(new Vector2Int(x, y));
                }
            }
        }

        // Shuffle available positions randomly
        System.Random random = new System.Random();
        availablePositions = availablePositions.OrderBy(x => random.Next()).ToList();

        // Place each letter from the target word into random available positions
        for (int i = 0; i < targetWord.Length; i++)
        {
            Vector2Int position = availablePositions[i];
            char letter = targetWord[i];
            
            // Now that tiles are instantiated, you can safely set letters
            grid[position.x, position.y].SetLetter(letter, position);
        }

        // Fill the remaining empty positions with random letters
        for (int i = targetWord.Length; i < availablePositions.Count; i++)
        {
            Vector2Int position = availablePositions[i];
            char randomLetter = (char)Random.Range('A', 'Z' + 1);

            grid[position.x, position.y].SetLetter(randomLetter, position);
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
        grid[gridPos.x, gridPos.y] = tile;  // Ensure the tile is placed in the correct grid position

        // Log to confirm tile creation
        Debug.Log($"Tile created at {gridPos.x}, {gridPos.y}");
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
        WordGameManager.UpdateCurrentWord(currentWord);
    }

    private void SelectTargetWord()
{
    string era = GameManager.Instance.EraSelected; // Get the selected era
    List<string> words = WordValidator.GetWordsForEra(era); // Get word list for the era

    if (words.Count == 0)
    {
        Debug.LogError($"No words found for the selected era: {era}");
        return;
    }

    targetWord = words[Random.Range(0, words.Count)].ToUpper(); // Pick a random word
    lettersToPlace = new List<char>(targetWord.ToCharArray()); // Convert the word into a char list
    Debug.Log($"Target Word: {targetWord}");

    string sentence = WordValidator.GetSentenceForWord(targetWord, era); // Get sentence for the word
    Debug.Log($"Selected Sentence: {sentence}");
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

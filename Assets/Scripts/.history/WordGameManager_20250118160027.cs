using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Linq;

public class WordGameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public SpriteRenderer BackgroundImage;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI sentenceText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI pointsText;

    [Header("Game Settings")]
    [SerializeField] private int correctWordPoints = 100;
    [SerializeField] private Color correctWordColor = Color.green;
    [SerializeField] private Color incorrectWordColor = Color.red;

    [Header("Progress Bar")]
    public GameObject progressImagePrefab;
    public Transform progressBarContainer;
    private List<GameObject> progressImages = new List<GameObject>();

    private int currentScore = 0;
    private const float MESSAGE_DISPLAY_TIME = 2f;
    public string targetWord;
    private string originalSentence;
    private string currentWord = "";
    public int solvedWordCountInCurrentEra = 0;
    public static WordGameManager Instance { get; private set; }
    public int currentWordIndex = 0;
    public List<string> currentEraWords;
    public HashSet<int> solvedWordsInCurrentEra = new HashSet<int>();
    private bool gameInitialized = false;
    private int hintLevel = 0;
    public const float HINT_HIGHLIGHT_DURATION = 2f;
    private Coroutine numberAnimationCoroutine;
    private const float NUMBER_ANIMATION_DELAY = 0.2f; // Delay between each number
    private TextMeshProUGUI pointText;
    private TextMeshProUGUI hintPointAmountText;
    private Coroutine pointAnimationCoroutine;
    private const float POINT_ANIMATION_DURATION = 3f; // Even faster overall animation
    private const float BUMP_SCALE = 1.60f; // Slightly smaller bump for smoother feel
    private const float BUMP_DURATION = 0.40f; // Faster bumps

    private HashSet<string> solvedWords = new HashSet<string>();
    public delegate void WordSolvedHandler(string word);
    public event WordSolvedHandler OnWordSolved;

    [SerializeField] private GameSceneCanvasController gameSceneCanvasController;

    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintButtonText;
    private Coroutine showLengthCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("WordGameManager initialized"); // Debug log
        }
        else
        {
            Destroy(gameObject);
        }

        // Find the GameSceneCanvasController
        if (gameSceneCanvasController == null)
        {
            gameSceneCanvasController = FindObjectOfType<GameSceneCanvasController>();
        }
    }

    private void Start()
    {
        Debug.Log("WordGameManager starting"); // Debug log
        
        // Find hint button in scene if not assigned
        if (hintButton == null)
        {
            hintButton = GameObject.Find("HintButton")?.GetComponent<Button>();
            if (hintButton != null)
            {
                hintButtonText = hintButton.GetComponentInChildren<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogError("Could not find HintButton in scene!");
            }
        }

        if (GameManager.Instance != null)
        {
            Debug.Log($"Current era: {GameManager.Instance.CurrentEra}"); // Debug log
            StartNewGameInEra();
            UpdateHintButton(); // Initialize hint button state
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            InitializeUI();
            CreateProgressBar();
            
            // Restore solved words for current era
            solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(GameManager.Instance.CurrentEra);
            
            if (!gameInitialized)
            {
                StartNewGameInEra();
                gameInitialized = true;
            }
            else
            {
                LoadWord(currentWordIndex);
                UpdateProgressBar();
                UpdateSentenceDisplay();
            }
            
            UpdateHintButton(); // Initialize hint button state when scene loads
        }
        // Reset UI references when changing scenes
        pointText = null;
        InitializeUI();
    }

    private void CreateProgressBar()
    {
        if (progressImagePrefab == null || progressBarContainer == null) return;

        foreach (Transform child in progressBarContainer)
        {
            Destroy(child.gameObject);
        }
        progressImages.Clear();

        int wordCountInEra = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra).Count;

        for (int i = 0; i < wordCountInEra; i++)
        {
            GameObject progressImage = Instantiate(progressImagePrefab, progressBarContainer);
            progressImages.Add(progressImage);
        }
    }

    private IEnumerator AnimatePointsChange(int startPoints, int endPoints)
    {
        float elapsedTime = 0f;
        Vector3 originalScale = pointText.transform.localScale;
        bool isIncreasing = endPoints > startPoints;
        float animationDuration = 0.5f;
        int lastPoints = startPoints;
        Coroutine currentBumpCoroutine = null;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            
            // Calculate current points
            int currentPoints = Mathf.RoundToInt(Mathf.Lerp(startPoints, endPoints, t));
            
            // If points value changed, create a bump effect
            if (currentPoints != lastPoints)
            {
                // Stop any existing bump animation
                if (currentBumpCoroutine != null)
                {
                    StopCoroutine(currentBumpCoroutine);
                    pointText.transform.localScale = originalScale;
                }
                
                // Start new bump animation
                currentBumpCoroutine = StartCoroutine(BumpScale(pointText.transform, originalScale));
                lastPoints = currentPoints;
            }
            
            // Update points display
            if (pointText != null)
            {
                pointText.text = currentPoints.ToString();
                pointText.color = isIncreasing ? Color.green : Color.red;
            }
            
            yield return null;
        }
        
        // Ensure we end up at the exact final value
        if (pointText != null)
        {
            pointText.text = endPoints.ToString();
            pointText.transform.localScale = originalScale;
            pointText.color = Color.white;
        }
    }

    private IEnumerator BumpScale(Transform target, Vector3 originalScale)
    {
        float bumpDuration = 0.2f;
        float elapsedTime = 0f;
        
        while (elapsedTime < bumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / bumpDuration;
            
            // Create a bump curve that goes up and back down
            float scale = 1f + 0.5f * Mathf.Sin(t * Mathf.PI);
            target.localScale = originalScale * scale;
            
            yield return null;
        }
        
        // Ensure we return to original scale
        target.localScale = originalScale;
    }

    public void HandleCorrectWord()
    {
        if (pointAnimationCoroutine != null)
        {
            StopCoroutine(pointAnimationCoroutine);
        }
        
        int startPoints = GameManager.Instance.CurrentPoints;
        GameManager.Instance.AddPoints(GameManager.POINTS_PER_WORD);
        pointAnimationCoroutine = StartCoroutine(AnimatePointsChange(startPoints, GameManager.Instance.CurrentPoints));
        
        GridManager.Instance.ResetGridForNewWord();
        UpdateProgressBar();
        UpdateSentenceDisplay();
    }

    public void StartNewGameInEra()
    {
        Debug.Log("Starting new game in era"); // Debug log
        if (GameManager.Instance == null) return;

        // Get the pre-shuffled words for the era
        currentEraWords = GameManager.Instance.GetCurrentEraWords();
        Debug.Log($"Got {currentEraWords.Count} words for era {GameManager.Instance.CurrentEra}"); // Debug log
        
        solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(GameManager.Instance.CurrentEra);
        currentWordIndex = 0;
        
        if (currentEraWords.Count > 0)
        {
            LoadWord(currentWordIndex);
            CreateProgressBar();
            UpdateProgressBar();
        }
        else
        {
            Debug.LogError("No words available for current era!");
        }
    }

    private void InitializeUI()
    {
        // Find UI references in the scene
        if (BackgroundImage == null)
            BackgroundImage = GameObject.Find("BackgroundImage")?.GetComponent<SpriteRenderer>();
        
        if (scoreText == null)
            scoreText = GameObject.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
        
        if (messageText == null)
            messageText = GameObject.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
        
        if (sentenceText == null)
            sentenceText = GameObject.Find("Sentence")?.GetComponent<TextMeshProUGUI>();

        if (progressBarContainer == null)
            progressBarContainer = GameObject.Find("ProgressPanel")?.transform;

        if (hintText == null)
            hintText = GameObject.Find("HintText")?.GetComponent<TextMeshProUGUI>();

        if (pointsText == null)
            pointsText = GameObject.Find("PointsText")?.GetComponent<TextMeshProUGUI>();

        // Find point text in either scene
        if (pointText == null)
        {
            // Try to find in game scene
            pointText = GameObject.Find("point")?.GetComponent<TextMeshProUGUI>();
            
            // If not found, try to find in main menu
            if (pointText == null)
            {
                Transform pointPanel = GameObject.Find("PointPanel")?.transform;
                if (pointPanel != null)
                {
                    pointText = pointPanel.Find("point")?.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // Find hint point amount text
        if (hintPointAmountText == null)
        {
            Transform hintButton = GameObject.Find("HintButton")?.transform;
            if (hintButton != null)
            {
                hintPointAmountText = hintButton.Find("PointAmount")?.GetComponent<TextMeshProUGUI>();
            }
        }
        
        // Set initial values
        if (scoreText != null) scoreText.text = "Score: 0";
        if (messageText != null) messageText.text = "";
        if (BackgroundImage != null)
        {
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        }

        UpdatePointsDisplay();
        UpdateHintCostDisplay();
    }

    public void SetupGame(string word, string sentence)
    {
        targetWord = word;
        originalSentence = sentence;
        currentWord = "";
        
        Debug.Log($"New word to guess: {targetWord}"); // Added debug log
        
        if (sentenceText != null)
        {
            sentenceText.text = originalSentence;
        }
    }

    public void UpdateCurrentWord(string word)
    {
        currentWord = word;
        UpdateSentenceDisplay();
    }

    private void UpdateSentenceDisplay()
    {
        if (sentenceText != null && !string.IsNullOrEmpty(originalSentence))
        {
            if (solvedWordsInCurrentEra.Contains(currentWordIndex))
            {
                sentenceText.text = originalSentence.Replace("_____", targetWord);
            }
            else
            {
                string displaySentence = originalSentence;
                if (!string.IsNullOrEmpty(currentWord))
                {
                    // Show formed word with ... for remaining
                    string displayWord = currentWord + "...";
                    displaySentence = originalSentence.Replace("_____", displayWord);
                }
                else if (GameManager.Instance.HasUsedHint(targetWord, 2))
                {
                    string displayWord = new string('_', targetWord.Length);
                    displaySentence = originalSentence.Replace("_____", displayWord);
                }
                else
                {
                    displaySentence = originalSentence.Replace("_____", "...");
                }
                sentenceText.text = displaySentence;
            }
        }
    }

    private void ShowMessage(string message, Color color = default)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color == default ? Color.white : color;
            Invoke(nameof(ClearMessage), MESSAGE_DISPLAY_TIME);
        }
    }

    private void ClearMessage()
    {
        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    public void HandleIncorrectWord()
    {
        ShowMessage("Try again!", incorrectWordColor);
        ClearCurrentWord();
    }

    public void ClearCurrentWord()
    {
        currentWord = "";
        UpdateCurrentWord("");
    }

    public bool IsWordSolved(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        int wordIndex = currentEraWords.IndexOf(word.ToUpper());
        return solvedWordsInCurrentEra.Contains(wordIndex);
    }

    public void LoadWord(int index)
    {
        if (currentEraWords == null || index < 0 || index >= currentEraWords.Count) return;

        // Stop any ongoing animation when loading new word
        if (numberAnimationCoroutine != null)
        {
            StopCoroutine(numberAnimationCoroutine);
            numberAnimationCoroutine = null;
        }

        currentWordIndex = index;
        targetWord = currentEraWords[currentWordIndex];
        string sentence = WordValidator.GetSentenceForWord(targetWord, GameManager.Instance.CurrentEra);

        if (sentence == null) return;

        SetupGame(targetWord, sentence);
        
        if (GridManager.Instance != null)
        {
            GridManager.Instance.SetupNewPuzzle(targetWord);
        }

        // Reset hint button state for new word
        if (hintButton != null)
        {
            hintButton.interactable = GameManager.Instance.CanUseHint(1); // Enable for first hint
            if (hintButtonText != null)
            {
                hintButtonText.text = $"Hint 1 ({GameManager.HINT_COST})";
            }
        }

        UpdateHintButton();
        UpdateSentenceDisplay();
    }

    public void NextWord()
    {
        if (currentEraWords == null) return;

        if (currentWordIndex < currentEraWords.Count - 1)
        {
            currentWordIndex++;
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
    }

    public void PreviousWord()
    {
        if (currentWordIndex > 0)
        {
            currentWordIndex--;
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
    }

    public void UpdateProgressBar()
    {
        if (progressImages == null || progressImages.Count == 0) return;

        // Get solved words for current era
        solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(GameManager.Instance.CurrentEra);

        for (int i = 0; i < progressImages.Count; i++)
        {
            if (progressImages[i] == null) continue;

            RectTransform rectTransform = progressImages[i].GetComponent<RectTransform>();
            Image image = progressImages[i].GetComponent<Image>();
            
            if (rectTransform == null || image == null) continue;

            if (solvedWordsInCurrentEra.Contains(i))
            {
                image.color = Color.green;
                rectTransform.localScale = i == currentWordIndex ? 
                    new Vector3(0.39f, 0.39f, 0.39f) : 
                    new Vector3(0.32f, 0.32f, 0.32f);
            }
            else
            {
                image.color = Color.white;
                rectTransform.localScale = i == currentWordIndex ? 
                    new Vector3(0.39f, 0.39f, 0.39f) : 
                    new Vector3(0.32f, 0.32f, 0.32f);
            }
        }
    }

    public void GiveHint()
    {
        if (GameManager.Instance == null) return;

        // Determine which hint level to use next
        int hintToUse = GameManager.Instance.HasUsedHint(targetWord, 1) ? 2 : 1;

        // Check if we can afford the hint
        if (GameManager.Instance.CanUseHint(hintToUse))
        {
            int cost = hintToUse == 1 ? GameManager.HINT_COST : GameManager.SECOND_HINT_COST;
            int startPoints = GameManager.Instance.CurrentPoints;
            int endPoints = startPoints - cost;

            // Apply the hint
            if (hintToUse == 1)
            {
                GridManager.Instance.HighlightFirstLetter(targetWord[0]);
            }
            else
            {
                ShowWordLength();
            }

            // Store hint usage and deduct points
            GameManager.Instance.UseHint(hintToUse);
            GameManager.Instance.StoreHintUsage(targetWord, hintToUse);
            
            // Animate points change
            if (pointAnimationCoroutine != null)
            {
                StopCoroutine(pointAnimationCoroutine);
            }
            pointAnimationCoroutine = StartCoroutine(AnimatePointsChange(startPoints, endPoints));
            
            // Update UI
            UpdateHintButton();
            UpdateSentenceDisplay();
        }
    }

    private void UpdateHintButton()
    {
        if (hintButton != null)
        {
            bool hasUsedHint1 = GameManager.Instance.HasUsedHint(targetWord, 1);
            bool hasUsedHint2 = GameManager.Instance.HasUsedHint(targetWord, 2);
            
            // Determine next hint level and cost
            int nextHintLevel;
            int hintCost;

            if (!hasUsedHint1)
            {
                nextHintLevel = 1;
                hintCost = GameManager.HINT_COST;
            }
            else if (!hasUsedHint2)
            {
                nextHintLevel = 2;
                hintCost = GameManager.SECOND_HINT_COST;
            }
            else
            {
                // Both hints used
                nextHintLevel = 0;
                hintCost = 0;
            }

            // Check if player can afford the hint
            bool canAfford = GameManager.Instance.CurrentPoints >= hintCost;
            bool hintsAvailable = nextHintLevel > 0;

            // Update button state based on both availability and affordability
            hintButton.interactable = hintsAvailable && canAfford;

            // Update hint button text
            if (hintButtonText != null)
            {
                if (!hintsAvailable)
                {
                    hintButtonText.text = "0 Hints";
                }
                else if (!canAfford)
                {
                    hintButtonText.text = $"{hintCost} ({nextHintLevel})";
                    hintButtonText.color = Color.white;
                }
                else
                {
                    hintButtonText.text = $"{hintCost} ({nextHintLevel})";
                    hintButtonText.color = Color.white;
                }
            }
        }
    }

    private void ShowWordLength()
    {
        if (sentenceText != null && !string.IsNullOrEmpty(originalSentence))
        {
            // Stop any existing coroutine
            if (showLengthCoroutine != null)
            {
                StopCoroutine(showLengthCoroutine);
            }
            
            // Start new animation coroutine
            showLengthCoroutine = StartCoroutine(AnimateWordLength());
        }
    }

    private IEnumerator AnimateWordLength()
    {
        if (sentenceText != null && !string.IsNullOrEmpty(originalSentence))
        {
            // First show numbers sequentially
            for (int i = 1; i <= targetWord.Length; i++)
            {
                string numbers = string.Join("", Enumerable.Range(1, i).Select(x => x.ToString()));
                string displayWord = numbers.PadRight(targetWord.Length, '_');
                sentenceText.text = originalSentence.Replace("_____", displayWord);
                yield return new WaitForSeconds(0.2f); // Adjust timing as needed
            }

            // Then show all underscores
            yield return new WaitForSeconds(0.5f); // Pause before showing underscores
            string finalDisplay = new string('_', targetWord.Length);
            sentenceText.text = originalSentence.Replace("_____", finalDisplay);
        }
    }

    private void UpdatePointsDisplay()
    {
        if (pointText != null)
        {
            pointText.text = GameManager.Instance.CurrentPoints.ToString();
        }
    }

    private void UpdateHintCostDisplay()
    {
        if (hintPointAmountText != null)
        {
            int nextHintCost = (hintLevel == 0) ? GameManager.HINT_COST : GameManager.SECOND_HINT_COST;
            hintPointAmountText.text = nextHintCost.ToString();
            
            // Optionally change color based on whether player can afford it
            hintPointAmountText.color = GameManager.Instance.CanUseHint(hintLevel + 1) ? Color.white : Color.red;
        }
    }

    public void CheckWord(string word, List<LetterTile> selectedTiles)
    {
        if (word == targetWord)
        {
            // Word is correct
            foreach (var tile in selectedTiles)
            {
                tile.SetSolvedColor();
            }
            GameManager.Instance.StoreSolvedWordPositions(word, selectedTiles.Select(t => t.GetGridPosition()).ToList());
            AddPoints();
            solvedWords.Add(word);
            OnWordSolved?.Invoke(word);
        }
        else
        {
            // Word is incorrect
            foreach (var tile in selectedTiles)
            {
                tile.ResetTile();
            }
        }
        GridManager.Instance.ResetGridForNewWord();
    }

    private void AddPoints()
    {
        if (pointAnimationCoroutine != null)
        {
            StopCoroutine(pointAnimationCoroutine);
        }
        
        int startPoints = GameManager.Instance.CurrentPoints;
        GameManager.Instance.AddPoints(GameManager.POINTS_PER_WORD);
        pointAnimationCoroutine = StartCoroutine(AnimatePointsChange(startPoints, GameManager.Instance.CurrentPoints));
        
        solvedWordsInCurrentEra.Add(currentWordIndex);
        GameManager.Instance.StoreSolvedWordIndex(GameManager.Instance.CurrentEra, currentWordIndex);
        UpdateProgressBar();
        UpdateSentenceDisplay();
    }
}
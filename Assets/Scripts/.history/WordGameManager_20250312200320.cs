using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine.Advertisements;

public static class StringExtensions
{
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        int pos = text.IndexOf(search);
        if (pos < 0)
        {
            return text;
        }
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }
}

public class WordGameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Image BackgroundImage;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI sentenceText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Button nextQuestionButton;
    [SerializeField] private Button prevQuestionButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private GameObject difficultyPrefab;


    [Header("Game Settings")]
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
    private string currentBaseWord;
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
    public TextMeshProUGUI pointText;
    private TextMeshProUGUI hintPointAmountText;
    private Coroutine pointAnimationCoroutine;
    private const float POINT_ANIMATION_DURATION = 1.5f; // Even faster overall animation
    private const float BUMP_SCALE = 1.50f; // Slightly smaller bump for smoother feel
    private const float BUMP_DURATION = 0.35f; // Faster bumps

    private HashSet<string> solvedWords = new HashSet<string>();
    public delegate void WordSolvedHandler(string word);
    public event WordSolvedHandler OnWordSolved;

    [SerializeField] private GameSceneCanvasController gameSceneCanvasController;

    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintButtonText;
    [SerializeField] private TextMeshProUGUI solvedStatusText;
    private Coroutine showLengthCoroutine;

    private GridManager gridManager;

    [SerializeField] private int numberOfPreGeneratedGrids = 5; // Number of grids per era
    private List<GridData> preGeneratedGrids = new List<GridData>();

    private string currentFormingWord = "";

    private List<LetterTile> selectedTiles = new List<LetterTile>();

    private const int WORDS_BEFORE_AD = 3;
    public int wordsGuessedCount = 0; // Changed to public for debugging

    [Header("Coin Animation Settings")]
    [SerializeField] private GameObject coinForAnimationPrefab;
    [SerializeField] private GameObject pointPanel;
    [SerializeField] private GameObject safeArea;
    [SerializeField] private float coinSmoothTime = 6f;
    [SerializeField] private float coinMaxSpeed = 90f;
    [SerializeField] private float coinDelay = 0.1f;

    private Coroutine currentBumpCoroutine;

    [Header("Did You Know Panel")]
    [SerializeField] private GameObject didYouKnowPanel;
    [SerializeField] private TextMeshProUGUI didYouKnowText;
    [SerializeField] private float factDisplayTime = 4f;

    private bool isAnimationPlaying = false;

    [Header("Difficulty Display")]
    [SerializeField] private Transform difficultyContent;
    [SerializeField] private List<Sprite> difficultySprites; // Era-specific sprites

    private List<GameObject> difficultyIndicators = new List<GameObject>();

    // Add a flag to track if the current word was just solved
    private bool isCurrentWordNewlySolved = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("WordGameManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }

        // Update to use FindFirstObjectByType
        gridManager = FindFirstObjectByType<GridManager>();

        // Find the GameSceneCanvasController
        if (gameSceneCanvasController == null)
        {
            gameSceneCanvasController = FindFirstObjectByType<GameSceneCanvasController>();
        }
    }

    private void Start()
    {
        Debug.Log("WordGameManager starting"); // Debug log
        
        // Find hint button if not assigned
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
        
        // Initialize points display
        UpdatePointsDisplay();
        
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);

        
        // If we're on Android, verify facts after a delay
        if (Application.platform == RuntimePlatform.Android)
        {
            Invoke("VerifyFactsAfterLoadingDelayed", 5.0f);
        }
        
        // Check if navigation buttons are assigned
        if (nextQuestionButton == null || prevQuestionButton == null)
        {
            Debug.Log("[Android Debug] Navigation buttons not assigned in inspector, will try to find them in InitializeUI");
        }
        
        // Initialize the Did You Know panel
        InitializeDidYouKnowPanel();
        
        // Add ScreenFitter to didYouKnowPanel if it exists
        if (didYouKnowPanel != null && didYouKnowPanel.GetComponent<ScreenFitter>() == null)
        {
            Debug.Log("[Android Debug] Adding ScreenFitter to didYouKnowPanel");
            didYouKnowPanel.AddComponent<ScreenFitter>();
        }

        if (GameManager.Instance != null)
        {
            Debug.Log($"Current era: {GameManager.Instance.CurrentEra}"); // Debug log
            StartNewGameInEra();
            UpdateHintButton(); // Initialize hint button state
            GameManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            GameManager.Instance.OnEraChanged += HandleEraChanged; // Subscribe to era change event
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
        ClearGrid();

        // Update the progress bar based on loaded solved words
        UpdateProgressBar();

        didYouKnowPanel.SetActive(false);

        // Always initialize counter to 0 and reset isAdShowing flag on start
        wordsGuessedCount = 0;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged += HandleEraChanged;
            GameManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged -= HandleEraChanged;
            GameManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            InitializeUI();
            CreateProgressBar();
            
            // Restore solved words for current era with language-specific key
            string eraKey = GameManager.Instance.CurrentEra;
            if (GameManager.Instance.CurrentLanguage == "tr")
            {
                eraKey += "_tr";
            }
            else
            {
                eraKey += "_en";
            }
            
            // First check if we have language-specific indices
            solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(eraKey);
            
            // If no language-specific indices, use the regular GetSolvedWordsForEra method
            // which is now language-aware
            if (solvedWordsInCurrentEra.Count == 0)
            {
                solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(GameManager.Instance.CurrentEra);
            }
            
            if (!gameInitialized)
            {
                StartNewGameInEra();
                gameInitialized = true;
            }
            else
            {
                ClearGrid();
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
        if (progressImagePrefab == null || progressBarContainer == null)
        {
            Debug.LogError("Progress bar prefab or container is null!");
            return;
        }

        // Clear existing progress images
        foreach (Transform child in progressBarContainer)
        {
            Destroy(child.gameObject);
        }
        progressImages.Clear();

        // Get word count for current era
        int wordCountInEra = 0;
        if (GameManager.Instance != null && 
            GameManager.Instance.eraWordsPerLanguage.ContainsKey(GameManager.Instance.CurrentLanguage) &&
            GameManager.Instance.eraWordsPerLanguage[GameManager.Instance.CurrentLanguage].ContainsKey(GameManager.Instance.CurrentEra))
        {
            wordCountInEra = GameManager.Instance.eraWordsPerLanguage[GameManager.Instance.CurrentLanguage][GameManager.Instance.CurrentEra].Count;
        }

        Debug.Log($"Creating progress bar with {wordCountInEra} slots");

        // Create progress indicators
        for (int i = 0; i < wordCountInEra; i++)
        {
            GameObject progressImage = Instantiate(progressImagePrefab, progressBarContainer);
            
            // Ensure proper scaling and positioning
            RectTransform rectTransform = progressImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition3D = Vector3.zero;
            }

            progressImages.Add(progressImage);
        }

        // Force layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(progressBarContainer as RectTransform);
        
        // Update the progress bar immediately
        UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
        if (progressImages == null || progressImages.Count == 0)
        {
            Debug.LogWarning("No progress images to update!");
            return;
        }

        // Get all solved base words across all eras
        HashSet<string> allSolvedBaseWords = GameManager.Instance.GetAllSolvedBaseWords();
        Debug.Log($"Updating progress bar. Total solved words: {allSolvedBaseWords.Count}");

        // Get the current era's words
        var eraWords = GameManager.Instance.eraWordsPerLanguage[GameManager.Instance.CurrentLanguage][GameManager.Instance.CurrentEra];

        for (int i = 0; i < progressImages.Count; i++)
        {
            if (progressImages[i] == null) continue;

            // Get the word at this index
            string wordAtIndex = i < eraWords.Count ? eraWords[i] : null;
            
            bool isSolved = false;
            if (wordAtIndex != null)
            {
                // Get the base word and check if it's solved
                string baseWord = GameManager.Instance.GetBaseWord(wordAtIndex);
                isSolved = allSolvedBaseWords.Contains(baseWord);
            }

            // Update the progress image
            RectTransform rectTransform = progressImages[i].GetComponent<RectTransform>();
            Image image = progressImages[i].GetComponent<Image>();
            
            if (rectTransform != null && image != null)
            {
                image.color = isSolved ? Color.green : Color.white;
                
                // Scale the current word indicator
                Vector3 newScale = i == currentWordIndex ? 
                    new Vector3(0.39f, 0.39f, 0.39f) : 
                    new Vector3(0.32f, 0.32f, 0.32f);
                
                rectTransform.localScale = newScale;
            }
        }
    }

    private IEnumerator AnimatePointsIncrease(int pointsToAdd)
    {
        Debug.Log("Starting point animation");
        
        if (pointAnimationCoroutine != null)
        {
            StopCoroutine(pointAnimationCoroutine);
        }
        
        float elapsedTime = 0f;
        Vector3 originalScale = pointText.transform.localScale;
        Color originalColor = pointText.color;
        int startPoints = GameManager.Instance.CurrentPoints;
        int targetPoints = startPoints + pointsToAdd;
        int lastPoints = startPoints;
        
        // Determine color based on whether points are being added or subtracted
        Color animationColor = pointsToAdd >= 0 ? Color.green : Color.red;

        while (elapsedTime < POINT_ANIMATION_DURATION)
        {
            elapsedTime += Time.deltaTime;
            
            float t = elapsedTime / POINT_ANIMATION_DURATION;
            t = t * t * (3 - 2 * t); // Smoother cubic easing
            
            // Calculate current points with accelerating step size
            int currentPoints = Mathf.RoundToInt(Mathf.Lerp(startPoints, targetPoints, t));
            
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
            
            // Update points display with just the number
            if (pointText != null)
            {
                pointText.text = currentPoints.ToString(); // Only show the number
                pointText.color = animationColor;
            }
            
            yield return null;
        }
        
        // Ensure we end up at the exact final value and return to original color
        if (pointText != null)
        {
            pointText.text = targetPoints.ToString(); // Only show the number
            pointText.transform.localScale = originalScale;
            pointText.color = originalColor;
        }

        // Update the points in GameManager
        GameManager.Instance.AddPoints(pointsToAdd);
    }

    private IEnumerator BumpScale(Transform target, Vector3 originalScale)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < BUMP_DURATION)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / BUMP_DURATION;
            
            // Smoother bump curve
            float scale = 1f + (BUMP_SCALE - 1f) * (1f - (2f * t - 1f) * (2f * t - 1f));
            target.localScale = originalScale * scale;
            
            yield return null;
        }
        
        // Ensure we return to original scale
        target.localScale = originalScale;
    }
    
    private void ShowAd()
    {
        Debug.Log("=== AD FUNCTIONALITY REMOVED ===");
        // Ads have been removed from the game, just reset the counter
        wordsGuessedCount = 0;
    }

    public void StartNewGameInEra()
    {
        Debug.Log("Starting new game in era");
        if (GameManager.Instance != null)
        {
            // Reset the newly solved flag when starting a new game
            isCurrentWordNewlySolved = false;
            
            currentEraWords = new List<string>(GameManager.Instance.eraWordsPerLanguage[GameManager.Instance.CurrentLanguage][GameManager.Instance.CurrentEra]);
            Debug.Log($"Words order for {GameManager.Instance.CurrentEra}: {string.Join(", ", currentEraWords)}");
            
            // Create progress indicators
            CreateProgressBar();
            UpdateProgressBar();

            // Get the language-specific key for era
            string eraKey = GameManager.Instance.CurrentEra;
            if (GameManager.Instance.CurrentLanguage == "tr")
            {
                eraKey += "_tr";
            }
            else
            {
                eraKey += "_en";
            }
            
            // Get language-specific solved words
            solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(eraKey);
            
            // If no language-specific solved words, fall back to non-language specific
            if (solvedWordsInCurrentEra.Count == 0)
            {
                solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(GameManager.Instance.CurrentEra);
            }

            // Load first unsolved word
            currentWordIndex = 0;
            for (int i = 0; i < currentEraWords.Count; i++)
            {
                if (!GameManager.Instance.IsWordGuessed(currentEraWords[i]))
                {
                    currentWordIndex = i;
                    break;
                }
            }

            // Make sure didYouKnowPanel is initialized before loading word
            if (didYouKnowPanel == null)
            {
                Debug.LogWarning("didYouKnowPanel is null in StartNewGameInEra, initializing it");
                InitializeDidYouKnowPanel();
            }

            LoadWord(currentWordIndex);
            UpdateSentenceDisplay();
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    private void InitializeUI()
    {
        // Find UI references in the scene
        if (BackgroundImage == null)
            BackgroundImage = GameObject.Find("BackgroundImage")?.GetComponent<Image>();
        
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
        if (scoreText != null) 
            scoreText.text = "0";
        if (messageText != null) messageText.text = "";
        if (BackgroundImage != null)
        {
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        }

        UpdatePointsDisplay();
        UpdateHintCostDisplay();

        // Find navigation buttons if not assigned
        if (nextQuestionButton == null)
        {
            GameObject nextButton = GameObject.Find("NextQuestionButton");
            if (nextButton != null)
            {
                nextQuestionButton = nextButton.GetComponent<Button>();
                Debug.Log("[Android Debug] Found NextQuestionButton in scene");
            }
            else
            {
                Debug.LogWarning("[Android Debug] NextQuestionButton not found in scene!");
            }
        }

        if (prevQuestionButton == null)
        {
            GameObject prevButton = GameObject.Find("PrevQuestionButton");
            if (prevButton != null)
            {
                prevQuestionButton = prevButton.GetComponent<Button>();
                Debug.Log("[Android Debug] Found PrevQuestionButton in scene");
            }
            else
            {
                Debug.LogWarning("[Android Debug] PrevQuestionButton not found in scene!");
            }
        }
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
        
        // Check if second hint is active
        bool isSecondHintActive = GameManager.Instance.HasUsedHint(targetWord, 2);
        
        if (isSecondHintActive)
        {
            // Always show underscores with current selection when hint 2 is active
            string displayWord = new string('_', targetWord.Length);
            if (GridManager.Instance.IsSelecting() && GridManager.Instance.GetSelectedTiles().Count > 0)
            {
                // Get currently selected letters
                string selectedLetters = string.Join("", GridManager.Instance.GetSelectedTiles().Select(t => t.GetLetter()));
                
                // Replace underscores with selected letters
                char[] displayChars = displayWord.ToCharArray();
                for (int i = 0; i < selectedLetters.Length && i < displayWord.Length; i++)
                {
                    displayChars[i] = selectedLetters[i];
                }
                displayWord = new string(displayChars);
            }
            sentenceText.text = originalSentence.Replace("_____", displayWord);
        }
        else
        {
            if (GridManager.Instance.IsSelecting())
            {
                // Normal mode: show the formed word
                UpdateSentenceDisplay(word);
            }
            else
            {
                // Revert to "..." when not selecting
                ResetSentenceDisplay();
            }
        }
    }

    private void UpdateHintSentenceDisplay(string word)
    {
        if (sentenceText != null)
        {
            // Create the illusion of letters over underscores
            string displaySentence = originalSentence.Replace("_____", "...");
            for (int i = 0; i < word.Length; i++)
            {
                displaySentence = displaySentence.ReplaceFirst("_", word[i].ToString());
            }
            sentenceText.text = displaySentence;
        }
    }

    private void UpdateSentenceDisplay(string word = null)
    {
        if (sentenceText != null && !string.IsNullOrEmpty(originalSentence))
        {
            // First, convert all "_____" to "..."
            string displaySentence = originalSentence.Replace("_____", "...");
            
            // Then, only if we have a target word and it's in the original sentence
            if (targetWord != null && originalSentence.Contains("_____"))
            {
                // Find the position of "_____" in the original sentence
                int blankPos = originalSentence.IndexOf("_____");
                
                if (GameManager.Instance.IsWordGuessed(targetWord))
                {
                    // Show guessed word
                    displaySentence = displaySentence.Substring(0, blankPos) + 
                                    targetWord + 
                                    displaySentence.Substring(blankPos + 3);
                }
                else if (GameManager.Instance.HasUsedHint(targetWord, 2))
                {
                    // If second hint was used, show underscores
                    string displayWord = new string('_', targetWord.Length);
                    
                    if (GridManager.Instance.IsSelecting() && GridManager.Instance.GetSelectedTiles().Count > 0)
                    {
                        // Get currently selected letters
                        string selectedLetters = string.Join("", GridManager.Instance.GetSelectedTiles().Select(t => t.GetLetter()));
                        
                        // Replace underscores with selected letters
                        char[] displayChars = displayWord.ToCharArray();
                        for (int i = 0; i < selectedLetters.Length && i < displayWord.Length; i++)
                        {
                            displayChars[i] = selectedLetters[i];
                        }
                        displayWord = new string(displayChars);
                    }
                    
                    displaySentence = displaySentence.Substring(0, blankPos) + 
                                    displayWord + 
                                    displaySentence.Substring(blankPos + 3);
                }
                else if (!string.IsNullOrEmpty(currentWord) && 
                         GridManager.Instance.IsSelecting() &&
                         GridManager.Instance.GetSelectedTiles().Count > 0)
                {
                    // Show word being formed ONLY while actively selecting letters
                    string selectedLetters = string.Join("", GridManager.Instance.GetSelectedTiles().Select(t => t.GetLetter()));
                    displaySentence = displaySentence.Substring(0, blankPos) + 
                                    selectedLetters + 
                                    "..." + 
                                    displaySentence.Substring(blankPos + 3);
                }
                else
                {
                    // Ensure we show "..." in all other cases
                    displaySentence = displaySentence.Substring(0, blankPos) + 
                                    "..." + 
                                    displaySentence.Substring(blankPos + 3);
                }
            }

            sentenceText.text = displaySentence;
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
        
        // If the word is the current target word, use the stored currentBaseWord
        if (word == targetWord && !string.IsNullOrEmpty(currentBaseWord))
        {
            bool isSolved = GameManager.Instance.IsWordSolved(currentBaseWord);
            Debug.Log($"Checking if word '{word}' is solved using currentBaseWord '{currentBaseWord}': {isSolved}");
            return isSolved;
        }
        
        // Otherwise, get the base word (English version)
        string baseWord = GameManager.Instance.GetBaseWord(word);
        bool result = GameManager.Instance.IsWordSolved(baseWord);
        Debug.Log($"Checking if word '{word}' is solved using baseWord '{baseWord}': {result}");
        return result;
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
        
        // Get the word in the original language list (which might be English or Turkish)
        string originalWord = currentEraWords[currentWordIndex];
        
        // Get the base word (English version) and store it as a class field
        currentBaseWord = GameManager.Instance.GetBaseWord(originalWord);
        
        // Get the proper translation for the current language
        targetWord = GameManager.Instance.GetTranslation(currentBaseWord, GameManager.Instance.CurrentLanguage);
        
        Debug.Log($"[Android Debug] LoadWord: Original={originalWord}, Base={currentBaseWord}, Translated={targetWord}");
        
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
            hintButton.interactable = GameManager.Instance.CanUseHint(1, targetWord); // Enable for first hint
            if (hintButtonText != null)
            {
                hintButtonText.text = $"Hint 1 ({GameManager.HINT_COST})";
            }
        }

        // Update hint button and hint text
        UpdateHintButton();
        UpdateSentenceDisplay();

        // Check if the word is already solved, if so highlight it in the grid
        bool isSolved = GameManager.Instance.IsWordSolved(currentBaseWord);
        Debug.Log($"[Android Debug] Word {currentBaseWord} is solved: {isSolved}, isCurrentWordNewlySolved: {isCurrentWordNewlySolved}");
        
        if (isSolved && !isCurrentWordNewlySolved)
        {
            Debug.Log($"[Android Debug] Word {currentBaseWord} is already solved (not newly solved), checking positions");
            
            // Get positions for the current word
            List<Vector2Int> positions = GameManager.Instance.GetSolvedWordPositions(targetWord);
            Debug.Log($"[Android Debug] GetSolvedWordPositions for {targetWord} returned: {(positions != null ? positions.Count : 0)} positions");
            
            // If no positions found for translated word, try the base word
            if (positions == null || positions.Count == 0)
            {
                Debug.Log($"[Android Debug] No positions found for {targetWord}, trying with base word {currentBaseWord}");
                positions = GameManager.Instance.GetSolvedWordPositions(currentBaseWord);
                Debug.Log($"[Android Debug] GetSolvedWordPositions for base word {currentBaseWord} returned: {(positions != null ? positions.Count : 0)} positions");
                
                // If still no positions, try with translations
                if (positions == null || positions.Count == 0)
                {
                    Debug.Log($"[Android Debug] No positions found for base word, trying with explicit translations");
                    string enWord = GameManager.Instance.GetTranslation(currentBaseWord, "en");
                    string trWord = GameManager.Instance.GetTranslation(currentBaseWord, "tr");
                    
                    positions = GameManager.Instance.GetSolvedWordPositions(enWord);
                    Debug.Log($"[Android Debug] GetSolvedWordPositions for English word {enWord} returned: {(positions != null ? positions.Count : 0)} positions");
                    
                    if (positions == null || positions.Count == 0)
                    {
                        positions = GameManager.Instance.GetSolvedWordPositions(trWord);
                        Debug.Log($"[Android Debug] GetSolvedWordPositions for Turkish word {trWord} returned: {(positions != null ? positions.Count : 0)} positions");
                    }
                }
            }
            
            Debug.Log($"Word {currentBaseWord} is already solved (not newly solved), showing Did You Know fact for {targetWord} instantly");
            // Show the "Did You Know" fact immediately without delay for already solved words
            ShowDidYouKnowInstantly();
            
            // Update the hint button text to show "Solved" in black
            if (hintButtonText != null)
            {
            }
        }
        else
        {
            if (didYouKnowPanel != null)
            {
                didYouKnowPanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning("didYouKnowPanel is null, attempting to reinitialize");
                InitializeDidYouKnowPanel();
            }
            
            if (isSolved && isCurrentWordNewlySolved)
            {
                Debug.Log($"Word {currentBaseWord} was just solved in this session, not showing Did You Know fact instantly");
            }
            else
            {
                Debug.Log($"Word {currentBaseWord} is not solved yet.");
            }
        }

        // Update UI
        UpdateProgressBar();

        // Get the current word's difficulty from GameManager
        string difficulty = GameManager.Instance.GetWordDifficulty(targetWord);
        if (difficultyContent != null && difficultyPrefab != null)
        {
            // Clear existing indicators
            foreach (var indicator in difficultyIndicators)
            {
                Destroy(indicator);
            }
            difficultyIndicators.Clear();

            // Get sprite index based on current era
            int spriteIndex = GameManager.Instance.CurrentEra switch
            {
                "Ancient Egypt" => 0,
                "Medieval Europe" => 1,
                "Renaissance" => 2,
                "Industrial Revolution" => 3,
                "Ancient Greece" => 4,
                "Viking Age" => 5,
                "Feudal Japan" => 6,
                "Ottoman Empire" => 7,
                _ => 0
            };

            // Determine how many indicators to show based on difficulty
            int indicatorCount = difficulty switch
            {
                "easy" => 1,
                "normal" => 2,
                "hard" => 3,
                _ => 1
            };
            
            // Create indicators
            for (int i = 0; i < indicatorCount; i++)
            {
                GameObject indicator = Instantiate(difficultyPrefab, difficultyContent);
                Image indicatorImage = indicator.GetComponent<Image>();
                
                if (indicatorImage != null && spriteIndex < difficultySprites.Count)
                {
                    indicatorImage.sprite = difficultySprites[spriteIndex];
                    //indicatorImage.SetNativeSize();
                }
                
                difficultyIndicators.Add(indicator);
            }

            // Force layout update
            LayoutRebuilder.ForceRebuildLayoutImmediate(difficultyContent as RectTransform);
        }
    }

    public void NextWord()
    {
        Debug.Log($"[Android Debug] NextWord button clicked, isAnimationPlaying: {isAnimationPlaying}");
        if (isAnimationPlaying)
        {
            Debug.Log("[Android Debug] Animation is playing, ignoring NextWord call");
            return;
        }
        
        // Clear the grid to default color
        ClearGrid();

        if (currentEraWords == null) return;

        if (currentWordIndex < currentEraWords.Count - 1)
        {
            // Reset the newly solved flag when navigating to a different word
            isCurrentWordNewlySolved = false;
            
            currentWordIndex++;
            LoadWord(currentWordIndex);
            
            // No need to check if word is solved here - LoadWord already does this
            // and will highlight the word if needed
            
            Debug.Log($"[Android Debug] Moved to next word, index: {currentWordIndex}");
        }
        else
        {
            Debug.Log($"[Android Debug] Already at last word, index: {currentWordIndex}");
            
            // Even if we're at the last word, we should still update the UI
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
    }

    private void ClearGrid()
    {
        int gridSize = GridManager.Instance.grid.GetLength(0);
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                var tile = GridManager.Instance.grid[i, j];
                tile.ResetTile();
                tile.isSolved = false;
                tile.GetComponent<Image>().raycastTarget = true;
                tile.GetComponent<Image>().color = tile.defaultColor; // Directly set the color
            }
        }
    }

    public void PreviousWord()
    {
        Debug.Log($"[Android Debug] PreviousWord button clicked, isAnimationPlaying: {isAnimationPlaying}");
        if (isAnimationPlaying)
        {
            Debug.Log("[Android Debug] Animation is playing, ignoring PreviousWord call");
            return;
        }
        
        // Clear the grid to default color
        ClearGrid();
        
        if (currentWordIndex > 0)
        {
            // Reset the newly solved flag when navigating to a different word
            isCurrentWordNewlySolved = false;
            
            currentWordIndex--;
            LoadWord(currentWordIndex);
            
            // No need to check if word is solved here - LoadWord already does this
            // and will highlight the word if needed
            
            Debug.Log($"[Android Debug] Moved to previous word, index: {currentWordIndex}");
        }
        else
        {
            Debug.Log($"[Android Debug] Already at first word, index: {currentWordIndex}");
            
            // Even if we're at the first word, we should still update the UI
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
    }

    public void GiveHint()
    {
        if (GameManager.Instance == null) return;

        // Determine which hint level to use next
        int hintToUse = GameManager.Instance.HasUsedHint(targetWord, 1) ? 2 : 1;

        // Check if we can afford the hint
        if (GameManager.Instance.CanUseHint(hintToUse, targetWord))
        {
            int cost = hintToUse == 1 ? GameManager.HINT_COST : GameManager.SECOND_HINT_COST;

            // Apply the hint
            if (hintToUse == 1)
            {
                GridManager.Instance.HighlightFirstLetter(targetWord[0]);
            }
            else
            {
                ShowWordLength();
            }

            // Store hint usage
            //GameManager.Instance.UseHint(hintToUse);
            GameManager.Instance.StoreHintUsage(targetWord, hintToUse);
            
            // Animate points change - GameManager.UseHint already handles the point deduction
            if (pointAnimationCoroutine != null)
            {
                StopCoroutine(pointAnimationCoroutine);
            }
            pointAnimationCoroutine = StartCoroutine(AnimatePointsIncrease(-cost));
            
            // Update UI
            UpdateHintButton();
            UpdateSentenceDisplay();
        }
    }

    private void UpdateHintButton()
    {
        if (hintButton != null)
        {
            // Use the stored base word instead of calling GetBaseWord again
            bool isSolved = GameManager.Instance.IsWordSolved(currentBaseWord);
            Debug.Log($"UpdateHintButton: Checking if word '{targetWord}' (base: '{currentBaseWord}') is solved: {isSolved}");
            
            if (isSolved)
            {
                // Disable button and update text if word is already solved
                hintButton.interactable = false;
            
                return;
            }
        
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
            bool canAfford = GameManager.Instance.CanUseHint(nextHintLevel, targetWord);
            bool hintsAvailable = nextHintLevel > 0;

            // Update button state based on both availability and affordability
            hintButton.interactable = hintsAvailable && canAfford;

            // Update hint button text
            if (hintButtonText != null)
            {
                if (!hintsAvailable)
                {
                    hintButtonText.text = GameManager.Instance.CurrentLanguage == "tr" ? "0 İpucu" : "0 Hints";
                    hintButtonText.color = Color.red;
                }
                else if (!canAfford)
                {
                    hintButtonText.text = $"{hintCost} ({nextHintLevel})";
                    hintButtonText.color = Color.red;
                }
                else
                {
                    hintButtonText.text = $"{hintCost} ({nextHintLevel})";
                    hintButtonText.color = Color.green;
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

    public void UpdatePointsDisplay()
    {
        if (pointText != null)
        {
            // Only show the number
            pointText.text = GameManager.Instance.CurrentPoints.ToString();
        }
        else
        {
            // Try to find the point text if it wasn't assigned
            GameObject pointTextObj = GameObject.Find("PointText");
            if (pointTextObj != null)
            {
                pointText = pointTextObj.GetComponent<TextMeshProUGUI>();
                if (pointText != null)
                {
                    pointText.text = GameManager.Instance.CurrentPoints.ToString();
                }
            }
        }
        
        // Update hint cost display as well since it's related to points
        UpdateHintCostDisplay();
        
        // Update hint button to show solved status
        UpdateHintButton();
    }

    private void UpdateHintCostDisplay()
    {
        if (hintPointAmountText != null)
        {
            int nextHintCost = (hintLevel == 0) ? GameManager.HINT_COST : GameManager.SECOND_HINT_COST;
            hintPointAmountText.text = nextHintCost.ToString();
            
            // Optionally change color based on whether player can afford it
            hintPointAmountText.color = GameManager.Instance.CanUseHint(hintLevel + 1, targetWord) ? Color.white : Color.red;
        }
    }

    public void CheckWord(string word, List<LetterTile> selectedTiles)
    {
        Debug.Log($"[Android Debug] Checking word: {word}, Target: {targetWord}");
        
        if (string.IsNullOrEmpty(word))
        {
            Debug.Log("[Android Debug] Word is null or empty");
            return;
        }
        
        // Check if the word matches the target word
        if (word.Equals(targetWord, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[Android Debug] Word matches target: {word}");
            
            // Trigger success vibration
            VibrationManager.VibrateSuccess();
            
            // Mark the word as solved
            string baseWord = GameManager.Instance.GetBaseWord(targetWord);
            GameManager.Instance.MarkWordAsSolved(baseWord);
            
            // Store the solved word index
            StoreSolvedWordIndex(baseWord);
            
            // Add points for solving the word
            int wordLength = targetWord.Length;
            int pointsToAdd = wordLength * 10; // 10 points per letter
            
            // Add bonus points for longer words
            if (wordLength >= 5) pointsToAdd += 10;
            if (wordLength >= 7) pointsToAdd += 20;
            if (wordLength >= 9) pointsToAdd += 30;
            
            // Add points to the player's score
            GameManager.Instance.AddPoints(pointsToAdd);
            
            // Update the UI
            currentScore += pointsToAdd;
            
            // Mark tiles as solved
            foreach (var tile in selectedTiles)
            {
                tile.SetSolvedColor();
                tile.isSolved = true;
                tile.GetComponent<Image>().raycastTarget = false;
            }
            
            // Spawn coins for visual feedback
            StartCoroutine(SpawnCoins(Mathf.Min(wordLength * 2, 10), selectedTiles));
            
            // Show success message
            ShowMessage("Correct!", Color.green);
            
            // Update the progress bar
            UpdateProgressBar();
            
            // Increment the solved word count for the current era
            solvedWordCountInCurrentEra++;
            
            // Add to solved words set
            solvedWords.Add(baseWord);
            
            // Save the solved words to PlayerPrefs
            SaveSolvedWords();
            
            // Increment the words guessed count
            wordsGuessedCount++;
            
            // Check if we should show an ad
            if (wordsGuessedCount % WORDS_BEFORE_AD == 0)
            {
                ShowAd();
            }
            
            // Set the flag to indicate this word was newly solved
            isCurrentWordNewlySolved = true;
            
            // Show the "Did You Know" panel
            ShowDidYouKnow();
            
            // Invoke the word solved event
            OnWordSolved?.Invoke(baseWord);
            
            // Try to mark the word in the current grid
            AttemptToMarkWordInCurrentGrid(targetWord);
        }
        else
        {
            Debug.Log($"[Android Debug] Word does not match target: {word} vs {targetWord}");
            
            // Trigger error vibration
            VibrationManager.VibrateError();
            
            // Reset the tiles
            ResetTiles(selectedTiles);
            
            // Show error message
            ShowMessage("Try again!", incorrectWordColor);
                        Debug.LogWarning($"[Android Debug] Translated word '{translatedWord}' not found in current era words. Case-sensitive comparison failed.");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[Android Debug] Current word index {currentWordIndex} is out of range (max: {currentEraWords.Count - 1})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error processing solved words: {e.Message}\n{e.StackTrace}");
        }
    }
    
    // Android workaround for marking a word as solved even if we don't have position data
    private void AttemptToMarkWordInCurrentGrid(string word)
    {
        if (string.IsNullOrEmpty(word) || GridManager.Instance == null)
        {
            Debug.LogWarning("[Android Debug] Cannot mark word in grid: Invalid word or GridManager is null");
            return;
        }
        
        Debug.Log($"[Android Debug] Attempting to mark word '{word}' in current grid using Pattern Search");
        
        // Get all tiles in the grid
        List<LetterTile> allTiles = GridManager.Instance.GetAllTiles();
        
        // Create a dictionary to map each letter to the tiles containing that letter
        Dictionary<char, List<LetterTile>> letterTiles = new Dictionary<char, List<LetterTile>>();
        
        // Fill the dictionary
        foreach (LetterTile tile in allTiles)
        {
            // GetLetter() returns a char, so no need to use indexing
            char letter = tile.GetLetter();
            if (!letterTiles.ContainsKey(letter))
            {
                letterTiles[letter] = new List<LetterTile>();
            }
            letterTiles[letter].Add(tile);
        }
        
        // Get a list of all possible starting tiles (tiles with the first letter of the word)
        word = word.ToUpper();
        if (word.Length > 0 && letterTiles.ContainsKey(word[0]))
        {
            Debug.Log($"[Android Debug] Found {letterTiles[word[0]].Count} tiles with first letter '{word[0]}'");
            
            // For each possible starting tile, try to find a valid path for the word
            foreach (LetterTile startTile in letterTiles[word[0]])
            {
                List<LetterTile> path = new List<LetterTile> { startTile };
                if (FindValidPath(word, 1, path, letterTiles))
                {
                    // A valid path was found, mark these tiles as solved
                    Debug.Log($"[Android Debug] Valid path found for word '{word}'! Marking tiles as solved.");
                    foreach (LetterTile tile in path)
                    {
                        tile.SetSolvedColor();
                        tile.isSolved = true;
                        tile.GetComponent<Image>().raycastTarget = false;
                    }
                    
                    // Save the positions for future reference
                    List<Vector2Int> positions = path.Select(t => t.GetGridPosition()).ToList();
                    GameManager.Instance.StoreSolvedWordPositions(word, positions);
                    
                    // Store positions for both English and Turkish versions
                    string baseWord = GameManager.Instance.GetBaseWord(word);
                    string enWord = GameManager.Instance.GetTranslation(baseWord, "en");
                    string trWord = GameManager.Instance.GetTranslation(baseWord, "tr");
                    
                    if (word != enWord)
                        GameManager.Instance.StoreSolvedWordPositions(enWord, positions);
                    
                    if (word != trWord)
                        GameManager.Instance.StoreSolvedWordPositions(trWord, positions);
                    
                    return; // Successfully marked the word
                }
            }
            
            Debug.LogWarning($"[Android Debug] Could not find a valid path for word '{word}' in the grid");
        }
        else
        {
            Debug.LogWarning($"[Android Debug] No tiles found with first letter '{(word.Length > 0 ? word[0] : '?')}'");
        }
    }
    
    // Recursive helper method to find a valid path for a word in the grid
    private bool FindValidPath(string word, int currentIndex, List<LetterTile> currentPath, Dictionary<char, List<LetterTile>> letterTiles)
    {
        if (currentIndex >= word.Length)
        {
            // We've found a complete path for the word
            return true;
        }
        
        // Get the current letter we're looking for
        char currentLetter = word[currentIndex];
        
        // Check if we have any tiles with this letter
        if (!letterTiles.ContainsKey(currentLetter))
        {
            return false;
        }
        
        // Get the last tile in our current path
        LetterTile lastTile = currentPath[currentPath.Count - 1];
        Vector2Int lastPos = lastTile.GetGridPosition();
        
        // Check each tile with the current letter to see if it's adjacent to our last tile
        foreach (LetterTile tile in letterTiles[currentLetter])
        {
            // Skip if the tile is already in our path
            if (currentPath.Contains(tile))
            {
                continue;
            }
            
            Vector2Int tilePos = tile.GetGridPosition();
            
            // Check if this tile is adjacent to the last tile in our path
            if (Math.Abs(tilePos.x - lastPos.x) <= 1 && Math.Abs(tilePos.y - lastPos.y) <= 1)
            {
                // This tile is adjacent, add it to our path
                currentPath.Add(tile);
                
                // Recursively try to find the rest of the path
                if (FindValidPath(word, currentIndex + 1, currentPath, letterTiles))
                {
                    return true; // We found a complete path
                }
                
                // If we get here, this path didn't work out, remove the tile and try another
                currentPath.RemoveAt(currentPath.Count - 1);
            }
        }
        
        // We tried all possible tiles and couldn't find a valid path
        return false;
    }

    private void SetTilesSolvedByPositions(List<Vector2Int> positions)
    {
        if (positions == null || positions.Count == 0 || GridManager.Instance == null)
        {
            Debug.LogWarning("[Android Debug] Cannot mark tiles as solved: Invalid positions or GridManager is null");
            return;
        }
        
        Debug.Log($"[Android Debug] Marking {positions.Count} tiles as solved");
        
        foreach (Vector2Int pos in positions)
        {
            LetterTile tile = GridManager.Instance.GetTileAtPosition(pos);
            if (tile != null)
            {
                tile.SetSolvedColor();
                tile.isSolved = true;
                tile.GetComponent<Image>().raycastTarget = false;
                Debug.Log($"[Android Debug] Marked tile at position {pos.x},{pos.y} as solved");
            }
            else
            {
                Debug.LogWarning($"[Android Debug] No tile found at position {pos.x},{pos.y}");
            }
        }
    }
}
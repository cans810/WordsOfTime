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
using CandyCoded.HapticFeedback;

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

    private const int WORDS_BEFORE_AD = 5; // Changed from 3 to 5 to show ads after every 5 words
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
        // Reset the newly solved flag at the beginning of word check
        isCurrentWordNewlySolved = false;
        
        Debug.Log($"Checking word: {word} against target: {targetWord}");
        if (word == targetWord)
        {
            // Word is correct
            Debug.Log("Word is correct, starting coin animation");
            
            // Set the flag to indicate this word was just solved
            isCurrentWordNewlySolved = true;
            
            foreach (var tile in selectedTiles)
            {
                tile.SetSolvedColor();
                tile.isSolved = true;
                tile.GetComponent<Image>().raycastTarget = false;
            }
            
            // Use the stored base word instead of calling GetBaseWord again
            Debug.Log($"Base word: {currentBaseWord}");
            
            // Store positions and mark word as solved
            List<Vector2Int> positions = selectedTiles.Select(t => t.GetGridPosition()).ToList();
            Debug.Log($"[Android Debug] Storing {positions.Count} positions for word: {word}");
            
            // Store positions for the current word
            GameManager.Instance.StoreSolvedWordPositions(word, positions);
            
            // Also store positions using explicit translations to ensure multiple language support
            string enWord = GameManager.Instance.GetTranslation(currentBaseWord, "en");
            string trWord = GameManager.Instance.GetTranslation(currentBaseWord, "tr");
            
            // Store positions for the English version
            GameManager.Instance.StoreSolvedWordPositions(enWord, positions);
            Debug.Log($"[Android Debug] Stored positions for English word: {enWord}");
            
            // Store positions for the Turkish version
            GameManager.Instance.StoreSolvedWordPositions(trWord, positions);
            Debug.Log($"[Android Debug] Stored positions for Turkish word: {trWord}");
            
            // Store the base word for cross-language support
            GameManager.Instance.StoreSolvedBaseWord(GameManager.Instance.CurrentEra, currentBaseWord);
            
            // Store the word index
            StoreSolvedWordIndex(word);
            
            // Trigger word guessed event for saving
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerWordGuessed(word);
                
                // Add both translations to guessed words
                GameManager.Instance.AddGuessedWord(enWord);
                GameManager.Instance.AddGuessedWord(trWord);
            }
            
            // Get the word's difficulty and calculate points
            string difficulty = GameManager.Instance.GetWordDifficulty(targetWord);
            int pointsToAward = difficulty switch
            {
                "easy" => 200,
                "normal" => 300,
                "hard" => 400,
                _ => 200  // default to easy points if difficulty is unknown
            };

            // Calculate coins based on points
            int coinsToSpawn = difficulty switch
            {
                "easy" => 8,    // 200 points = 8 coins
                "normal" => 10,  // 300 points = 10 coins
                "hard" => 12,    // 400 points = 12 coins
                _ => 8          // default to easy coins if difficulty is unknown
            };

            // Start both animations simultaneously
            StartCoroutine(SpawnCoins(coinsToSpawn, selectedTiles));
            StartCoroutine(AnimatePointsIncrease(pointsToAward));
            
            SoundManager.Instance.PlaySound("PointGain");

            // Update UI
            UpdateProgressBar();
            UpdateSentenceDisplay();
            
            // Always increment the counter when a word is correctly guessed
            Debug.Log($"Before increment: wordsGuessedCount = {wordsGuessedCount}");
            wordsGuessedCount++;
            Debug.Log($"Words guessed count incremented to: {wordsGuessedCount}/{WORDS_BEFORE_AD}");
            
            // Save game after successful word guess
            if (SaveManager.Instance != null)
            {
                Debug.Log("Saving game after successful word guess");
                SaveManager.Instance.SaveGame();
            }
            
            // Check if it's time to show an ad
            if (wordsGuessedCount >= WORDS_BEFORE_AD)
            {
                Debug.Log($"=== TIME TO SHOW AD! ===");
                Debug.Log($"wordsGuessedCount: {wordsGuessedCount}, WORDS_BEFORE_AD: {WORDS_BEFORE_AD}");
                ShowAd();
            }
            else
            {
                Debug.Log($"Not showing ad yet. wordsGuessedCount: {wordsGuessedCount}, WORDS_BEFORE_AD: {WORDS_BEFORE_AD}");
            }

            // Notify GameManager about the guessed word
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGuessedWord(word);
            }
            else
            {
                Debug.LogWarning("GameManager instance is null!");
            }

            // Mark the word as solved
            solvedWords.Add(word);

            // Update the progress bar
            UpdateProgressBar();

            // Save the solved words to the JSON file
            SaveSolvedWords();

            // Update hint button state
            UpdateHintButton();
        }
        else
        {
            // Word is incorrect - reset the tiles and sentence
            ResetTiles(selectedTiles);
            ResetSentenceDisplay();
        }
    }

    private void ResetTiles(List<LetterTile> tiles)
    {
        foreach (var tile in tiles)
        {
            tile.ResetTile();
        }
    }

    private void StoreSolvedWordIndex(string word)
    {
        // Get the base word (English version) for the current word
        string baseWord = GameManager.Instance.GetBaseWord(word);
        Debug.Log($"[Android Debug] StoreSolvedWordIndex called for word: {word}, base word: {baseWord}, current language: {GameManager.Instance.CurrentLanguage}");
        
        // LANGUAGE-SPECIFIC INDEX HANDLING
        // Store the index for BOTH languages
        
        // For Turkish, store in a Turkish-specific index collection
        solvedWordsInCurrentEra.Add(currentWordIndex);
        GameManager.Instance.StoreSolvedWordIndex(GameManager.Instance.CurrentEra + "_tr", currentWordIndex);
        
        // For English, also store in an English-specific index collection
        GameManager.Instance.StoreSolvedWordIndex(GameManager.Instance.CurrentEra + "_en", currentWordIndex);
        
        // Store the base word for cross-language support - this is the key part that works across languages
        GameManager.Instance.StoreSolvedBaseWord(GameManager.Instance.CurrentEra, baseWord);
        
        // Store the positions for both the current word and its translation
        List<Vector2Int> positions = GridManager.Instance.GetSelectedTiles().Select(t => t.GetGridPosition()).ToList();
        Debug.Log($"[Android Debug] Storing {positions.Count} positions for word: {word}");
        GameManager.Instance.StoreSolvedWordPositions(word, positions);
        
        // Store positions for ALL language versions of the word
        foreach (string language in new[] { "en", "tr" })
        {
            string translatedWord = GameManager.Instance.GetTranslation(baseWord, language);
            Debug.Log($"[Android Debug] Translation for {language}: {translatedWord}");
            
            if (translatedWord != word)
            {
                GameManager.Instance.StoreSolvedWordPositions(translatedWord, positions);
                Debug.Log($"[Android Debug] Storing positions for {language} translation: {translatedWord}, position count: {positions.Count}");
                
                // Also make sure we mark the word as guessed
                GameManager.Instance.AddGuessedWord(translatedWord);
            }
        }
        
        // Additional debug for Android platform
        if (Application.platform == RuntimePlatform.Android)
        {
            // Check if the translation works correctly
            string turkishWord = GameManager.Instance.GetTranslation(baseWord, "tr");
            string englishWord = GameManager.Instance.GetTranslation(baseWord, "en");
            
            Debug.Log($"[Android Debug] Translations - English: {englishWord}, Turkish: {turkishWord}");
            
            // Check if positions are stored correctly
            var positionsEN = GameManager.Instance.GetSolvedWordPositions(englishWord);
            var positionsTR = GameManager.Instance.GetSolvedWordPositions(turkishWord);
            
            Debug.Log($"[Android Debug] Position count after storing - English: {(positionsEN != null ? positionsEN.Count : 0)}, Turkish: {(positionsTR != null ? positionsTR.Count : 0)}");
            
            // Check if the base word is marked as solved
            bool isBaseSolved = GameManager.Instance.IsWordSolved(baseWord);
            Debug.Log($"[Android Debug] Is base word '{baseWord}' marked as solved: {isBaseSolved}");
        }
    }

    // Add a method to handle language changes
    private void HandleLanguageChanged()
    {
        Debug.Log("[Android Debug] Language changed, updating game for new language");
        
        // Verify facts when switching to Turkish
        if (GameManager.Instance.CurrentLanguage == "tr")
        {
            // Use WordValidator static method directly since WordValidator is a static class
            WordValidator.VerifyTurkishFacts();
        }
        
        // Refresh fact for the current word if it's already solved
        if (currentBaseWord != null && GameManager.Instance.IsWordSolved(currentBaseWord))
        {
            Debug.Log($"[Android Debug] Current word '{currentBaseWord}' is already solved, refreshing fact");
            RefreshCurrentWordFact();
        }

        // Get the language-specific key for the current era
        string eraKey = GameManager.Instance.CurrentEra;
        if (GameManager.Instance.CurrentLanguage == "tr")
        {
            eraKey += "_tr";
        }
        else
        {
            eraKey += "_en";
        }
        
        // Update the collection of solved words
        solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(eraKey);
        Debug.Log($"[Android Debug] Found {solvedWordsInCurrentEra.Count} solved words for era {eraKey}");
        
        // Reload words for the current language
        currentEraWords = new List<string>(GameManager.Instance.eraWordsPerLanguage[GameManager.Instance.CurrentLanguage][GameManager.Instance.CurrentEra]);
        Debug.Log($"[Android Debug] Loaded {currentEraWords.Count} words for era {GameManager.Instance.CurrentEra} in language {GameManager.Instance.CurrentLanguage}");
        
        // Process solved words to mark them as solved in the current language
        ProcessSolvedWords();
        Debug.Log("[Android Debug] Processed solved words after language change");
    }

    public void SetupWordGame(string word)
    {
        Debug.Log($"Setting up word game for: {word}");
        targetWord = word;

        // Make sure GridManager exists
        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager instance is null!");
            return;
        }

        // Setup the grid
        GridManager.Instance.SetupNewPuzzle(word);

        // Update UI
        if (sentenceText != null)
        {
            sentenceText.text = originalSentence;
        }

        // Update sentence
        UpdateSentence();
    }

    private void UpdateSentence()
    {
        Debug.Log("UpdateSentence called");
        if (sentenceText == null)
        {
            Debug.LogError("sentenceText is null!");
            return;
        }
        
        if (string.IsNullOrEmpty(targetWord))
        {
            Debug.LogError("targetWord is empty!");
            return;
        }

        string sentence = WordValidator.GetSentenceForWord(targetWord, GameManager.Instance.CurrentEra);
        Debug.Log($"Original sentence: {sentence}");
        
        if (!string.IsNullOrEmpty(sentence))
        {
            originalSentence = sentence;
            UpdateSentenceDisplay();
        }
        else
        {
            Debug.LogWarning($"No sentence found for word: {targetWord}");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    public List<GridData> GetPreGeneratedGrids()
    {
        return preGeneratedGrids;
    }

    public void LoadPreGeneratedGrids(List<GridData> grids)
    {
        preGeneratedGrids = grids;
        Debug.Log($"Loaded {grids.Count} pre-generated grids");
    }

    public void GenerateNewGrids()
    {
        preGeneratedGrids.Clear();
        // Generate new grids for each era
        foreach (string era in GameManager.Instance.GetAllEras())
        {
            for (int i = 0; i < numberOfPreGeneratedGrids; i++)
            {
                GridData grid = GenerateGridForEra(era);
                preGeneratedGrids.Add(grid);
            }
        }
        Debug.Log($"Generated {preGeneratedGrids.Count} new grids");
        SaveManager.Instance.SaveGame();
    }

    private GridData GenerateGridForEra(string era)
    {
        GridData grid = new GridData();
        grid.era = era;
        grid.gridSize = 5; // Or whatever size you use
        
        // Get a random word for this era
        grid.targetWord = GetRandomWordForEra(era);
        
        // Generate the grid letters and positions
        GenerateGridLettersAndPositions(grid);
        
        return grid;
    }

    private string GetRandomWordForEra(string era)
    {
        if (GameManager.Instance != null)
        {
            var eraWords = GameManager.Instance.GetCurrentEraWords();
            if (eraWords != null && eraWords.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, eraWords.Count);
                return eraWords[randomIndex];
            }
        }
        
        Debug.LogError($"No words found for era: {era}");
        return string.Empty;
    }

    private void GenerateGridLettersAndPositions(GridData grid)
    {
        grid.letters = new List<string>();
        grid.correctWordPositions = new List<Vector2IntSerializable>();

        // Create a 2D array to track letter placement
        string[,] gridArray = new string[grid.gridSize, grid.gridSize];
        
        // Convert target word to char array
        char[] wordChars = grid.targetWord.ToCharArray();
        
        // Random starting position
        int startX = UnityEngine.Random.Range(0, grid.gridSize);
        int startY = UnityEngine.Random.Range(0, grid.gridSize);
        
        // Random direction (horizontal or vertical)
        bool isHorizontal = UnityEngine.Random.value > 0.5f;
        
        // Place the word
        for (int i = 0; i < wordChars.Length; i++)
        {
            int x = isHorizontal ? (startX + i) % grid.gridSize : startX;
            int y = isHorizontal ? startY : (startY + i) % grid.gridSize;
            
            gridArray[x, y] = wordChars[i].ToString();
            grid.correctWordPositions.Add(new Vector2IntSerializable(x, y));
        }
        
        // Fill remaining spaces with random letters
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (int x = 0; x < grid.gridSize; x++)
        {
            for (int y = 0; y < grid.gridSize; y++)
            {
                if (string.IsNullOrEmpty(gridArray[x, y]))
                {
                    gridArray[x, y] = alphabet[UnityEngine.Random.Range(0, alphabet.Length)].ToString();
                }
            }
        }
        
        // Convert 2D array to linear list
        for (int y = 0; y < grid.gridSize; y++)
        {
            for (int x = 0; x < grid.gridSize; x++)
            {
                grid.letters.Add(gridArray[x, y]);
            }
        }
    }

    // Method to get a grid for a specific era
    public GridData GetGridForEra(string era)
    {
        var availableGrids = preGeneratedGrids.FindAll(g => g.era == era);
        if (availableGrids.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableGrids.Count);
            return availableGrids[randomIndex];
        }
        
        // If no grid found, generate a new one
        return GenerateGridForEra(era);
    }

    public void OnTileDragStart(LetterTile tile)
    {
        // Don't update the sentence display during drag
        // Just add the tile to the selection
        selectedTiles.Add(tile);
        tile.SetSelected(true);
    }

    public void OnTileDragEnd()
    {
        // Reset the sentence display when drag ends
        ResetSentenceDisplay();
    }

    private void ResetSentenceDisplay()
    {
        if (sentenceText != null)
        {
            // Check if second hint is active
            bool isSecondHintActive = GameManager.Instance.HasUsedHint(targetWord, 2);
            
            if (isSecondHintActive)
            {
                // Keep showing underscores if second hint is active
                string underscores = new string('_', targetWord.Length);
                sentenceText.text = originalSentence.Replace("_____", underscores);
            }
            else
            {
                // Otherwise, revert to "..."
                sentenceText.text = originalSentence.Replace("_____", "...");
            }
        }
    }

    private string GetFormedWord()
    {
        return string.Join("", selectedTiles.Select(t => t.GetLetter()));
    }

    private bool IsValidWord(string word)
    {
        return !string.IsNullOrEmpty(word) && word.Length > 0 && word.All(char.IsLetter);
    }

    private void SaveSolvedWords()
    {
        // Save solved words to the JSON file
        SaveManager.Instance.Data.solvedWords = solvedWords.ToList();
        SaveManager.Instance.SaveGame();
        Debug.Log("Solved words saved to JSON: " + string.Join(",", solvedWords));
    }

    private void OnApplicationQuit()
    {
        // Save solved words when the game is closed
        SaveSolvedWords();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveSolvedWords();
            Debug.Log("Game paused, saved solved words");
        }
        else
        {
            // When resuming, check if we need to reset the isAdShowing flag
            // This handles cases where the app was paused during an ad and then resumed
        }
    }

    private IEnumerator SpawnCoins(int coinCount, List<LetterTile> guessedTiles)
    {
        Debug.Log($"Spawning {coinCount} coins from guessed tiles");

        if (guessedTiles == null || guessedTiles.Count == 0)
        {
            Debug.LogError("Guessed tiles list is null or empty!");
            yield break;
        }

        if (coinForAnimationPrefab == null)
        {
            Debug.LogError("Coin prefab is not assigned!");
            yield break;
        }

        if (safeArea == null)
        {
            Debug.LogError("Safe area reference is missing!");
            yield break;
        }

        // Add debug logs
        Debug.Log($"Starting coin animations, count: {coinCount}");
        
        List<GameObject> activeCoins = new List<GameObject>();

        for (int i = 0; i < coinCount; i++)
        {
            // Get a random tile from the guessed word
            LetterTile randomTile = guessedTiles[UnityEngine.Random.Range(0, guessedTiles.Count)];
            if (randomTile == null)
            {
                Debug.LogWarning("Random tile is null, skipping coin spawn");
                continue;
            }

            Vector3 spawnPosition = randomTile.transform.position;
            Debug.Log($"Spawning coin {i + 1} at position: {spawnPosition}");

            GameObject coin = Instantiate(coinForAnimationPrefab, spawnPosition, Quaternion.identity, safeArea.transform);
            if (coin == null)
            {
                Debug.LogError("Failed to instantiate coin!");
                continue;
            }

            // Make the coin bigger
            coin.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            coin.tag = "Coin";
            activeCoins.Add(coin);
            StartCoroutine(MoveCoinToPanel(coin, () => {
                activeCoins.Remove(coin);
                Debug.Log($"Coin destroyed, remaining coins: {activeCoins.Count}");
                if (activeCoins.Count == 0)
                {
                    Debug.Log("All coins finished, showing Did You Know panel");
                    ShowDidYouKnow();
                }
            }));
        }
    }

    private IEnumerator MoveCoinToPanel(GameObject coin, System.Action onDestroy)
    {
        Debug.Log("Moving coin to panel");

        Vector3 startPoint = coin.transform.position;
        Vector3 endPoint = pointPanel.transform.position;

        Vector3 controlPoint1 = startPoint + new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(1f, 3f), 0);
        Vector3 controlPoint2 = endPoint + new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(1f, 3f), 0);

        float baseDuration = 1f;
        float randomDuration = UnityEngine.Random.Range(baseDuration * 0.9f, baseDuration * 1.1f);
        float elapsedTime = 0f;

        while (elapsedTime < randomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / randomDuration;

            Vector3 position = CalculateBezierPoint(t, startPoint, controlPoint1, controlPoint2, endPoint);
            coin.transform.position = position;

            if (Vector3.Distance(coin.transform.position, endPoint) < 0.1f)
            {
                break;
            }

            yield return null;
        }

        Debug.Log("Coin reached destination");
        onDestroy?.Invoke();
        Destroy(coin);
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    public void ShowDidYouKnow()
    {
        Debug.Log($"[DidYouKnow Debug] ShowDidYouKnow called for word: {targetWord} in era: {GameManager.Instance?.CurrentEra}");
        
        // Initialize panel references if needed
        InitializeDidYouKnowPanel();
        
        // Make sure GameManager is valid
        if (GameManager.Instance == null)
        {
            Debug.LogError("[DidYouKnow Debug] GameManager.Instance is null! Cannot proceed.");
            return;
        }
        
        try
        {
            // Log some key information to help with debugging
            Debug.Log($"[DidYouKnow Debug] didYouKnowPanel active state before check: {didYouKnowPanel.activeSelf}");
            
            // Check if the current word is solved
            bool isWordSolved = IsWordSolved(targetWord);
            Debug.Log($"[DidYouKnow Debug] Is word '{targetWord}' solved: {isWordSolved}");
            
            // If the word is not solved, hide the panel and return
            if (!isWordSolved)
            {
                Debug.Log($"[DidYouKnow Debug] Word '{targetWord}' is not solved, hiding panel");
                didYouKnowPanel.SetActive(false);
                return;
            }
            
            // Get the current language
            string language = GameManager.Instance.CurrentLanguage;
            string era = GameManager.Instance.CurrentEra;
            
            // Add more detailed logging about the word and era
            Debug.Log($"[DidYouKnow Debug] Current word details - Word: '{targetWord}', Era: '{era}', Language: '{language}'");
            
            // Check if the word exists in the WordValidator's data
            Debug.Log($"[DidYouKnow Debug] Checking if word '{targetWord}' is valid for era '{era}': {WordValidator.IsValidWord(targetWord, era)}");
            
            // First try to get the fact using the exact word in the current language
            Debug.Log($"[DidYouKnow Debug] Getting fact for word: {targetWord}, era: {era}, language: {language}");
            string fact = WordValidator.GetFactForWord(targetWord, era, language);
            
            // Log the result of getting the fact
            if (fact == "LOADING")
            {
                Debug.Log("[DidYouKnow Debug] Fact is still in LOADING state");
            }
            else if (string.IsNullOrEmpty(fact))
            {
                Debug.LogWarning("[DidYouKnow Debug] Fact is empty or null, checking JSON files");
                
                // Check if the word exists in the JSON files - this should populate the fact
                WordValidator.CheckWordInJsonFiles(targetWord, era);
                
                // Try to get the fact again after checking JSON files
                fact = WordValidator.GetFactForWord(targetWord, era, language);
                
                // If still no fact and we're in Turkish, try with the exact Turkish word without falling back to English
                if (string.IsNullOrEmpty(fact) && language == "tr")
                {
                    Debug.Log($"[DidYouKnow Debug] Still no fact for Turkish word '{targetWord}', trying to find it directly in Turkish data");
                    // No additional fallback needed here as GetFactForWord already handles this case
                }
                // If still no fact, try with base word as a last resort
                else if (string.IsNullOrEmpty(fact))
                {
                    // Try to get the base word (English version) and use that to get the fact
                    string baseWord = GameManager.Instance.GetBaseWord(targetWord);
                    Debug.Log($"[DidYouKnow Debug] Base word for '{targetWord}' is '{baseWord}'");
                    
                    if (baseWord != targetWord)
                    {
                        Debug.Log($"[DidYouKnow Debug] Trying with base word: {baseWord}");
                        fact = WordValidator.GetFactForWord(baseWord, era, language);
                        
                        if (string.IsNullOrEmpty(fact) && language != "en")
                        {
                            Debug.Log($"[DidYouKnow Debug] Still no fact, trying with English language");
                            fact = WordValidator.GetFactForWord(baseWord, era, "en");
                        }
                        
                        // If still no fact, check if the base word exists in the JSON files
                        if (string.IsNullOrEmpty(fact))
                        {
                            WordValidator.CheckWordInJsonFiles(baseWord, era);
                        }
                    }
                }
            }
            else
                Debug.Log($"[DidYouKnow Debug] Successfully got fact: {(fact.Length > 30 ? fact.Substring(0, 30) + "..." : fact)}");
            
            if (!string.IsNullOrEmpty(fact) && fact != "LOADING")
            {
                // Make sure the OkayButton is active
                Transform okayButtonTransform = didYouKnowPanel.transform.Find("OkayButton");
                if (okayButtonTransform != null)
                {
                    okayButtonTransform.gameObject.SetActive(true);
                    Debug.Log("[DidYouKnow Debug] OkayButton set to active");
                }
                else
                {
                    Debug.LogWarning("[DidYouKnow Debug] OkayButton not found in didYouKnowPanel");
                }
                
                // Start coroutine to show the panel with the fact
                Debug.Log("[DidYouKnow Debug] Starting ShowDidYouKnowCoroutine");
                StartCoroutine(ShowDidYouKnowCoroutine(fact));
            }
            else
            {
                Debug.Log("[DidYouKnow Debug] Facts are still loading or empty, will retry in 1 second");
                Invoke(nameof(ShowDidYouKnow), 1f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DidYouKnow Debug] Error in ShowDidYouKnow: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private IEnumerator ShowDidYouKnowCoroutine(string fact)
    {
        Debug.Log($"[DidYouKnow Debug] ShowDidYouKnowCoroutine started with fact: {(fact.Length > 30 ? fact.Substring(0, 30) + "..." : fact)}");
        
        bool panelActivated = false;
        
        try
        {
            // Make sure the panel is initialized
            if (didYouKnowPanel == null)
            {
                Debug.LogError("[DidYouKnow Debug] didYouKnowPanel is null!");
                yield break;
            }
            
            // Make sure the text component is initialized
            if (didYouKnowText == null)
            {
                Debug.LogError("[DidYouKnow Debug] didYouKnowText is null!");
                yield break;
            }
            
            // Format the fact with the appropriate title
            string language = GameManager.Instance.CurrentLanguage;
            string titleText = language == "tr" ? "BİLİYOR MUYDUNUZ?" : "DID YOU KNOW?";
            didYouKnowText.text = titleText + "\n\n" + fact;
            Debug.Log($"[DidYouKnow Debug] Set didYouKnowText to: {titleText}\\n\\n{(fact.Length > 30 ? fact.Substring(0, 30) + "..." : fact)}");
            
            // Check if the panel has a CanvasGroup component
            CanvasGroup canvasGroup = didYouKnowPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"[DidYouKnow Debug] Found CanvasGroup with alpha: {canvasGroup.alpha}");
                
                // Make sure the CanvasGroup is fully visible
                if (canvasGroup.alpha < 1f)
                {
                    Debug.Log("[DidYouKnow Debug] CanvasGroup alpha was less than 1, setting to 1");
                    canvasGroup.alpha = 1f;
                }
                
                // Ensure interactivity
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            // Activate the panel
            didYouKnowPanel.SetActive(true);
            Debug.Log("[DidYouKnow Debug] Panel activated");
            
            // Disable navigation buttons while panel is showing
            DisableNavigationButtons();
            
            panelActivated = true;
            
            // Force layout refresh to ensure UI elements are properly positioned
            Canvas.ForceUpdateCanvases();
            if (didYouKnowPanel.GetComponent<RectTransform>() != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(didYouKnowPanel.GetComponent<RectTransform>());
                Debug.Log("[DidYouKnow Debug] Forced layout update");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DidYouKnow Debug] Error setting up didYouKnow panel: {e.Message}\n{e.StackTrace}");
            EnableNavigationButtons();
            yield break;
        }
        
        // Verify panel is actually active - moved outside try-catch
        if (panelActivated)
        {
            yield return null; // Wait one frame
            Debug.Log($"[DidYouKnow Debug] Panel active state after activation: {didYouKnowPanel.activeSelf}, Panel enabled: {didYouKnowPanel.activeInHierarchy}");
            
            // If panel is not active in hierarchy, try to activate it again
            if (!didYouKnowPanel.activeInHierarchy)
            {
                Debug.LogWarning("[DidYouKnow Debug] Panel not active in hierarchy after activation, trying again");
                didYouKnowPanel.SetActive(false);
                yield return null;
                didYouKnowPanel.SetActive(true);
                yield return null;
                Debug.Log($"[DidYouKnow Debug] Panel active state after second activation: {didYouKnowPanel.activeSelf}, Panel enabled: {didYouKnowPanel.activeInHierarchy}");
            }
        }
    }

    // Helper method to disable navigation buttons
    private void DisableNavigationButtons()
    {
        Debug.Log($"[Android Debug] Disabling navigation buttons. Next button: {(nextQuestionButton != null ? "exists" : "null")}, Prev button: {(prevQuestionButton != null ? "exists" : "null")}");
        if (nextQuestionButton != null) nextQuestionButton.interactable = false;
        if (prevQuestionButton != null) prevQuestionButton.interactable = false;
        if (homeButton != null) homeButton.interactable = false;
        Debug.Log("[Android Debug] Navigation buttons disabled");
    }
    
    // Helper method to enable navigation buttons
    private void EnableNavigationButtons()
    {
        Debug.Log($"[Android Debug] Enabling navigation buttons. Next button: {(nextQuestionButton != null ? "exists" : "null")}, Prev button: {(prevQuestionButton != null ? "exists" : "null")}");
        if (nextQuestionButton != null) nextQuestionButton.interactable = true;
        if (prevQuestionButton != null) prevQuestionButton.interactable = true;
        if (homeButton != null) homeButton.interactable = true;
        Debug.Log("[Android Debug] Navigation buttons re-enabled");
    }

    // Add this method to handle the close button click
    public void OnCloseDidYouKnowPanel()
    {
        Debug.Log("[Android Debug] Close button clicked on Did You Know panel");
        
        // Hide panel
        didYouKnowPanel.SetActive(false);
        
        // Re-enable navigation buttons
        EnableNavigationButtons();
        
        // Reset animation flag
        isAnimationPlaying = false;
        Debug.Log("[Android Debug] Animation flag reset after manual close");
    }

    // Add these methods to directly test button clicks
    public void OnNextButtonClick()
    {
        Debug.Log("[Android Debug] Next button clicked directly");
        NextWord();
    }

    public void OnPrevButtonClick()
    {
        Debug.Log("[Android Debug] Prev button clicked directly");
        PreviousWord();
    }

    // Add this method to ensure ads are initialized before showing
    private bool EnsureAdsInitialized()
    {
        // Only rewarded ads are used in the game now
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("Unity Ads not initialized. Attempting to initialize...");
            
            if (AdManager.Instance != null)
            {
                // Initialize through the AdsInitializer instead
                AdsInitializer adsInitializer = FindObjectOfType<AdsInitializer>();
                if (adsInitializer != null)
                {
                    adsInitializer.InitializeAds();
                    Debug.Log("Requested Unity Ads initialization");
                }
                else
                {
                    Debug.LogError("Cannot initialize Unity Ads - AdsInitializer not found");
                }
                return false; // Return false as ads are not ready yet
            }
            else
            {
                Debug.LogError("Cannot initialize Unity Ads - AdManager not found");
                return false;
            }
        }
        
        return true;
    }

    // Handle era change event
    private void HandleEraChanged()
    {
        Debug.Log("Era changed, updating solved words display");
        
        try
        {
            // Reset the newly solved flag when changing eras
            isCurrentWordNewlySolved = false;
            
            // Update background image immediately to ensure it changes even if other operations fail
            UpdateBackgroundImage();
            
            // Reset current word index and load the first word for the new era
            currentWordIndex = 0;
            
            // Start a new game in the current era
            StartNewGameInEra();
            
            // Update points display
            UpdatePointsDisplay();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in HandleEraChanged: {e.Message}\n{e.StackTrace}");
            
            // Make sure the background image is updated even if other operations fail
            UpdateBackgroundImage();
        }
    }
    
    private void UpdateBackgroundImage()
    {
        // Update background image if we have access to it
        GameObject backgroundObj = GameObject.Find("BackgroundImage");
        if (backgroundObj != null)
        {
            Image backgroundImage = backgroundObj.GetComponent<Image>();
            if (backgroundImage != null && GameManager.Instance != null)
            {
                backgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
                Debug.Log("Updated background image for era: " + GameManager.Instance.CurrentEra);
            }
            else
            {
                Debug.LogWarning("BackgroundImage component or GameManager.Instance is null");
            }
        }
        else
        {
            Debug.LogWarning("BackgroundImage GameObject not found");
        }
        
        // Note: ProcessSolvedWords() is called in StartNewGameInEra, which is already invoked in HandleEraChanged
    }

    /// <summary>
    /// Shows the Did You Know panel immediately without any delay.
    /// This method is used for already solved words to display the fact instantly.
    /// </summary>
    public void ShowDidYouKnowInstantly()
    {
        Debug.Log($"[DidYouKnow Debug] ShowDidYouKnowInstantly called for word: {targetWord} in era: {GameManager.Instance?.CurrentEra}");
        
        // Initialize panel references if needed
        InitializeDidYouKnowPanel();
        
        // Make sure GameManager is valid
        if (GameManager.Instance == null)
        {
            Debug.LogError("[DidYouKnow Debug] GameManager.Instance is null! Cannot proceed.");
                return;
        }
        
        try
        {
            // Log some key information to help with debugging
            Debug.Log($"[DidYouKnow Debug] didYouKnowPanel active state before check: {didYouKnowPanel.activeSelf}");
            
            // Check if the current word is solved
            bool isWordSolved = IsWordSolved(targetWord);
            Debug.Log($"[DidYouKnow Debug] Is word '{targetWord}' solved: {isWordSolved}");
            
            // If the word is not solved, hide the panel and return
            if (!isWordSolved)
            {
                Debug.Log($"[DidYouKnow Debug] Word '{targetWord}' is not solved, hiding panel");
                didYouKnowPanel.SetActive(false);
                return;
            }
            
            // Get the current language
            string language = GameManager.Instance.CurrentLanguage;
            string era = GameManager.Instance.CurrentEra;
            
            Debug.Log($"[DidYouKnow Debug] Getting fact for word: {targetWord}, era: {era}, language: {language}");
            string fact = WordValidator.GetFactForWord(targetWord, era, language);
            
            // Log the result of getting the fact
            if (fact == "LOADING")
                Debug.Log("[DidYouKnow Debug] Fact is still in LOADING state");
            else if (string.IsNullOrEmpty(fact))
            {
                Debug.LogWarning("[DidYouKnow Debug] Fact is empty or null, trying with base word");
                
                // Check if the word exists in the JSON files - this should populate the fact
                WordValidator.CheckWordInJsonFiles(targetWord, era);
                
                // Try to get the fact again after checking JSON files
                fact = WordValidator.GetFactForWord(targetWord, era, language);
                
                // If still no fact, try with base word
                if (string.IsNullOrEmpty(fact))
                {
                    // Try to get the base word (English version) and use that to get the fact
                    string baseWord = GameManager.Instance.GetBaseWord(targetWord);
                    Debug.Log($"[DidYouKnow Debug] Base word for '{targetWord}' is '{baseWord}'");
                    
                    if (baseWord != targetWord)
                    {
                        Debug.Log($"[DidYouKnow Debug] Trying with base word: {baseWord}");
                        fact = WordValidator.GetFactForWord(baseWord, era, language);
                        
                        if (string.IsNullOrEmpty(fact) && language != "en")
                        {
                            Debug.Log($"[DidYouKnow Debug] Still no fact, trying with English language");
                            fact = WordValidator.GetFactForWord(baseWord, era, "en");
                        }
                        
                        // If still no fact, check if the base word exists in the JSON files
                        if (string.IsNullOrEmpty(fact))
                        {
                            WordValidator.CheckWordInJsonFiles(baseWord, era);
                            // Try one more time after checking JSON files
                            fact = WordValidator.GetFactForWord(baseWord, era, language);
                        }
                    }
                }
            }
            else
                Debug.Log($"[DidYouKnow Debug] Successfully got fact: {(fact.Length > 30 ? fact.Substring(0, 30) + "..." : fact)}");
            
            if (!string.IsNullOrEmpty(fact) && fact != "LOADING")
            {
                // Get the language-specific title
                string titleText = language == "tr" ? "BİLİYOR MUYDUNUZ?" : "DID YOU KNOW?";
                
                // Set the fact text
                didYouKnowText.text = titleText + "\n\n" + fact;
                Debug.Log("[DidYouKnow Debug] Set fact text: " + (fact.Length > 30 ? fact.Substring(0, 30) + "..." : fact));
                
                // Activate the panel
                didYouKnowPanel.SetActive(true);
                Debug.Log("[DidYouKnow Debug] Panel activated instantly");

                Transform okayButtonTransform = didYouKnowPanel.transform.Find("OkayButton");
                if (okayButtonTransform != null)
                {
                    okayButtonTransform.gameObject.SetActive(false);
                    Debug.Log("[DidYouKnow Debug] OkayButton set to active");
                }
            }
            else
            {
                Debug.LogWarning("[DidYouKnow Debug] Cannot show panel immediately: fact is empty or still loading");
                // Still try regular method which includes retry mechanism
                ShowDidYouKnow();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DidYouKnow Debug] Error showing fact panel instantly: {e.Message}\n{e.StackTrace}");
        }
    }

    // Add a method to initialize the Did You Know panel
    public void InitializeDidYouKnowPanel()
    {
        Debug.Log("[DidYouKnow Debug] Initializing Did You Know panel");
        
        // Find and validate panel references
        if (didYouKnowPanel == null)
        {
            Debug.LogError("[DidYouKnow Debug] didYouKnowPanel reference is null! Attempting to find it...");
            didYouKnowPanel = GameObject.Find("DidYouKnowPanel");
            
            if (didYouKnowPanel == null)
            {
                Debug.LogError("[DidYouKnow Debug] CRITICAL: DidYouKnowPanel not found in scene! Check scene hierarchy.");
                return;
            }
            Debug.Log("[DidYouKnow Debug] Successfully found DidYouKnowPanel in scene");
        }
        
        if (didYouKnowText == null)
        {
            Debug.LogError("[DidYouKnow Debug] didYouKnowText reference is null! Attempting to find it...");
            didYouKnowText = didYouKnowPanel.GetComponentInChildren<TextMeshProUGUI>();
            
            if (didYouKnowText == null)
            {
                Debug.LogError("[DidYouKnow Debug] CRITICAL: TextMeshProUGUI component not found in DidYouKnowPanel! Check panel hierarchy.");
                return;
            }
            Debug.Log("[DidYouKnow Debug] Successfully found TextMeshProUGUI component in DidYouKnowPanel");
        }
        
        // Ensure the panel is initially inactive
        if (didYouKnowPanel.activeSelf)
        {
            Debug.Log("[DidYouKnow Debug] Panel was active, deactivating it initially");
            didYouKnowPanel.SetActive(false);
        }
        
        // Check for and setup the OkayButton
        Transform okayButtonTransform = didYouKnowPanel.transform.Find("OkayButton");
        if (okayButtonTransform != null)
        {
            Button okayButton = okayButtonTransform.GetComponent<Button>();
            if (okayButton != null)
            {
                // Remove any existing listeners to avoid duplicates
                okayButton.onClick.RemoveAllListeners();
                
                // Add the close panel listener
                okayButton.onClick.AddListener(OnCloseDidYouKnowPanel);
                Debug.Log("[DidYouKnow Debug] Successfully set up OkayButton click listener");
            }
            else
            {
                Debug.LogError("[DidYouKnow Debug] OkayButton found but has no Button component!");
            }
        }
        else
        {
            Debug.LogError("[DidYouKnow Debug] OkayButton not found in DidYouKnowPanel!");
        }
        
        // Check for CanvasGroup and ensure it's properly set up
        CanvasGroup canvasGroup = didYouKnowPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log("[DidYouKnow Debug] CanvasGroup properties set to visible and interactive");
        }
        
        Debug.Log("[DidYouKnow Debug] Did You Know panel initialization complete");
    }

    // Helper method to refresh the current word's fact when language changes
    private void RefreshCurrentWordFact()
    {
        Debug.Log($"[Android Debug] Refreshing fact for word: {currentBaseWord} in language: {GameManager.Instance.CurrentLanguage}");
        
        // Get the word in the current language
        string translatedWord = GameManager.Instance.GetTranslation(currentBaseWord, GameManager.Instance.CurrentLanguage);
        
        // Verify we have a fact for this word
        string fact = WordValidator.GetFactForWord(translatedWord, GameManager.Instance.CurrentEra, GameManager.Instance.CurrentLanguage);
        
        if (!string.IsNullOrEmpty(fact))
        {
            Debug.Log($"[Android Debug] Found fact for {translatedWord}: {fact}");
            
            // Display the fact if the DidYouKnow panel is active
            if (didYouKnowPanel != null && didYouKnowPanel.activeSelf)
            {
                if (didYouKnowText != null)
                {
                    string language = GameManager.Instance.CurrentLanguage;
                    string titleText = language == "tr" ? "BİLİYOR MUYDUNUZ?" : "DID YOU KNOW?";
                    didYouKnowText.text = titleText + "\n\n" + fact;
                    Debug.Log($"[Android Debug] Updated fact in DidYouKnow panel: {fact}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[Android Debug] No fact found for {translatedWord} in {GameManager.Instance.CurrentEra}");
        }
    }

    private void ProcessSolvedWords()
    {
        try
        {
            Debug.Log($"[Android Debug] ProcessSolvedWords: Platform={Application.platform}, Era={GameManager.Instance.CurrentEra}, Language={GameManager.Instance.CurrentLanguage}");
            
            // If GridManager is not available, do nothing
            if (GridManager.Instance == null)
            {
                Debug.LogWarning("[Android Debug] GridManager is null, skipping processing solved words");
                return;
            }
            
            // Set up the grid for the current word
            if (currentWordIndex < currentEraWords.Count)
            {
                Debug.Log($"[Android Debug] Loading word at index {currentWordIndex}: {(currentWordIndex < currentEraWords.Count ? currentEraWords[currentWordIndex] : "INVALID")}");
                LoadWord(currentWordIndex);
                
                // Now go through all solved words and mark their tiles as solved
                HashSet<string> solvedBaseWords = GameManager.Instance.GetSolvedBaseWordsForEra(GameManager.Instance.CurrentEra);
                Debug.Log($"[Android Debug] Found {solvedBaseWords.Count} solved base words for era {GameManager.Instance.CurrentEra}: {string.Join(", ", solvedBaseWords)}");
                
                // Log all solved base words across all eras for debugging
                Debug.Log("[Android Debug] All solved base words by era:");
                
                // Use public methods to get all solved base words for logging
                foreach (var era in GameManager.Instance.EraList)
                {
                    HashSet<string> eraSolvedWords = GameManager.Instance.GetSolvedBaseWordsForEra(era);
                    Debug.Log($"[Android Debug] Era '{era}': {string.Join(", ", eraSolvedWords)}");
                }
                
                // For each solved base word, mark the current language version as solved
                foreach (string baseWord in solvedBaseWords)
                {
                    // Get the current language version of this base word
                    string translatedWord = GameManager.Instance.GetTranslation(baseWord, GameManager.Instance.CurrentLanguage);
                    Debug.Log($"[Android Debug] Processing solved base word: '{baseWord}', translation to {GameManager.Instance.CurrentLanguage}: '{translatedWord}'");
                    
                    // Debug log all words in currentEraWords for comparison
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        Debug.Log($"[Android Debug] All words in current era ({currentEraWords.Count} words):");
                        for (int i = 0; i < Math.Min(currentEraWords.Count, 20); i++) // Log up to 20 words to avoid flooding
                        {
                            Debug.Log($"[Android Debug] Word {i}: '{currentEraWords[i]}'");
                        }
                    }
                    
                    // Check if this translated word exists in the current era's words
                    int wordIndex = -1;
                    for (int i = 0; i < currentEraWords.Count; i++)
                    {
                        // Use case-insensitive comparison
                        if (string.Equals(currentEraWords[i], translatedWord, StringComparison.OrdinalIgnoreCase))
                        {
                            wordIndex = i;
                            Debug.Log($"[Android Debug] Found match for '{translatedWord}' at index {i}");
                            break;
                        }
                    }
                    
                    // If wordIndex is still -1, try with a direct uppercase comparison (for Android compatibility)
                    if (wordIndex == -1)
                    {
                        string upperTranslatedWord = translatedWord.ToUpper();
                        for (int i = 0; i < currentEraWords.Count; i++)
                        {
                            if (currentEraWords[i].ToUpper() == upperTranslatedWord)
                            {
                                wordIndex = i;
                                Debug.Log($"[Android Debug] Found match using uppercase for '{translatedWord}' at index {i}");
                                break;
                            }
                        }
                    }
                    
                    if (wordIndex != -1)
                    {
                        // Add this word to the solved words collection if it's not already there
                        if (!solvedWordsInCurrentEra.Contains(wordIndex))
                        {
                            solvedWordsInCurrentEra.Add(wordIndex);
                            Debug.Log($"[Android Debug] Added translated word '{translatedWord}' (index {wordIndex}) to solved words");
                        }
                        
                        // If this is the current word being displayed, mark its tiles as solved
                        if (string.Equals(translatedWord, targetWord, StringComparison.OrdinalIgnoreCase) || 
                            translatedWord.ToUpper() == targetWord.ToUpper()) // Additional check for Android
                        {
                            Debug.Log($"[Android Debug] Current word '{targetWord}' is solved in base language, marking tiles");
                            
                            // Get the solved positions for this word
                            List<Vector2Int> solvedPositions = GameManager.Instance.GetSolvedWordPositions(translatedWord);
                            
                            if (solvedPositions == null || solvedPositions.Count == 0)
                            {
                                // Try to get positions using the base word instead
                                solvedPositions = GameManager.Instance.GetSolvedWordPositions(baseWord);
                                Debug.Log($"[Android Debug] Using positions from base word '{baseWord}' instead: {(solvedPositions != null ? solvedPositions.Count : 0)} positions");
                                
                                // Try with uppercase versions as well (for Android)
                                if (solvedPositions == null || solvedPositions.Count == 0)
                                {
                                    solvedPositions = GameManager.Instance.GetSolvedWordPositions(translatedWord.ToUpper());
                                    Debug.Log($"[Android Debug] Trying uppercase: GetSolvedWordPositions for '{translatedWord.ToUpper()}' returned: {(solvedPositions != null ? solvedPositions.Count : 0)} positions");
                                }
                                
                                // Try with both English and Turkish translations
                                if (solvedPositions == null || solvedPositions.Count == 0)
                                {
                                    string enWord = GameManager.Instance.GetTranslation(baseWord, "en");
                                    solvedPositions = GameManager.Instance.GetSolvedWordPositions(enWord);
                                    Debug.Log($"[Android Debug] Trying English: GetSolvedWordPositions for '{enWord}' returned: {(solvedPositions != null ? solvedPositions.Count : 0)} positions");
                                }
                                
                                if (solvedPositions == null || solvedPositions.Count == 0)
                                {
                                    string trWord = GameManager.Instance.GetTranslation(baseWord, "tr");
                                    solvedPositions = GameManager.Instance.GetSolvedWordPositions(trWord);
                                    Debug.Log($"[Android Debug] Trying Turkish: GetSolvedWordPositions for '{trWord}' returned: {(solvedPositions != null ? solvedPositions.Count : 0)} positions");
                                }
                            }
                            
                            // Mark the tiles as solved
                            if (solvedPositions != null && solvedPositions.Count > 0)
                            {
                                SetTilesSolvedByPositions(solvedPositions);
                            }
                            else
                            {
                                Debug.LogWarning($"[Android Debug] No solved positions found for '{translatedWord}' or '{baseWord}'");
                                
                                // ANDROID WORKAROUND: If we can't find the positions but we know the word is solved,
                                // try to deduce the positions from the current grid if possible
                                if (Application.platform == RuntimePlatform.Android)
                                {
                                    Debug.Log("[Android Debug] Using ANDROID WORKAROUND to mark word as solved");
                                    AttemptToMarkWordInCurrentGrid(targetWord);
                                }
                            }
                        }
                    }
                    else
                    {
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

    public void VibrateDevice(){
        HapticFeedback.LightFeedback();
    }
}
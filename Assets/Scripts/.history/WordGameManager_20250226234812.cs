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
    [SerializeField] private TextMeshProUGUI pointsText;
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
    private TextMeshProUGUI pointText;
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

        // Check if navigation buttons are assigned
        if (nextQuestionButton == null || prevQuestionButton == null)
        {
            Debug.Log("[Android Debug] Navigation buttons not assigned in inspector, will try to find them in InitializeUI");
        }
        
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
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            GameManager.Instance.OnEraChanged += HandleEraChanged; // Subscribe to era change event
        }
        Debug.Log("WordGameManager enabled");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            GameManager.Instance.OnEraChanged -= HandleEraChanged; // Unsubscribe from era change event
        }
        Debug.Log("WordGameManager disabled");
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
        Debug.Log("=== SHOW AD CALLED ===");
        Debug.Log($"Words guessed count: {wordsGuessedCount}, WORDS_BEFORE_AD: {WORDS_BEFORE_AD}");

        // If no ads purchased, just reset the counter
        if (GameManager.Instance.NoAdsBought)
        {
            Debug.Log("No Ads purchased - skipping ad");
            wordsGuessedCount = 0;
            return;
        }

        // Show the ad if AdManager is available
        if (AdManager.Instance != null)
        {
            try
            {
                Debug.Log("WordGameManager: Showing interstitial ad...");
                
                // Always reset the counter when we attempt to show an ad
                wordsGuessedCount = 0;
                
                // Show the ad
                AdManager.Instance.ShowInterstitialAd();
                Debug.Log("Ad request sent to AdManager");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing ad: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("AdManager instance not found!");
            wordsGuessedCount = 0; // Reset counter even if AdManager is not found
        }
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
        
        Debug.Log($"LoadWord: Original={originalWord}, Base={currentBaseWord}, Translated={targetWord}");
        
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
        Debug.Log($"Word {currentBaseWord} is solved: {isSolved}, isCurrentWordNewlySolved: {isCurrentWordNewlySolved}");
        
        if (isSolved && !isCurrentWordNewlySolved)
        {
            Debug.Log($"Word {currentBaseWord} is already solved (not newly solved), showing Did You Know fact for {targetWord} instantly");
            // Show the "Did You Know" fact immediately without delay for already solved words
            ShowDidYouKnowInstantly();
            
            // Update the hint button text to show "Solved" in black
            if (hintButtonText != null)
            {
                hintButtonText.text = "Solved";
                hintButtonText.color = Color.black;
            }
        }
        else
        {
            didYouKnowPanel.SetActive(false);
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
                if (hintButtonText != null)
                {
                    hintButtonText.text = GameManager.Instance.CurrentLanguage == "tr" ? "Çözüldü" : "Solved";
                    hintButtonText.color = Color.black;
                }
            
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
            
            // Store positions for the current word
            GameManager.Instance.StoreSolvedWordPositions(word, positions);
            
            // Store positions for ALL language versions of the word
            foreach (string language in new[] { "en", "tr" })
            {
                string translatedWord = GameManager.Instance.GetTranslation(currentBaseWord, language);
                GameManager.Instance.StoreSolvedWordPositions(translatedWord, positions);
            }
            
            // Store the base word for cross-language support
            GameManager.Instance.StoreSolvedBaseWord(GameManager.Instance.CurrentEra, currentBaseWord);
            
            // Store the word index
            StoreSolvedWordIndex(word);
            
            // Trigger word guessed event for saving
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerWordGuessed(word);
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
        
        // LANGUAGE-SPECIFIC INDEX HANDLING
        // Only store the index for the CURRENT language
        if (GameManager.Instance.CurrentLanguage == "tr")
        {
            // For Turkish, store in a Turkish-specific index collection
            solvedWordsInCurrentEra.Add(currentWordIndex);
            // Store with language identifier to avoid conflicts
            GameManager.Instance.StoreSolvedWordIndex(GameManager.Instance.CurrentEra + "_tr", currentWordIndex);
        }
        else
        {
            // For English, store in an English-specific index collection
            solvedWordsInCurrentEra.Add(currentWordIndex);
            // Store with language identifier to avoid conflicts
            GameManager.Instance.StoreSolvedWordIndex(GameManager.Instance.CurrentEra + "_en", currentWordIndex);
        }
        
        // Store the base word for cross-language support - this is the key part that works across languages
        GameManager.Instance.StoreSolvedBaseWord(GameManager.Instance.CurrentEra, baseWord);
        
        // Store the positions for both the current word and its translation
        List<Vector2Int> positions = GridManager.Instance.GetSelectedTiles().Select(t => t.GetGridPosition()).ToList();
        GameManager.Instance.StoreSolvedWordPositions(word, positions);
        
        // Store positions for ALL language versions of the word
        foreach (string language in new[] { "en", "tr" })
        {
            string translatedWord = GameManager.Instance.GetTranslation(baseWord, language);
            if (translatedWord != word)
            {
                GameManager.Instance.StoreSolvedWordPositions(translatedWord, positions);
            }
        }
    }

    private void HandleLanguageChanged()
    {
        Debug.Log("Language changed, updating grid...");
        
        // Simply reload the current word - LoadWord will handle the translation
        LoadWord(currentWordIndex);
        
        // Update UI
        UpdateProgressBar();
        
        // Make sure to update the hint button
        UpdateHintButton();
        
        // We're no longer highlighting solved words on the grid
        // So we don't need to call UpdateSolvedWordsDisplay
        /*
        // Update the grid display to show solved words in the new language
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UpdateSolvedWordsDisplay();
        }
        */
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
        Debug.Log($"[Android Debug] ShowDidYouKnow called for word: {targetWord} in language: {GameManager.Instance?.CurrentLanguage ?? "language unknown"}");
        
        // First, ensure the panel reference exists
        if (didYouKnowPanel == null)
        {
            Debug.LogError("[Android Debug] didYouKnowPanel is null! Attempting to find it in the scene...");
            didYouKnowPanel = GameObject.Find("DidYouKnowPanel");
            
            if (didYouKnowPanel == null)
            {
                Debug.LogError("[Android Debug] Could not find DidYouKnowPanel in the scene! Panel can't be shown.");
                return;
            }
            else
            {
                Debug.Log("[Android Debug] Successfully found DidYouKnowPanel in the scene");
            }
        }
        
        // Also check for TextMeshProUGUI reference
        if (didYouKnowText == null)
        {
            Debug.LogError("[Android Debug] didYouKnowText is null! Attempting to find it...");
            didYouKnowText = didYouKnowPanel.GetComponentInChildren<TextMeshProUGUI>();
            
            if (didYouKnowText == null)
            {
                Debug.LogError("[Android Debug] Could not find TextMeshProUGUI in DidYouKnowPanel!");
                return;
            }
            else
            {
                Debug.Log("[Android Debug] Successfully found TextMeshProUGUI in DidYouKnowPanel");
            }
        }
        
        // Make sure we have a valid GameManager instance
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Android Debug] GameManager.Instance is null! Cannot show didYouKnow panel.");
            return;
        }
        
        try
        {
            // Check if the current word is solved
            bool isWordSolved = IsWordSolved(targetWord);
            Debug.Log($"[Android Debug] Is word '{targetWord}' solved: {isWordSolved}");
            
            // If the word is not solved, hide the panel and return
            if (!isWordSolved)
            {
                Debug.Log($"[Android Debug] Word '{targetWord}' is not solved, hiding Did You Know panel");
                didYouKnowPanel.SetActive(false);
                return;
            }
            
            // Get the current language
            string language = GameManager.Instance.CurrentLanguage;
            
            // Let GetFactForWord handle the era translation based on language
            Debug.Log($"[Android Debug] Getting fact for word: {targetWord}, era: {GameManager.Instance.CurrentEra}, language: {language}");
            string fact = WordValidator.GetFactForWord(targetWord, GameManager.Instance.CurrentEra, language);
            Debug.Log($"[Android Debug] Got fact: {(fact == "LOADING" ? "LOADING" : (string.IsNullOrEmpty(fact) ? "EMPTY" : "SUCCESS"))}");
            
            if (!string.IsNullOrEmpty(fact) && fact != "LOADING")
            {
                // Make sure the OkayButton is active for newly solved words
                Transform okayButtonTransform = didYouKnowPanel.transform.Find("OkayButton");
                if (okayButtonTransform != null)
                {
                    okayButtonTransform.gameObject.SetActive(true);
                    Debug.Log("[Android Debug] OkayButton set to active in ShowDidYouKnow for newly solved word");
                }
                else
                {
                    Debug.LogError("[Android Debug] OkayButton not found in didYouKnowPanel");
                }
                
                StartCoroutine(ShowDidYouKnowCoroutine(fact));
            }
            else
            {
                Debug.Log("[Android Debug] Facts are still loading, will retry in 1 second");
                Invoke(nameof(ShowDidYouKnow), 1f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error in ShowDidYouKnow: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Shows the Did You Know panel immediately without any delay.
    /// This method is used for already solved words to display the fact instantly.
    /// </summary>
    public void ShowDidYouKnowInstantly()
    {
        Debug.Log($"[Android Debug] ShowDidYouKnowInstantly called for word: {targetWord} in language: {GameManager.Instance.CurrentLanguage}");
        
        if (didYouKnowPanel == null)
        {
            Debug.LogError("[Android Debug] didYouKnowPanel is not assigned!");
            return;
        }
        
        // Check if the current word is solved
        bool isWordSolved = IsWordSolved(targetWord);
        Debug.Log($"[Android Debug] Is word '{targetWord}' solved: {isWordSolved}");
        
        // If the word is not solved, hide the panel and return
        if (!isWordSolved)
        {
            Debug.Log($"[Android Debug] Word '{targetWord}' is not solved, hiding Did You Know panel");
            didYouKnowPanel.SetActive(false);
            return;
        }
        
        // Get the current language
        string language = GameManager.Instance.CurrentLanguage;
        
        // Let GetFactForWord handle the era translation based on language
        string fact = WordValidator.GetFactForWord(targetWord, GameManager.Instance.CurrentEra, language);
        
        if (!string.IsNullOrEmpty(fact) && fact != "LOADING")
        {
            didYouKnowPanel.SetActive(true);
            
            // Set the text based on language
            if (language == "tr")
            {
                didYouKnowText.text = $"Biliyor muydun?\n\n{fact}";
            }
            else
            {
                didYouKnowText.text = $"Did you know?\n\n{fact}";
            }
            
            // Ensure the OkayButton is set to inactive
            Transform okayButtonTransform = didYouKnowPanel.transform.Find("OkayButton");
            if (okayButtonTransform != null)
            {
                okayButtonTransform.gameObject.SetActive(false);
                Debug.Log("[Android Debug] OkayButton set to inactive in ShowDidYouKnowInstantly");
            }
            else
            {
                Debug.LogError("[Android Debug] OkayButton not found in didYouKnowPanel");
            }
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, ensure the panel is positioned within the safe area
            if (safeArea != null)
            {
                Rect safeArea = Screen.safeArea;
                RectTransform panelRect = didYouKnowPanel.GetComponent<RectTransform>();
                
                Canvas canvas = didYouKnowPanel.GetComponentInParent<Canvas>();
                
                if (canvas != null)
                {
                    // Convert safe area to canvas space
                    Vector2 canvasSize = canvas.GetComponent<RectTransform>().sizeDelta;
                    float scaleX = canvasSize.x / Screen.width;
                    float scaleY = canvasSize.y / Screen.height;
                    
                    // Center the panel in the safe area
                    float safeAreaCenterX = safeArea.center.x * scaleX;
                    float safeAreaCenterY = safeArea.center.y * scaleY;
                    
                    panelRect.anchoredPosition = new Vector2(safeAreaCenterX, safeAreaCenterY);
                }
            }
            #endif
            
            // Disable navigation buttons while the panel is active
            //DisableNavigationButtons();
        }
        else
        {
            Debug.Log("[Android Debug] Facts are still loading, will retry in 1 second");
            Invoke(nameof(ShowDidYouKnowInstantly), 1f);
        }
    }
    
    private IEnumerator ShowDidYouKnowCoroutine(string fact)
    {
        Debug.Log("[Android Debug] ShowDidYouKnowCoroutine started");
        
        // Wait for animations outside of try-catch
        yield return new WaitForSeconds(0.5f);
        
        try
        {
            // Double check panel exists
            if (didYouKnowPanel == null)
            {
                Debug.LogError("[Android Debug] didYouKnowPanel is null in coroutine! Attempting to find it...");
                didYouKnowPanel = GameObject.Find("DidYouKnowPanel");
                if (didYouKnowPanel == null)
                {
                    Debug.LogError("[Android Debug] Could not find DidYouKnowPanel in coroutine!");
                    yield break;
                }
            }
            
            // Double check text component exists
            if (didYouKnowText == null)
            {
                Debug.LogError("[Android Debug] didYouKnowText is null in coroutine! Attempting to find it...");
                didYouKnowText = didYouKnowPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (didYouKnowText == null)
                {
                    Debug.LogError("[Android Debug] Could not find TextMeshProUGUI in coroutine!");
                    yield break;
                }
            }
            
            Debug.Log("[Android Debug] About to check if word is solved in coroutine");
            
            // Safety check - is word still solved (in case something changed?)
            if (!IsWordSolved(targetWord))
            {
                Debug.Log($"[Android Debug] Word '{targetWord}' is no longer solved in coroutine, hiding panel");
                didYouKnowPanel.SetActive(false);
                yield break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error checking panel references: {e.Message}\n{e.StackTrace}");
            yield break;
        }
        
        // If fact is "LOADING", retry for longer (up to 15 seconds)
        int retryCount = 0;
        const int MAX_RETRIES = 15;  // Increased from 10 to 15
        
        while (fact == "LOADING" && retryCount < MAX_RETRIES)
        {
            Debug.Log($"[Android Debug] Fact is still loading. Retry attempt {retryCount + 1}/{MAX_RETRIES}");
            
            // Yield outside of try-catch
            yield return new WaitForSeconds(1f);
            
            string language = "en";
            bool shouldContinue = true;
            
            try
            {
                if (GameManager.Instance != null)
                {
                    language = GameManager.Instance.CurrentLanguage;
                    fact = WordValidator.GetFactForWord(targetWord, GameManager.Instance.CurrentEra, language);
                    Debug.Log($"[Android Debug] Retry {retryCount + 1}: Got fact: {(fact == "LOADING" ? "LOADING" : (string.IsNullOrEmpty(fact) ? "EMPTY" : "SUCCESS"))}");
                }
                else
                {
                    Debug.LogError("[Android Debug] GameManager.Instance is null during retry!");
                    shouldContinue = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Android Debug] Error during fact retry: {e.Message}\n{e.StackTrace}");
                shouldContinue = false;
            }
            
            if (!shouldContinue)
                break;
                
            retryCount++;
        }
        
        if (fact == "LOADING" || string.IsNullOrEmpty(fact))
        {
            Debug.LogError($"[Android Debug] Failed to load fact after {retryCount} retries. fact = {fact}");
            yield break;
        }
        
        Debug.Log($"[Android Debug] Successfully loaded fact: {fact.Substring(0, Math.Min(30, fact.Length))}...");
        
        try
        {
            // Set the panel title based on language
            string title = GameManager.Instance?.CurrentLanguage == "tr" ? "Biliyor Muydunuz:" : "Did You Know:";
            didYouKnowText.text = $"<b>{title}</b>\n{fact}";
            
            // Position panel in safe area on Android
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    PositionPanelInSafeArea();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Android Debug] Error positioning panel: {e.Message}");
                }
            }
            
            // Activate the panel
            didYouKnowPanel.SetActive(true);
            Debug.Log("[Android Debug] DidYouKnowPanel activated successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error setting up panel: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void PositionPanelInSafeArea()
    {
        if (didYouKnowPanel == null) return;
        
        try
        {
            Debug.Log("[Android Debug] Positioning panel in safe area");
            
            // Get the safe area
            Rect safeArea = Screen.safeArea;
            RectTransform rectTransform = didYouKnowPanel.GetComponent<RectTransform>();
            
            if (rectTransform != null)
            {
                // Calculate a position that's within the safe area
                // Position it near the middle-bottom of the screen but within safe area
                Vector2 centerPosition = safeArea.center;
                float yOffset = safeArea.height * 0.3f;  // Position it 30% up from the bottom of safe area
                
                // Set the position
                rectTransform.position = new Vector3(centerPosition.x, safeArea.yMin + yOffset, rectTransform.position.z);
                Debug.Log($"[Android Debug] Panel positioned at: {rectTransform.position}");
            }
            else
            {
                Debug.LogError("[Android Debug] didYouKnowPanel doesn't have a RectTransform component");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error in PositionPanelInSafeArea: {e.Message}");
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
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("Unity Ads not initialized when trying to show ad. Attempting to initialize...");
            
            if (AdManager.Instance != null && AdManager.Instance.GetComponent<InterstitialAdExample>() != null)
            {
                AdManager.Instance.GetComponent<InterstitialAdExample>().Initialize();
                Debug.Log("Requested Unity Ads initialization");
                return false; // Return false as ads are not ready yet
            }
            else
            {
                Debug.LogError("Cannot initialize Unity Ads - AdManager or InterstitialAdExample not found");
                return false;
            }
        }
        
        return true; // Ads are initialized
    }

    // Handle era change event
    private void HandleEraChanged()
    {
        Debug.Log("Era changed, updating solved words display");
        
        // Reset the newly solved flag when changing eras
        isCurrentWordNewlySolved = false;
        
        // Reset current word index and load the first word for the new era
        currentWordIndex = 0;
        
        // Start a new game in the current era
        StartNewGameInEra();
        
        // We're no longer highlighting solved words on the grid
        /*
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UpdateSolvedWordsDisplay();
        }
        */
        
        // Update hint button to show solved status
        UpdateHintButton();
    }
}
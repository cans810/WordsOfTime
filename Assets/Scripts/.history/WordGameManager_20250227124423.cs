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
    [SerializeField] private GameObject okayButton; // Reference to the OK button

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

        // Initialize solvedWords
        if (solvedWords == null)
        {
            solvedWords = new HashSet<string>();
            Debug.Log("[Android Debug] Initialized solvedWords HashSet");
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

        // Load solved words from save data
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null && SaveManager.Instance.Data.solvedWords != null)
        {
            solvedWords = new HashSet<string>(SaveManager.Instance.Data.solvedWords);
            Debug.Log($"Loaded {solvedWords.Count} solved words from save data: {string.Join(", ", solvedWords)}");
            
            // Synchronize with GameManager's guessed words
            SynchronizeSolvedWords();
        }

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
            
            // Load solved words from save data
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null && SaveManager.Instance.Data.solvedWords != null)
            {
                solvedWords = new HashSet<string>(SaveManager.Instance.Data.solvedWords);
                Debug.Log($"[OnSceneLoaded] Loaded {solvedWords.Count} solved words from save data");
                
                // Synchronize with GameManager's guessed words
                SynchronizeSolvedWords();
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
        Debug.Log($"[Language Debug] Updating progress bar. Total solved base words: {allSolvedBaseWords.Count}");
        
        // Also check our local solvedWords collection
        Debug.Log($"[Language Debug] Local solvedWords count: {solvedWords.Count}");

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
                // Check multiple sources to determine if the word is solved
                
                // 1. Check if the index is in solvedWordsInCurrentEra
                bool indexSolved = solvedWordsInCurrentEra.Contains(i);
                
                // 2. Get the base word and check if it's in allSolvedBaseWords
                string baseWord = GameManager.Instance.GetBaseWord(wordAtIndex);
                bool baseSolved = allSolvedBaseWords.Contains(baseWord);
                
                // 3. Check if the word itself is in solvedWords
                bool directlySolved = solvedWords.Contains(wordAtIndex);
                
                // 4. Check if the word is guessed in GameManager
                bool isGuessed = GameManager.Instance.IsWordGuessed(wordAtIndex);
                
                // Consider the word solved if any of these conditions are true
                isSolved = indexSolved || baseSolved || directlySolved || isGuessed;
                
                Debug.Log($"[Language Debug] Word at index {i}: '{wordAtIndex}', base: '{baseWord}', " +
                          $"indexSolved: {indexSolved}, baseSolved: {baseSolved}, " +
                          $"directlySolved: {directlySolved}, isGuessed: {isGuessed}, " +
                          $"FINAL: {isSolved}");
                
                // If the word is solved but not in our collections, add it
                if (isSolved)
                {
                    if (!solvedWords.Contains(wordAtIndex))
                    {
                        solvedWords.Add(wordAtIndex);
                        Debug.Log($"[Language Debug] Added '{wordAtIndex}' to solvedWords during progress bar update");
                    }
                    
                    if (!solvedWordsInCurrentEra.Contains(i))
                    {
                        solvedWordsInCurrentEra.Add(i);
                        Debug.Log($"[Language Debug] Added index {i} to solvedWordsInCurrentEra during progress bar update");
                    }
                    
                    if (!GameManager.Instance.IsWordGuessed(wordAtIndex))
                    {
                        GameManager.Instance.AddGuessedWord(wordAtIndex);
                        Debug.Log($"[Language Debug] Added '{wordAtIndex}' to GameManager's guessed words during progress bar update");
                    }
                }
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
        
        // Save any changes we made
        SaveSolvedWords();
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
            Debug.Log($"[Language Debug] UpdateSentenceDisplay for word: {word ?? targetWord}, original sentence: {originalSentence}");
            
            // First, convert all "_____" to "..."
            string displaySentence = originalSentence.Replace("_____", "...");
            
            // Then, only if we have a target word and it's in the original sentence
            if (targetWord != null && originalSentence.Contains("_____"))
            {
                // Find the position of "_____" in the original sentence
                int blankPos = originalSentence.IndexOf("_____");
                
                // Check multiple sources to determine if the word is solved
                bool isWordSolved = IsWordSolved(targetWord) || 
                                   GameManager.Instance.IsWordGuessed(targetWord) || 
                                   solvedWordsInCurrentEra.Contains(currentWordIndex);
                
                Debug.Log($"[Language Debug] Is word '{targetWord}' solved for sentence display: {isWordSolved}");
                
                if (isWordSolved)
                {
                    // Show solved word
                    displaySentence = displaySentence.Substring(0, blankPos) + 
                                    targetWord + 
                                    displaySentence.Substring(blankPos + 3);
                    Debug.Log($"[Language Debug] Showing solved word '{targetWord}' in sentence");
                    
                    // Also mark the word as guessed in GameManager if it's not already
                    if (!GameManager.Instance.IsWordGuessed(targetWord))
                    {
                        GameManager.Instance.AddGuessedWord(targetWord);
                        Debug.Log($"[Language Debug] Added '{targetWord}' to GameManager's guessed words during sentence update");
                    }
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
                                    displaySentence.Substring(blankPos + 3);
                }
            }
            
            sentenceText.text = displaySentence;
            Debug.Log($"[Language Debug] Set sentence text to: {displaySentence}");
        }
        else
        {
            Debug.LogWarning("[Language Debug] Cannot update sentence display: sentenceText is null or originalSentence is empty");
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
        
        // First check if the word itself is in the solved words list
        bool directlySolved = solvedWords.Contains(word);
        
        if (directlySolved)
        {
            Debug.Log($"[Language Debug] Word '{word}' is directly in solvedWords");
            return true;
        }
        
        // If not directly solved, check if the base word (English version) is solved
        string baseWord = GameManager.Instance.GetBaseWord(word);
        bool baseSolved = baseWord != word && solvedWords.Contains(baseWord);
        
        if (baseSolved)
        {
            Debug.Log($"[Language Debug] Base word '{baseWord}' for '{word}' is in solvedWords");
            return true;
        }
        
        // Check if the word in the other language is solved
        string otherLanguage = GameManager.Instance.CurrentLanguage == "tr" ? "en" : "tr";
        string translatedWord = GameManager.Instance.GetTranslation(baseWord, otherLanguage);
        bool translationSolved = !string.IsNullOrEmpty(translatedWord) && 
                                translatedWord != word && 
                                solvedWords.Contains(translatedWord);
        
        if (translationSolved)
        {
            Debug.Log($"[Language Debug] Translation '{translatedWord}' of '{word}' in {otherLanguage} is in solvedWords");
            return true;
        }
        
        // Check if the word is in GameManager's solved words
        bool inGameManager = GameManager.Instance.IsWordSolved(word) || 
                            GameManager.Instance.IsWordSolved(baseWord);
        
        if (inGameManager)
        {
            Debug.Log($"[Language Debug] Word '{word}' or its base '{baseWord}' is solved in GameManager");
            return true;
        }
        
        // Finally, check if the current word index is in the solved indices for this era
        if (currentWordIndex >= 0 && currentWordIndex < currentEraWords.Count)
        {
            bool indexSolved = solvedWordsInCurrentEra.Contains(currentWordIndex);
            if (indexSolved)
            {
                Debug.Log($"[Language Debug] Word index {currentWordIndex} for '{word}' is in solvedWordsInCurrentEra");
                return true;
            }
        }
        
        Debug.Log($"[Language Debug] Word '{word}' is NOT solved by any method");
        return false;
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
        
        Debug.Log($"[Language Debug] LoadWord: Original={originalWord}, Base={currentBaseWord}, Translated={targetWord}, Index={index}");
        
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

        // Check if the word is already solved, if so highlight it in the grid
        bool isSolved = IsWordSolved(targetWord) || GameManager.Instance.IsWordSolved(currentBaseWord);
        Debug.Log($"[Language Debug] Word {targetWord} (base: {currentBaseWord}) is solved: {isSolved}, isCurrentWordNewlySolved: {isCurrentWordNewlySolved}");
        
        // Also check if the index is in the solved indices for this era
        bool isIndexSolved = solvedWordsInCurrentEra.Contains(currentWordIndex);
        Debug.Log($"[Language Debug] Index {currentWordIndex} is in solvedWordsInCurrentEra: {isIndexSolved}");
        
        // Consider the word solved if either the word itself is solved or its index is in the solved indices
        isSolved = isSolved || isIndexSolved;
        
        if (isSolved)
        {
            // If the word is solved but not in solvedWords, add it
            if (!solvedWords.Contains(targetWord))
            {
                solvedWords.Add(targetWord);
                Debug.Log($"[Language Debug] Added {targetWord} to solvedWords");
                SaveSolvedWords();
            }
            
            // Mark the word as guessed in GameManager to ensure it appears in the sentence
            if (!GameManager.Instance.IsWordGuessed(targetWord))
            {
                GameManager.Instance.AddGuessedWord(targetWord);
                Debug.Log($"[Language Debug] Added {targetWord} to GameManager's guessed words");
            }
            
            // If the word is already solved (not newly solved), show the Did You Know panel
            if (!isCurrentWordNewlySolved)
            {
                Debug.Log($"[Language Debug] Word {targetWord} is already solved (not newly solved), showing Did You Know fact instantly");
                
                // Show the "Did You Know" fact immediately without delay for already solved words
                ShowDidYouKnowInstantly();
            }
            else
            {
                Debug.Log($"[Language Debug] Word {targetWord} was just solved in this session, not showing Did You Know fact instantly");
                didYouKnowPanel.SetActive(false);
            }
            
            // Update the hint button text to show "Solved" in black
            if (hintButtonText != null)
            {
                hintButtonText.text = GameManager.Instance.CurrentLanguage == "tr" ? "ÇÖZÜLDÜ" : "SOLVED";
                hintButtonText.color = Color.black;
                Debug.Log($"[Language Debug] Set hint button text to '{hintButtonText.text}' for solved word");
            }
            
            // Also highlight the word in the grid
            if (GridManager.Instance != null)
            {
                List<Vector2Int> positions = GameManager.Instance.GetSolvedWordPositions(targetWord);
                if (positions != null && positions.Count > 0)
                {
                    GridManager.Instance.HighlightSolvedWord(positions);
                    Debug.Log($"[Language Debug] Highlighted solved word {targetWord} in grid");
                }
                else
                {
                    Debug.Log($"[Language Debug] No positions found for solved word {targetWord}");
                }
            }
        }
        else
        {
            didYouKnowPanel.SetActive(false);
            Debug.Log($"[Language Debug] Word {targetWord} is not solved yet.");
        }

        // Update UI
        UpdateProgressBar();
        UpdateSentenceDisplay();

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
            Debug.Log($"[Language Debug] UpdateHintButton: Checking if word '{targetWord}' (base: '{currentBaseWord}') is solved: {isSolved}");
            
            if (isSolved)
            {
                // Disable button and update text if word is already solved
                hintButton.interactable = false;
                if (hintButtonText != null)
                {
                    // Make sure to use uppercase for Turkish to match the style
                    hintButtonText.text = GameManager.Instance.CurrentLanguage == "tr" ? "ÇÖZÜLDÜ" : "SOLVED";
                    hintButtonText.color = Color.black;
                    Debug.Log($"[Language Debug] Set hint button text to '{hintButtonText.text}' for solved word");
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
                    hintButtonText.text = GameManager.Instance.CurrentLanguage == "tr" ? "0 İPUCU" : "0 HINTS";
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
                Debug.Log($"[Language Debug] Set hint button text to '{hintButtonText.text}' with color {hintButtonText.color}");
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
        Debug.Log($"[Language Debug] StoreSolvedWordIndex for word: {word}, baseWord: {baseWord}");
        
        // LANGUAGE-SPECIFIC INDEX HANDLING
        // Store the index for BOTH languages to ensure cross-language progress
        
        // Store in current language-specific index collection
        solvedWordsInCurrentEra.Add(currentWordIndex);
        
        // Store with language identifier for current language
        string currentLangKey = GameManager.Instance.CurrentEra + "_" + GameManager.Instance.CurrentLanguage;
        GameManager.Instance.StoreSolvedWordIndex(currentLangKey, currentWordIndex);
        Debug.Log($"[Language Debug] Stored index {currentWordIndex} with key: {currentLangKey}");
        
        // Also store for the other language
        string otherLang = GameManager.Instance.CurrentLanguage == "tr" ? "en" : "tr";
        string otherLangKey = GameManager.Instance.CurrentEra + "_" + otherLang;
        GameManager.Instance.StoreSolvedWordIndex(otherLangKey, currentWordIndex);
        Debug.Log($"[Language Debug] Also stored index {currentWordIndex} with key: {otherLangKey}");
        
        // Store the base word for cross-language support
        GameManager.Instance.StoreSolvedBaseWord(GameManager.Instance.CurrentEra, baseWord);
        
        // Store the positions for both the current word and its translation
        List<Vector2Int> positions = GridManager.Instance.GetSelectedTiles().Select(t => t.GetGridPosition()).ToList();
        GameManager.Instance.StoreSolvedWordPositions(word, positions);
        
        // Store positions for ALL language versions of the word
        foreach (string language in new[] { "en", "tr" })
        {
            string translatedWord = GameManager.Instance.GetTranslation(baseWord, language);
            if (!string.IsNullOrEmpty(translatedWord) && translatedWord != word)
            {
                GameManager.Instance.StoreSolvedWordPositions(translatedWord, positions);
                Debug.Log($"[Language Debug] Stored positions for translated word: {translatedWord} in {language}");
            }
        }
    }

    private void HandleLanguageChanged()
    {
        Debug.Log("[Language Debug] Language changed to: " + GameManager.Instance.CurrentLanguage);
        
        // Synchronize solved words between languages
        SynchronizeSolvedWords();
        
        // Update the solvedWordsInCurrentEra collection for the new language
        string eraKey = GameManager.Instance.CurrentEra;
        if (GameManager.Instance.CurrentLanguage == "tr")
        {
            eraKey += "_tr";
        }
        else
        {
            eraKey += "_en";
        }
        
        // Get the solved words for the current language
        HashSet<int> languageSpecificSolvedWords = GameManager.Instance.GetSolvedWordsForEra(eraKey);
        Debug.Log($"[Language Debug] Found {languageSpecificSolvedWords.Count} solved words for {eraKey}");
        
        // Merge with the current solved words
        foreach (int index in languageSpecificSolvedWords)
        {
            if (!solvedWordsInCurrentEra.Contains(index))
            {
                solvedWordsInCurrentEra.Add(index);
                Debug.Log($"[Language Debug] Added index {index} to solvedWordsInCurrentEra");
            }
        }
        
        // Also check the other language key
        string otherLangKey = GameManager.Instance.CurrentEra + "_" + (GameManager.Instance.CurrentLanguage == "tr" ? "en" : "tr");
        HashSet<int> otherLanguageSolvedWords = GameManager.Instance.GetSolvedWordsForEra(otherLangKey);
        Debug.Log($"[Language Debug] Found {otherLanguageSolvedWords.Count} solved words for {otherLangKey}");
        
        // Merge with the current solved words
        foreach (int index in otherLanguageSolvedWords)
        {
            if (!solvedWordsInCurrentEra.Contains(index))
            {
                solvedWordsInCurrentEra.Add(index);
                Debug.Log($"[Language Debug] Added index {index} from other language to solvedWordsInCurrentEra");
            }
        }
        
        Debug.Log($"[Language Debug] Total solvedWordsInCurrentEra after merging: {solvedWordsInCurrentEra.Count}");
        
        // Simply reload the current word - LoadWord will handle the translation
        LoadWord(currentWordIndex);
        
        // Update UI
        UpdateProgressBar();
        
        // Make sure to update the hint button
        UpdateHintButton();
        
        // Save the synchronized state
        SaveSolvedWords();
        
        Debug.Log("[Language Debug] Language change handling complete");
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
        try
        {
            // Check for null references
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[Android Debug] SaveManager.Instance is null, cannot save solved words");
                return;
            }
            
            if (SaveManager.Instance.Data == null)
            {
                Debug.LogError("[Android Debug] SaveManager.Instance.Data is null, cannot save solved words");
                return;
            }
            
            // Save solved words to the JSON file
            SaveManager.Instance.Data.solvedWords = solvedWords.ToList();
            SaveManager.Instance.SaveGame();
            Debug.Log($"[Android Debug] Saved {solvedWords.Count} solved words to JSON: {string.Join(", ", solvedWords)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error saving solved words: {e.Message}\n{e.StackTrace}");
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[Android Debug] Application quitting, saving game state");
        
        try
        {
            // Save solved words
            SaveSolvedWords();
            
            // Also ensure GameManager saves its state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGameState();
            }
            
            Debug.Log("[Android Debug] Game state saved on quit");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Android Debug] Error saving game state on quit: {e.Message}\n{e.StackTrace}");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("[Android Debug] Application paused, saving game state");
            SaveSolvedWords();
            
            // Also ensure GameManager saves its state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGameState();
            }
            
            Debug.Log("[Android Debug] Game state saved on pause");
        }
        else
        {
            Debug.Log("[Android Debug] Application resumed");
            
            // Reload saved data when resuming
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
                
                // Reload solved words
                if (SaveManager.Instance.Data != null && SaveManager.Instance.Data.solvedWords != null)
                {
                    solvedWords = new HashSet<string>(SaveManager.Instance.Data.solvedWords);
                    Debug.Log($"[Android Debug] Reloaded {solvedWords.Count} solved words on resume");
                    
                    // Synchronize with GameManager's guessed words
                    SynchronizeSolvedWords();
                }
            }
            
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
        Debug.Log($"[DidYouKnow Debug] ShowDidYouKnow called for word: {targetWord} in era: {GameManager.Instance?.CurrentEra}, language: {GameManager.Instance?.CurrentLanguage}");
        
        // First, find and validate panel references
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
                Debug.LogError("[DidYouKnow Debug] CRITICAL: No TextMeshProUGUI component found in DidYouKnowPanel!");
                return;
            }
            Debug.Log("[DidYouKnow Debug] Successfully found didYouKnowText component");
        }
        
        // Find the OK button if not already assigned
        if (okayButton == null)
        {
            Transform okayButtonTransform = didYouKnowPanel.transform.Find("OkayButton");
            if (okayButtonTransform != null)
            {
                okayButton = okayButtonTransform.gameObject;
                Debug.Log("[DidYouKnow Debug] Found OkayButton in didYouKnowPanel");
            }
            else
            {
                Debug.LogWarning("[DidYouKnow Debug] OkayButton not found in didYouKnowPanel");
            }
        }
        
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
            
            // Get the base word first (English version) to ensure we can find facts
            string baseWord = GameManager.Instance.GetBaseWord(targetWord);
            Debug.Log($"[DidYouKnow Debug] Base word for '{targetWord}' is '{baseWord}'");
            
            // Try to get fact in current language first
            Debug.Log($"[DidYouKnow Debug] Getting fact for word: {targetWord}, era: {era}, language: {language}");
            string fact = WordValidator.GetFactForWord(targetWord, era, language);
            
            // Log the result of getting the fact
            if (fact == "LOADING")
                Debug.Log("[DidYouKnow Debug] Fact is still in LOADING state");
            else if (string.IsNullOrEmpty(fact))
            {
                Debug.LogWarning("[DidYouKnow Debug] Fact is empty or null for current word, trying with base word");
                
                // Try with base word in current language
                if (!string.IsNullOrEmpty(baseWord) && baseWord != targetWord)
                {
                    Debug.Log($"[DidYouKnow Debug] Trying with base word: {baseWord} in language: {language}");
                    fact = WordValidator.GetFactForWord(baseWord, era, language);
                }
                
                // If still no fact, try with English
                if (string.IsNullOrEmpty(fact) && language != "en")
                {
                    Debug.Log($"[DidYouKnow Debug] Still no fact, trying with English language for base word: {baseWord}");
                    fact = WordValidator.GetFactForWord(baseWord, era, "en");
                }
                
                // If still no fact, check direct JSON files
                if (string.IsNullOrEmpty(fact))
                {
                    Debug.Log($"[DidYouKnow Debug] Still no fact, checking JSON files directly for word: {targetWord}");
                    string directFact = WordValidator.CheckWordInJsonFiles(targetWord, era);
                    if (!string.IsNullOrEmpty(directFact))
                    {
                        Debug.Log($"[DidYouKnow Debug] Successfully got fact directly from JSON file: {(directFact.Length > 30 ? directFact.Substring(0, 30) + "..." : directFact)}");
                        fact = directFact;
                    }
                    else if (!string.IsNullOrEmpty(baseWord) && baseWord != targetWord)
                    {
                        Debug.Log($"[DidYouKnow Debug] Checking JSON files directly for base word: {baseWord}");
                        directFact = WordValidator.CheckWordInJsonFiles(baseWord, era);
                        if (!string.IsNullOrEmpty(directFact))
                        {
                            Debug.Log($"[DidYouKnow Debug] Successfully got fact for base word directly from JSON file: {(directFact.Length > 30 ? directFact.Substring(0, 30) + "..." : directFact)}");
                            fact = directFact;
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
                
                // Find the title TextMeshProUGUI component if it exists
                Transform titleTransform = didYouKnowPanel.transform.Find("Title");
                if (titleTransform != null)
                {
                    TextMeshProUGUI titleComponent = titleTransform.GetComponent<TextMeshProUGUI>();
                    if (titleComponent != null)
                    {
                        titleComponent.text = titleText;
                        Debug.Log($"[DidYouKnow Debug] Set title text to: {titleText}");
                    }
                }
                
                // Show OK button only if the word was just solved in this session
                if (okayButton != null)
                {
                    okayButton.SetActive(isCurrentWordNewlySolved);
                    Debug.Log($"[DidYouKnow Debug] OkayButton set to {(isCurrentWordNewlySolved ? "active" : "inactive")} based on isCurrentWordNewlySolved: {isCurrentWordNewlySolved}");
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
        Debug.Log("[DidYouKnow Debug] ShowDidYouKnowCoroutine started with fact length: " + fact.Length);
        
        // First wait for any animations to complete
        yield return new WaitForSeconds(0.5f);
        
        // Set the text content
        if (didYouKnowText == null)
        {
            Debug.LogError("[DidYouKnow Debug] didYouKnowText is null in coroutine!");
            yield break;
        }
        
        try
        {
            // Get the language-specific title
            string language = GameManager.Instance.CurrentLanguage;
            string titleText = language == "tr" ? "BİLİYOR MUYDUNUZ?" : "DID YOU KNOW?";
            
            // Find the title TextMeshProUGUI component if it exists
            Transform titleTransform = didYouKnowPanel.transform.Find("Title");
            if (titleTransform != null)
            {
                TextMeshProUGUI titleComponent = titleTransform.GetComponent<TextMeshProUGUI>();
                if (titleComponent != null)
                {
                    titleComponent.text = titleText;
                    Debug.Log($"[DidYouKnow Debug] Set title text to: {titleText}");
                }
                else
                {
                    Debug.LogError("[DidYouKnow Debug] Title TextMeshProUGUI component not found!");
                }
            }
            else
            {
                Debug.LogError("[DidYouKnow Debug] Title transform not found in didYouKnowPanel!");
            }
            
            // Add a space after the title for better formatting
            didYouKnowText.text = "\n" + fact;
            Debug.Log("[DidYouKnow Debug] Set didYouKnowText content");
            
            // Activate the panel
            didYouKnowPanel.SetActive(true);
            Debug.Log("[DidYouKnow Debug] Panel activated");
            
            // Only disable navigation buttons if this is a newly solved word
            if (isCurrentWordNewlySolved)
            {
                DisableNavigationButtons();
                Debug.Log("[DidYouKnow Debug] Navigation buttons disabled for newly solved word");
            }
            else
            {
                // For already solved words, make sure navigation is enabled
                EnableNavigationButtons();
                Debug.Log("[DidYouKnow Debug] Navigation buttons enabled for previously solved word");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DidYouKnow Debug] Error setting up didYouKnow panel: {e.Message}\n{e.StackTrace}");
            EnableNavigationButtons();
            yield break;
        }
        
        // No auto-close functionality - panel stays open until manually closed
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

    /// <summary>
    /// Shows the Did You Know panel immediately without any delay.
    /// This method is used for already solved words to display the fact instantly.
    /// </summary>
    public void ShowDidYouKnowInstantly()
    {
        Debug.Log($"[DidYouKnow Debug] ShowDidYouKnowInstantly called for word: {targetWord} in era: {GameManager.Instance?.CurrentEra}, language: {GameManager.Instance?.CurrentLanguage}");
        
        // Check for panel references
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
                Debug.LogError("[DidYouKnow Debug] CRITICAL: No TextMeshProUGUI component found in DidYouKnowPanel!");
                return;
            }
            Debug.Log("[DidYouKnow Debug] Successfully found didYouKnowText component");
        }
        
        // Find the OK button if not already assigned
        if (okayButton == null)
        {
            Transform okayButtonTransform = didYouKnowPanel.transform.Find("OkayButton");
            if (okayButtonTransform != null)
            {
                okayButton = okayButtonTransform.gameObject;
                Debug.Log("[DidYouKnow Debug] Found OkayButton in didYouKnowPanel");
            }
            else
            {
                Debug.LogWarning("[DidYouKnow Debug] OkayButton not found in didYouKnowPanel");
            }
        }
        
        try
        {
            // Log some key information to help with debugging
            Debug.Log($"[DidYouKnow Debug] didYouKnowPanel active state before check: {didYouKnowPanel.activeSelf}");
            
            // Check if the current word is solved - use our enhanced IsWordSolved method
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
            
            // Get the base word first (English version) to ensure we can find facts
            string baseWord = GameManager.Instance.GetBaseWord(targetWord);
            Debug.Log($"[DidYouKnow Debug] Base word for '{targetWord}' is '{baseWord}'");
            
            // Try to get fact in current language first
            Debug.Log($"[DidYouKnow Debug] Getting fact for word: {targetWord}, era: {era}, language: {language}");
            string fact = WordValidator.GetFactForWord(targetWord, era, language);
            
            // Log the result of getting the fact
            if (fact == "LOADING")
                Debug.Log("[DidYouKnow Debug] Fact is still in LOADING state");
            else if (string.IsNullOrEmpty(fact))
            {
                Debug.LogWarning("[DidYouKnow Debug] Fact is empty or null for current word, trying with base word");
                
                // Try with base word in current language
                if (!string.IsNullOrEmpty(baseWord) && baseWord != targetWord)
                {
                    Debug.Log($"[DidYouKnow Debug] Trying with base word: {baseWord} in language: {language}");
                    fact = WordValidator.GetFactForWord(baseWord, era, language);
                }
                
                // If still no fact, try with English
                if (string.IsNullOrEmpty(fact) && language != "en")
                {
                    Debug.Log($"[DidYouKnow Debug] Still no fact, trying with English language for base word: {baseWord}");
                    fact = WordValidator.GetFactForWord(baseWord, era, "en");
                    
                    // If we found an English fact but are in Turkish mode, we should translate the title
                    if (!string.IsNullOrEmpty(fact))
                    {
                        Debug.Log("[DidYouKnow Debug] Found English fact while in Turkish mode");
                }
                
                // If still no fact, check direct JSON files
                if (string.IsNullOrEmpty(fact))
                {
                    Debug.Log($"[DidYouKnow Debug] Still no fact, checking JSON files directly for word: {targetWord}");
                    string directFact = WordValidator.CheckWordInJsonFiles(targetWord, era);
                    if (!string.IsNullOrEmpty(directFact))
                    {
                        Debug.Log($"[DidYouKnow Debug] Successfully got fact directly from JSON file: {(directFact.Length > 30 ? directFact.Substring(0, 30) + "..." : directFact)}");
                        fact = directFact;
                    }
                    else if (!string.IsNullOrEmpty(baseWord) && baseWord != targetWord)
                    {
                        Debug.Log($"[DidYouKnow Debug] Checking JSON files directly for base word: {baseWord}");
                        directFact = WordValidator.CheckWordInJsonFiles(baseWord, era);
                        if (!string.IsNullOrEmpty(directFact))
                        {
                            Debug.Log($"[DidYouKnow Debug] Successfully got fact for base word directly from JSON file: {(directFact.Length > 30 ? directFact.Substring(0, 30) + "..." : directFact)}");
                            fact = directFact;
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
                
                // Find the title TextMeshProUGUI component if it exists
                Transform titleTransform = didYouKnowPanel.transform.Find("Title");
                if (titleTransform != null)
                {
                    TextMeshProUGUI titleComponent = titleTransform.GetComponent<TextMeshProUGUI>();
                    if (titleComponent != null)
                    {
                        titleComponent.text = titleText;
                        Debug.Log($"[DidYouKnow Debug] Set title text to: {titleText}");
                    }
                }
                
                // For already solved words (not newly solved), hide the OK button
                if (okayButton != null)
                {
                    // This is an already solved word, so hide the OK button
                    okayButton.SetActive(false);
                    Debug.Log("[DidYouKnow Debug] OkayButton hidden for already solved word");
                }
                
                // Set the fact text with a space after the title
                didYouKnowText.text = "\n\n" + fact;
                Debug.Log("[DidYouKnow Debug] Set fact text: " + (fact.Length > 30 ? fact.Substring(0, 30) + "..." : fact));
                
                // Activate the panel
                didYouKnowPanel.SetActive(true);
                Debug.Log("[DidYouKnow Debug] Panel activated instantly");
                
                // Position the panel in the safe area
                PositionPanelInSafeArea();
                
                // For already solved words, make sure navigation buttons are enabled
                EnableNavigationButtons();
                Debug.Log("[DidYouKnow Debug] Navigation buttons enabled for already solved word");
                
                // No auto-close for already solved words - panel stays open until manually closed
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

    private void PositionPanelInSafeArea()
    {
        if (didYouKnowPanel == null)
        {
            Debug.LogError("[DidYouKnow Debug] Cannot position panel in safe area: panel is null");
            return;
        }

        try
        {
            // Get the safe area
            Rect safeArea = Screen.safeArea;
            Debug.Log($"[DidYouKnow Debug] Safe area: {safeArea}, Screen size: {Screen.width}x{Screen.height}");
            
            // Get the canvas that contains the panel
            Canvas canvas = didYouKnowPanel.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DidYouKnow Debug] Panel is not in a Canvas hierarchy!");
                return;
            }
            
            // Get the RectTransform of the panel
            RectTransform panelRectTransform = didYouKnowPanel.GetComponent<RectTransform>();
            if (panelRectTransform == null)
            {
                Debug.LogError("[DidYouKnow Debug] Panel does not have a RectTransform component!");
                return;
            }
            
            // Calculate scale factor (for different resolutions)
            float scaleFactor = canvas.scaleFactor;
            
            // Calculate the safe area in canvas space
            Rect scaledSafeArea = new Rect(
                safeArea.x / scaleFactor,
                safeArea.y / scaleFactor,
                safeArea.width / scaleFactor,
                safeArea.height / scaleFactor
            );
            
            // Calculate the center position within the safe area
            Vector2 safeCenter = new Vector2(
                scaledSafeArea.x + scaledSafeArea.width * 0.5f,
                scaledSafeArea.y + scaledSafeArea.height * 0.5f
            );
            
            // Set the panel position to the center of the safe area
            panelRectTransform.anchoredPosition = safeCenter;
            
            // Make sure the panel doesn't extend outside the safe area
            Vector2 panelSize = panelRectTransform.sizeDelta;
            float maxWidth = scaledSafeArea.width * 0.9f; // Use 90% of safe area width
            float maxHeight = scaledSafeArea.height * 0.8f; // Use 80% of safe area height
            
            if (panelSize.x > maxWidth || panelSize.y > maxHeight)
            {
                // Scale down the panel if it's too large
                float widthRatio = maxWidth / panelSize.x;
                float heightRatio = maxHeight / panelSize.y;
                float minRatio = Mathf.Min(widthRatio, heightRatio);
                
                if (minRatio < 1.0f)
                {
                    panelRectTransform.sizeDelta = new Vector2(panelSize.x * minRatio, panelSize.y * minRatio);
                    Debug.Log($"[DidYouKnow Debug] Panel scaled down to fit in safe area, ratio: {minRatio}");
                }
            }
            
            Debug.Log("[DidYouKnow Debug] Panel positioned successfully in safe area");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DidYouKnow Debug] Error positioning panel in safe area: {e.Message}\n{e.StackTrace}");
        }
    }

    private void SynchronizeSolvedWords()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Android Debug] GameManager.Instance is null in SynchronizeSolvedWords");
            return;
        }
        
        // Get guessed words from GameManager
        List<string> guessedWords = GameManager.Instance.GetGuessedWords();
        Debug.Log($"[Language Debug] Synchronizing solvedWords ({solvedWords.Count}) with GameManager's guessed words ({guessedWords.Count})");
        
        // Add any guessed words to solvedWords
        foreach (string word in guessedWords)
        {
            if (!solvedWords.Contains(word))
            {
                solvedWords.Add(word);
                Debug.Log($"[Language Debug] Added missing word to solvedWords: {word}");
            }
            
            // Also add the base word (English version) to ensure cross-language compatibility
            string baseWord = GameManager.Instance.GetBaseWord(word);
            if (!string.IsNullOrEmpty(baseWord) && baseWord != word && !solvedWords.Contains(baseWord))
            {
                solvedWords.Add(baseWord);
                Debug.Log($"[Language Debug] Added base word to solvedWords: {baseWord}");
            }
            
            // Add the Turkish translation if we're in English mode
            if (GameManager.Instance.CurrentLanguage == "en")
            {
                string trWord = GameManager.Instance.GetTranslation(baseWord, "tr");
                if (!string.IsNullOrEmpty(trWord) && trWord != word && !solvedWords.Contains(trWord))
                {
                    solvedWords.Add(trWord);
                    Debug.Log($"[Language Debug] Added Turkish translation to solvedWords: {trWord}");
                }
            }
            // Add the English translation if we're in Turkish mode
            else if (GameManager.Instance.CurrentLanguage == "tr")
            {
                string enWord = GameManager.Instance.GetTranslation(baseWord, "en");
                if (!string.IsNullOrEmpty(enWord) && enWord != word && !solvedWords.Contains(enWord))
                {
                    solvedWords.Add(enWord);
                    Debug.Log($"[Language Debug] Added English translation to solvedWords: {enWord}");
                }
            }
        }
        
        // Add any solved words to GameManager's guessed words
        foreach (string word in solvedWords)
        {
            if (!guessedWords.Contains(word))
            {
                GameManager.Instance.AddGuessedWord(word);
                Debug.Log($"[Language Debug] Added missing word to GameManager's guessed words: {word}");
            }
        }
        
        Debug.Log($"[Language Debug] Synchronized solvedWords ({solvedWords.Count}) with GameManager's guessed words ({guessedWords.Count})");
        
        // Save the synchronized data
        SaveSolvedWords();
    }
}
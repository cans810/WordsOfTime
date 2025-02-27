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

    public void LoadWord(string word)
    {
        Debug.Log($"[WordGameManager] LoadWord called for word: {word}");
        
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogError("[WordGameManager] Cannot load empty word");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[WordGameManager] GameManager.Instance is null in LoadWord");
            return;
        }
        
        // Set the target word
        targetWord = word.ToLower();
        Debug.Log($"[WordGameManager] Target word set to: {targetWord}");
        
        // Clear the input fields
        ClearInputFields();
        
        // Update the UI
        UpdateUI();
        
        // If the word is already solved, preload the fact for it and show the Did You Know panel
        if (IsWordSolved(targetWord))
        {
            Debug.Log($"[WordGameManager] Word '{targetWord}' is already solved, preloading fact");
            string currentEra = GameManager.Instance.CurrentEra;
            string currentLanguage = GameManager.Instance.CurrentLanguage;
            PreloadFactForWord(targetWord, currentEra, currentLanguage);
            
            // Show the "Did You Know" fact immediately for already solved words
            Debug.Log($"[WordGameManager] Showing Did You Know panel instantly for solved word: {targetWord}");
            ShowDidYouKnowInstantly();
        }
        else
        {
            // Hide the "Did You Know" panel for unsolved words
            if (didYouKnowPanel != null)
            {
                didYouKnowPanel.SetActive(false);
                Debug.Log("[WordGameManager] Did You Know panel hidden for unsolved word");
            }
        }
    }
    
    // Overload for LoadWord that accepts an index
    public void LoadWord(int wordIndex)
    {
        Debug.Log($"[WordGameManager] LoadWord called for index: {wordIndex}");
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[WordGameManager] GameManager.Instance is null in LoadWord(int)");
            return;
        }
        
        if (currentEraWords == null || currentEraWords.Count == 0)
        {
            Debug.LogError("[WordGameManager] No words available for the current era");
            return;
        }
        
        // Ensure the index is within bounds
        if (wordIndex < 0 || wordIndex >= currentEraWords.Count)
        {
            Debug.LogWarning($"[WordGameManager] Word index {wordIndex} is out of bounds (0-{currentEraWords.Count - 1})");
            wordIndex = Mathf.Clamp(wordIndex, 0, currentEraWords.Count - 1);
        }
        
        // Update the current word index
        currentWordIndex = wordIndex;
        
        // Load the word at the specified index
        string wordToLoad = currentEraWords[wordIndex];
        Debug.Log($"[WordGameManager] Loading word at index {wordIndex}: {wordToLoad}");
        
        // Call the string version of LoadWord
        LoadWord(wordToLoad);
    }
    
    /// <summary>
    /// Preloads the fact for a word to ensure it's available for ShowDidYouKnowInstantly
    /// </summary>
    private void PreloadFactForWord(string word, string era, string language)
    {
        Debug.Log($"[DidYouKnow Debug] PreloadFactForWord called for word: '{word}', era: '{era}', language: '{language}'");
        
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogWarning("[DidYouKnow Debug] Cannot preload fact for empty word");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[DidYouKnow Debug] GameManager.Instance is null in PreloadFactForWord");
            return;
        }
        
        // Try to get the fact directly first
        string fact = WordValidator.GetFactForWord(word, era, language);
        
        // If the fact is empty or still loading, check the JSON files
        if (string.IsNullOrEmpty(fact) || fact == "LOADING")
        {
            Debug.Log($"[DidYouKnow Debug] Fact not found in memory, checking JSON files for word: '{word}', era: '{era}', language: '{language}'");
            WordValidator.CheckWordInJsonFiles(word, era, language);
            
            // Try to get the fact again after checking JSON files
            fact = WordValidator.GetFactForWord(word, era, language);
            
            // If still no fact, try with base word
            if (string.IsNullOrEmpty(fact))
            {
                string baseWord = GameManager.Instance.GetBaseWord(word);
                Debug.Log($"[DidYouKnow Debug] Trying with base word: '{baseWord}' for word: '{word}'");
                
                if (baseWord != word)
                {
                    // Check JSON files for the base word
                    WordValidator.CheckWordInJsonFiles(baseWord, era, language);
                    
                    // Try to get the fact for the base word
                    fact = WordValidator.GetFactForWord(baseWord, era, language);
                    
                    // If still no fact and language is not English, try English as fallback
                    if (string.IsNullOrEmpty(fact) && language != "en")
                    {
                        Debug.Log($"[DidYouKnow Debug] No fact found in {language}, trying English as fallback");
                        WordValidator.CheckWordInJsonFiles(baseWord, era, "en");
                        fact = WordValidator.GetFactForWord(baseWord, era, "en");
                    }
                }
            }
        }
        
        if (!string.IsNullOrEmpty(fact) && fact != "LOADING")
            Debug.Log($"[DidYouKnow Debug] Successfully preloaded fact for word: '{word}': {(fact.Length > 30 ? fact.Substring(0, 30) + "..." : fact)}");
        else
            Debug.LogWarning($"[DidYouKnow Debug] Failed to preload fact for word: '{word}'");
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
                Debug.LogError("[DidYouKnow Debug] CRITICAL: No TextMeshProUGUI component found in DidYouKnowPanel!");
                return;
            }
            Debug.Log("[DidYouKnow Debug] Successfully found didYouKnowText component");
        }
        
        // Ensure the panel is initially hidden
        didYouKnowPanel.SetActive(false);
    }

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
            
            // Get the current language and era
            string language = GameManager.Instance.CurrentLanguage;
            string era = GameManager.Instance.CurrentEra;
            
            // Add more detailed logging about the word and era
            Debug.Log($"[DidYouKnow Debug] Current word details - Word: '{targetWord}', Era: '{era}', Language: '{language}'");
            
            // Preload the fact for this word
            PreloadFactForWord(targetWord, era, language);
            
            // Get the fact for the word
            string fact = WordValidator.GetFactForWord(targetWord, era, language);
            
            // Log the result of getting the fact
            if (fact == "LOADING")
                Debug.Log("[DidYouKnow Debug] Fact is still in LOADING state");
            else if (string.IsNullOrEmpty(fact))
            {
                Debug.LogWarning("[DidYouKnow Debug] Fact is empty or null, trying with base word");
                
                // Try to get the base word (English version) and use that to get the fact
                string baseWord = GameManager.Instance.GetBaseWord(targetWord);
                Debug.Log($"[DidYouKnow Debug] Base word for '{targetWord}' is '{baseWord}'");
                
                if (baseWord != targetWord)
                {
                    Debug.Log($"[DidYouKnow Debug] Trying with base word: {baseWord}");
                    fact = WordValidator.GetFactForWord(baseWord, era, language);
                    
                    // If still no fact and language is not English, try English as fallback
                    if (string.IsNullOrEmpty(fact) && language != "en")
                    {
                        Debug.Log($"[DidYouKnow Debug] Still no fact, trying with English language");
                        fact = WordValidator.GetFactForWord(baseWord, era, "en");
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
                
                // Get the language-specific title
                string titleText = language == "tr" ? "BİLİYOR MUYDUNUZ?" : "DID YOU KNOW?";
                
                // Set the fact text with the title
                didYouKnowText.text = titleText + "\n\n" + fact;
                
                // Show the panel immediately
                didYouKnowPanel.SetActive(true);
                Debug.Log("[DidYouKnow Debug] Panel activated immediately");
            }
            else
            {
                Debug.Log("[DidYouKnow Debug] No valid fact found, hiding panel");
                didYouKnowPanel.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DidYouKnow Debug] Error in ShowDidYouKnowInstantly: {e.Message}\n{e.StackTrace}");
        }
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
            
            Debug.Log($"[DidYouKnow Debug] Getting fact for word: {targetWord}, era: {era}, language: {language}");
            string fact = WordValidator.GetFactForWord(targetWord, era, language);
            
            // Log the result of getting the fact
            if (fact == "LOADING")
                Debug.Log("[DidYouKnow Debug] Fact is still in LOADING state");
            else if (string.IsNullOrEmpty(fact))
            {
                Debug.LogWarning("[DidYouKnow Debug] Fact is empty or null, trying with base word");
                
                // Check if the word exists in the JSON files - this should populate the fact
                WordValidator.CheckWordInJsonFiles(targetWord, era, language);
                
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
                            WordValidator.CheckWordInJsonFiles(baseWord, era, language);
                            
                            // Try one more time after checking JSON files
                            fact = WordValidator.GetFactForWord(baseWord, era, language);
                            
                            // If still no fact and language is not English, try English as fallback
                            if (string.IsNullOrEmpty(fact) && language != "en")
                            {
                                Debug.Log($"[DidYouKnow Debug] No fact found in {language}, trying English as final fallback");
                                WordValidator.CheckWordInJsonFiles(baseWord, era, "en");
                                fact = WordValidator.GetFactForWord(baseWord, era, "en");
                            }
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
                
                // Get the language-specific title
                string titleText = language == "tr" ? "BİLİYOR MUYDUNUZ?" : "DID YOU KNOW?";
                
                // Set the fact text with the title
                didYouKnowText.text = titleText + "\n\n" + fact;
                
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

    public void NextWord()
    {
        Debug.Log("[WordGameManager] NextWord called");
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[WordGameManager] GameManager.Instance is null in NextWord");
            return;
        }
        
        string currentEra = GameManager.Instance.CurrentEra;
        string currentLanguage = GameManager.Instance.CurrentLanguage;
        
        // Get all words for the current era
        List<string> wordsForEra = WordValidator.GetWordsForEra(currentEra);
        
        if (wordsForEra.Count == 0)
        {
            Debug.LogWarning($"[WordGameManager] No words found for era: {currentEra}");
            return;
        }
        
        // Find the index of the current word
        int currentIndex = wordsForEra.IndexOf(targetWord.ToUpper());
        Debug.Log($"[WordGameManager] Current word: {targetWord}, index: {currentIndex}, total words: {wordsForEra.Count}");
        
        // If the current word is not found or it's the last word, go to the first word
        if (currentIndex == -1 || currentIndex >= wordsForEra.Count - 1)
        {
            Debug.Log("[WordGameManager] Going to first word");
            LoadWord(wordsForEra[0]);
        }
        else
        {
            // Go to the next word
            Debug.Log($"[WordGameManager] Going to next word: {wordsForEra[currentIndex + 1]}");
            LoadWord(wordsForEra[currentIndex + 1]);
        }
        
        // If the word is already solved, preload the fact for it
        if (IsWordSolved(targetWord))
        {
            Debug.Log($"[WordGameManager] Word '{targetWord}' is already solved, preloading fact");
            PreloadFactForWord(targetWord, currentEra, currentLanguage);
        }
    }
    
    public void PreviousWord()
    {
        Debug.Log("[WordGameManager] PreviousWord called");
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[WordGameManager] GameManager.Instance is null in PreviousWord");
            return;
        }
        
        string currentEra = GameManager.Instance.CurrentEra;
        string currentLanguage = GameManager.Instance.CurrentLanguage;
        
        // Get all words for the current era
        List<string> wordsForEra = WordValidator.GetWordsForEra(currentEra);
        
        if (wordsForEra.Count == 0)
        {
            Debug.LogWarning($"[WordGameManager] No words found for era: {currentEra}");
            return;
        }
        
        // Find the index of the current word
        int currentIndex = wordsForEra.IndexOf(targetWord.ToUpper());
        Debug.Log($"[WordGameManager] Current word: {targetWord}, index: {currentIndex}, total words: {wordsForEra.Count}");
        
        // If the current word is not found or it's the first word, go to the last word
        if (currentIndex <= 0)
        {
            Debug.Log("[WordGameManager] Going to last word");
            LoadWord(wordsForEra[wordsForEra.Count - 1]);
        }
        else
        {
            // Go to the previous word
            Debug.Log($"[WordGameManager] Going to previous word: {wordsForEra[currentIndex - 1]}");
            LoadWord(wordsForEra[currentIndex - 1]);
        }
        
        // If the word is already solved, preload the fact for it
        if (IsWordSolved(targetWord))
        {
            Debug.Log($"[WordGameManager] Word '{targetWord}' is already solved, preloading fact");
            PreloadFactForWord(targetWord, currentEra, currentLanguage);
        }
    }

    // Method to handle language changes
    private void HandleLanguageChanged()
    {
        string newLanguage = GameManager.Instance.CurrentLanguage;
        Debug.Log($"[WordGameManager] Language changed to: {newLanguage}");
        
        // Reload the current word with the new language
        if (!string.IsNullOrEmpty(targetWord))
        {
            // Preload the fact for the current word in the new language
            if (IsWordSolved(targetWord))
            {
                PreloadFactForWord(targetWord, GameManager.Instance.CurrentEra, newLanguage);
            }
            
            // Update the UI elements that depend on language
            UpdateUI();
            UpdateSentenceDisplay();
        }
    }
    
    // Method to handle era changes
    private void HandleEraChanged()
    {
        string newEra = GameManager.Instance.CurrentEra;
        Debug.Log($"[WordGameManager] Era changed to: {newEra}");
        
        // Start a new game in the new era
        StartNewGameInEra();
    }
    
    // Method to clear the grid
    private void ClearGrid()
    {
        Debug.Log("[WordGameManager] Clearing grid");
        
        if (GridManager.Instance != null)
        {
            // Since ResetGrid() is not accessible, we'll use a different approach
            // We'll try to clear the grid by creating a new grid
            if (gridManager != null)
            {
                // Try to call a method that might exist
                try
                {
                    // Try to invoke a method using reflection
                    var method = gridManager.GetType().GetMethod("ClearGrid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(gridManager, null);
                        Debug.Log("[WordGameManager] Successfully called GridManager.ClearGrid via reflection");
                    }
                    else
                    {
                        Debug.LogWarning("[WordGameManager] Could not find ClearGrid method via reflection");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WordGameManager] Error calling GridManager.ClearGrid: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[WordGameManager] GridManager.Instance is null in ClearGrid");
        }
    }
    
    // Method to update the hint button state
    private void UpdateHintButton()
    {
        Debug.Log("[WordGameManager] Updating hint button");
        
        if (hintButton == null)
        {
            Debug.LogWarning("[WordGameManager] Hint button is null in UpdateHintButton");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[WordGameManager] GameManager.Instance is null in UpdateHintButton");
            return;
        }
        
        // Check if the word is already solved
        bool isSolved = IsWordSolved(targetWord);
        
        if (isSolved)
        {
            // If the word is solved, disable the hint button and update the text
            hintButton.interactable = false;
            if (hintButtonText != null)
            {
                hintButtonText.text = "Solved";
                hintButtonText.color = Color.black;
            }
        }
        else
        {
            // If the word is not solved, enable the hint button if the player has enough points
            hintButton.interactable = GameManager.Instance.CanUseHint(1, targetWord);
            if (hintButtonText != null)
            {
                // Use a hardcoded value or get it from GameManager if available
                int hintCost = 10; // Default value
                try
                {
                    // Try to get the hint cost from GameManager using reflection
                    var field = typeof(GameManager).GetField("HINT_COST", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (field != null)
                    {
                        hintCost = (int)field.GetValue(null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[WordGameManager] Could not get HINT_COST from GameManager: {e.Message}");
                }
                
                hintButtonText.text = $"Hint 1 ({hintCost})";
                hintButtonText.color = Color.white;
            }
        }
    }
    
    // Method to update the points display
    private void UpdatePointsDisplay()
    {
        Debug.Log("[WordGameManager] Updating points display");
        
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[WordGameManager] GameManager.Instance is null in UpdatePointsDisplay");
            return;
        }
        
        if (pointText != null)
        {
            pointText.text = GameManager.Instance.CurrentPoints.ToString();
        }
    }
    
    // Method to update the hint cost display
    private void UpdateHintCostDisplay()
    {
        Debug.Log("[WordGameManager] Updating hint cost display");
        
        if (hintPointAmountText != null)
        {
            // Use a hardcoded value or get it from GameManager if available
            int hintCost = 10; // Default value
            try
            {
                // Try to get the hint cost from GameManager using reflection
                var field = typeof(GameManager).GetField("HINT_COST", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null)
                {
                    hintCost = (int)field.GetValue(null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WordGameManager] Could not get HINT_COST from GameManager: {e.Message}");
            }
            
            hintPointAmountText.text = hintCost.ToString();
        }
    }
    
    // Method to reset the sentence display
    private void ResetSentenceDisplay()
    {
        Debug.Log("[WordGameManager] Resetting sentence display");
        
        if (sentenceText != null && !string.IsNullOrEmpty(originalSentence))
        {
            // Replace the blank with "..."
            string displaySentence = originalSentence.Replace("_____", "...");
            sentenceText.text = displaySentence;
        }
    }
    
    // Method to clear input fields
    private void ClearInputFields()
    {
        Debug.Log("[WordGameManager] Clearing input fields");
        
        // Reset the current word
        currentWord = "";
        
        // Clear any selected tiles in the grid
        if (GridManager.Instance != null)
        {
            // Since ResetSelection() is not accessible, we'll use a different approach
            try
            {
                // Try to invoke a method using reflection
                var method = GridManager.Instance.GetType().GetMethod("ClearSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(GridManager.Instance, null);
                    Debug.Log("[WordGameManager] Successfully called GridManager.ClearSelection via reflection");
                }
                else
                {
                    Debug.LogWarning("[WordGameManager] Could not find ClearSelection method via reflection");
                    
                    // Alternative approach: try to reset the selection by getting and clearing selected tiles
                    var selectedTilesProperty = GridManager.Instance.GetType().GetProperty("SelectedTiles");
                    if (selectedTilesProperty != null)
                    {
                        var selectedTiles = selectedTilesProperty.GetValue(GridManager.Instance) as List<LetterTile>;
                        if (selectedTiles != null)
                        {
                            selectedTiles.Clear();
                            Debug.Log("[WordGameManager] Cleared selected tiles via reflection");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WordGameManager] Error clearing selection: {e.Message}");
            }
        }
    }
    
    // Method to update the UI
    private void UpdateUI()
    {
        Debug.Log("[WordGameManager] Updating UI");
        
        // Update the sentence display
        UpdateSentenceDisplay();
        
        // Update the progress bar
        UpdateProgressBar();
        
        // Update the hint button
        UpdateHintButton();
        
        // Update the points display
        UpdatePointsDisplay();
    }
    
    // Method to check if a word is correct
    public bool CheckWord(string word)
    {
        Debug.Log($"[WordGameManager] Checking word: {word}");
        
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogWarning("[WordGameManager] Cannot check empty word");
            return false;
        }
        
        // Check if the word matches the target word
        bool isCorrect = word.Equals(targetWord, StringComparison.OrdinalIgnoreCase);
        
        if (isCorrect)
        {
            Debug.Log($"[WordGameManager] Word '{word}' is correct!");
            
            // Mark the word as solved
            if (GameManager.Instance != null)
            {
                // Get the base word (English version)
                string baseWord = GameManager.Instance.GetBaseWord(targetWord);
                
                // Since AddSolvedWord is not available, use an alternative method
                // Since MarkWordAsSolved is not available, use an alternative method
                // Option 1: Try to find a public method that does something similar
                GameManager.Instance.AddSolvedWord(baseWord);
                
                // Option 2: If no public method exists, we might need to implement our own logic
                // or add a public method to GameManager
                
                // Set the flag to indicate that the current word was just solved
                isCurrentWordNewlySolved = true;
                
                // Update the progress bar
                UpdateProgressBar();
                
                // Show the "Did You Know" fact
                ShowDidYouKnow();
                
                // Increment the words guessed count
                wordsGuessedCount++;
                
                // Check if we should show an ad
                if (wordsGuessedCount >= WORDS_BEFORE_AD)
                {
                    ShowAd();
                }
            }
        }
        else
        {
            Debug.Log($"[WordGameManager] Word '{word}' is incorrect. Expected: '{targetWord}'");
            HandleIncorrectWord();
        }
        
        return isCorrect;
    }
    
    // Overload for CheckWord that takes a word and a callback
    public bool CheckWord(string word, Action<bool> callback)
    {
        Debug.Log($"[WordGameManager] CheckWord with callback called for word: {word}");
        
        // Check the word using the original method
        bool isCorrect = CheckWord(word);
        
        // Invoke the callback with the result
        callback?.Invoke(isCorrect);
        
        return isCorrect;
    }
    
    // Coroutine to show the "Did You Know" panel
    private IEnumerator ShowDidYouKnowCoroutine(string fact)
    {
        Debug.Log("[DidYouKnow Debug] Starting ShowDidYouKnowCoroutine");
        
        // Make sure the panel is active
        didYouKnowPanel.SetActive(true);
        
        // Position the panel in the safe area
        PositionPanelInSafeArea();
        
        // Wait for the specified display time
        yield return new WaitForSeconds(factDisplayTime);
        
        // Hide the panel
        didYouKnowPanel.SetActive(false);
        Debug.Log("[DidYouKnow Debug] Did You Know panel hidden after display time");
    }
    
    // Method to position the panel in the safe area
    private void PositionPanelInSafeArea()
    {
        if (didYouKnowPanel == null)
        {
            Debug.LogWarning("[DidYouKnow Debug] didYouKnowPanel is null in PositionPanelInSafeArea");
            return;
        }
        
        // Get the RectTransform of the panel
        RectTransform panelRect = didYouKnowPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            Debug.LogWarning("[DidYouKnow Debug] didYouKnowPanel does not have a RectTransform component");
            return;
        }
        
        // Center the panel in the screen
        panelRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("[DidYouKnow Debug] Panel positioned in safe area");
    }

    // Method to give a hint to the player
    public void GiveHint()
    {
        Debug.Log("[WordGameManager] GiveHint called");
        
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[WordGameManager] GameManager.Instance is null in GiveHint");
            return;
        }
        
        if (string.IsNullOrEmpty(targetWord))
        {
            Debug.LogWarning("[WordGameManager] Cannot give hint for empty target word");
            return;
        }
        
        // Check if the player has enough points for a hint
        if (GameManager.Instance.CanUseHint(1, targetWord))
        {
            // Use the hint
            GameManager.Instance.UseHint(1, targetWord);
            
            // Show the first letter of the target word
            if (sentenceText != null && !string.IsNullOrEmpty(originalSentence))
            {
                // Get the first letter of the target word
                char firstLetter = targetWord[0];
                
                // Create a display word with the first letter revealed
                string displayWord = firstLetter + new string('_', targetWord.Length - 1);
                
                // Update the sentence display
                string displaySentence = originalSentence.Replace("_____", displayWord);
                sentenceText.text = displaySentence;
            }
            
            // Update the hint button
            UpdateHintButton();
            
            // Update the points display
            UpdatePointsDisplay();
        }
        else
        {
            Debug.Log("[WordGameManager] Not enough points for hint");
            // Show a message to the player
            ShowMessage("Not enough points for hint!", Color.red);
        }
    }
}
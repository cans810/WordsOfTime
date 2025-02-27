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
    private Coroutine showLengthCoroutine;

    private GridManager gridManager;

    [SerializeField] private int numberOfPreGeneratedGrids = 5; // Number of grids per era
    private List<GridData> preGeneratedGrids = new List<GridData>();

    private string currentFormingWord = "";

    private List<LetterTile> selectedTiles = new List<LetterTile>();

    private const int WORDS_BEFORE_AD = 3;
    private int wordsGuessedCount = 0;
    private bool isAdShowing = false; // Add flag to track when an ad is being shown

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
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }

        // Load solved words from the JSON file
        LoadSolvedWords();

        // Update the progress bar based on loaded solved words
        UpdateProgressBar();

        didYouKnowPanel.SetActive(false);

        wordsGuessedCount = 0; // Ensure counter starts at 0
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }
        Debug.Log("WordGameManager enabled");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
        Debug.Log("WordGameManager disabled");
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
        // If an ad is already being shown, don't show another one
        if (isAdShowing)
        {
            Debug.Log("Ad is already being shown, ignoring request");
            return;
        }

        if (GameManager.Instance.NoAdsBought)
        {
            Debug.Log("No Ads purchased - skipping ad");
            return;
        }

        // Show the ad
        if (AdManager.Instance != null)
        {
            try
            {
                Debug.Log("WordGameManager: Showing interstitial ad...");
                isAdShowing = true; // Set flag before showing ad
                AdManager.Instance.ShowInterstitialAd();
                
                // Reset the counter only after successfully initiating ad display
                wordsGuessedCount = 0;
                Debug.Log("Ad request sent, counter reset to 0");
                
                // Reload the ad for next time after a short delay
                StartCoroutine(ReloadAdAfterDelay(1.0f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing ad: {e.Message}");
                isAdShowing = false; // Reset flag if there was an error
                // Don't reset counter if ad failed to show
            }
        }
        else
        {
            Debug.LogWarning("AdManager instance not found!");
            // Don't reset counter if AdManager is not available
        }
    }
    
    private IEnumerator ReloadAdAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (AdManager.Instance != null)
        {
            Debug.Log("Reloading ad after delay");
            AdManager.Instance.LoadInterstitialAd();
            isAdShowing = false; // Reset flag after reload
        }
    }

    // Method to be called when an ad is completed
    public void OnAdCompleted()
    {
        Debug.Log("Ad completed, resetting isAdShowing flag");
        isAdShowing = false;
    }

    public void StartNewGameInEra()
    {
        Debug.Log("Starting new game in era");
        if (GameManager.Instance != null)
        {
            // PROBLEM: WordValidator.GetWordsForEra might be loading words directly from JSON
            // Instead, let's get the words from GameManager's shuffled list
            currentEraWords = new List<string>(GameManager.Instance.eraWordsPerLanguage[GameManager.Instance.CurrentLanguage][GameManager.Instance.CurrentEra]);
            Debug.Log($"Words order for {GameManager.Instance.CurrentEra}: {string.Join(", ", currentEraWords)}");
            
            // Create progress indicators
            CreateProgressBar();
            UpdateProgressBar();

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
        // Get the base word (English version)
        string baseWord = GameManager.Instance.GetBaseWord(word);
        return GameManager.Instance.IsWordSolved(baseWord);
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
            hintButton.interactable = GameManager.Instance.CanUseHint(1, targetWord); // Enable for first hint
            if (hintButtonText != null)
            {
                hintButtonText.text = $"Hint 1 ({GameManager.HINT_COST})";
            }
        }

        UpdateHintButton();
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
            currentWordIndex++;
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
            Debug.Log($"[Android Debug] Moved to next word, index: {currentWordIndex}");
        }
        else
        {
            Debug.Log($"[Android Debug] Already at last word, index: {currentWordIndex}");
        }

        // Check if the current word is already solved
        string baseWord = GameManager.Instance.GetBaseWord(targetWord);
        if (GameManager.Instance.IsWordSolved(baseWord))
        {
            Debug.Log($"Word {baseWord} is solved, finding {targetWord} in grid to highlight");
            HighlightWordInGrid(targetWord);
        }
        else
        {
            Debug.Log($"Word {baseWord} is not solved yet.");
        }
        
        UpdateSentenceDisplay(targetWord);
        UpdateProgressBar();
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

    private void HighlightWordInGrid(string word)
    {
        Debug.Log($"Searching for word: {word}");
        int gridSize = GridManager.Instance.grid.GetLength(0);
        bool[,] visited = new bool[gridSize, gridSize];

        // First, clear the entire grid to white
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                GridManager.Instance.grid[row, col].ResetTile();
                GridManager.Instance.grid[row, col].GetComponent<Image>().color = Color.white;
            }
        }

        // Then search for the word
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                if (DFS(row, col, word, 0, visited))
                {
                    Debug.Log($"Found word {word} starting at ({row},{col})");
                    return;
                }
            }
        }
        
        Debug.LogError($"Failed to find word: {word} in grid!");
    }

    private bool DFS(int row, int col, string word, int index, bool[,] visited)
    {
        if (index == word.Length)
            return true;

        int gridSize = GridManager.Instance.grid.GetLength(0);

        if (row < 0 || row >= gridSize || col < 0 || col >= gridSize || visited[row, col])
            return false;

        if (char.ToUpperInvariant(GridManager.Instance.grid[row, col].GetLetter()) != char.ToUpperInvariant(word[index]))
            return false;

        visited[row, col] = true;

        // Check all 8 directions
        int[] dRow = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dCol = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int dir = 0; dir < 8; dir++)
        {
            if (DFS(row + dRow[dir], col + dCol[dir], word, index + 1, visited))
            {
                GridManager.Instance.grid[row, col].SetSolvedColor();
                GridManager.Instance.grid[row, col].isSolved = true;
                GridManager.Instance.grid[row, col].GetComponent<Image>().raycastTarget = false;
                return true;
            }
        }

        visited[row, col] = false;
        return false;
    }

    public void PreviousWord()
    {
        Debug.Log($"[Android Debug] PreviousWord button clicked, isAnimationPlaying: {isAnimationPlaying}");
        if (isAnimationPlaying)
        {
            Debug.Log("[Android Debug] Animation is playing, ignoring PreviousWord call");
            return;
        }
        
        if (currentWordIndex > 0)
        {
            currentWordIndex--;
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
            Debug.Log($"[Android Debug] Moved to previous word, index: {currentWordIndex}");
        }
        else
        {
            Debug.Log($"[Android Debug] Already at first word, index: {currentWordIndex}");
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
            GameManager.Instance.UseHint(hintToUse);
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
            // First check if the word is already guessed
            string baseWord = GameManager.Instance.GetBaseWord(targetWord);
            if (GameManager.Instance.IsWordSolved(baseWord))
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
        Debug.Log($"Checking word: {word} against target: {targetWord}");
        if (word == targetWord)
        {
            // Word is correct
            Debug.Log("Word is correct, starting coin animation");
            
            foreach (var tile in selectedTiles)
            {
                tile.SetSolvedColor();
                tile.isSolved = true;
                tile.GetComponent<Image>().raycastTarget = false;
            }
            
            // Get the base (English) word
            string baseWord = GameManager.Instance.GetBaseWord(word);
            Debug.Log($"Base word: {baseWord}");
            
            // Store positions and mark word as solved
            List<Vector2Int> positions = selectedTiles.Select(t => t.GetGridPosition()).ToList();
            GameManager.Instance.StoreSolvedWordPositions(word, positions);
            GameManager.Instance.StoreSolvedBaseWord(GameManager.Instance.CurrentEra, baseWord);
            GameManager.Instance.OnWordGuessed();
            
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

            // Increment counter and check for ad
            wordsGuessedCount++;
            Debug.Log($"Words guessed: {wordsGuessedCount}/{WORDS_BEFORE_AD}");
            
            // Save game after successful word guess
            if (SaveManager.Instance != null)
            {
                Debug.Log("Saving game after successful word guess");
                SaveManager.Instance.SaveGame();
            }
            
            // Check if it's time to show an ad
            if (wordsGuessedCount >= WORDS_BEFORE_AD && !isAdShowing)
            {
                Debug.Log("Playing ad after 3 words");
                ShowAd();
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
        
        // Store both the index and the base word
        solvedWordsInCurrentEra.Add(currentWordIndex);
        GameManager.Instance.StoreSolvedWordIndex(GameManager.Instance.CurrentEra, currentWordIndex);
        
        // Store the base word for cross-language support
        GameManager.Instance.StoreSolvedBaseWord(GameManager.Instance.CurrentEra, baseWord);
        
        // Store the positions for both the current word and its translation
        List<Vector2Int> positions = GridManager.Instance.GetSelectedTiles().Select(t => t.GetGridPosition()).ToList();
        GameManager.Instance.StoreSolvedWordPositions(word, positions);
        
        // Also store positions for the translated version
        string translatedWord = GameManager.Instance.GetTranslation(baseWord, GameManager.Instance.CurrentLanguage);
        if (translatedWord != word)
        {
            GameManager.Instance.StoreSolvedWordPositions(translatedWord, positions);
        }
    }

    private void HandleLanguageChanged()
    {
        Debug.Log("Language changed, updating grid...");
        
        // 1. Reset all tiles to default state
        int gridSize = GridManager.Instance.grid.GetLength(0);
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                GridManager.Instance.grid[i, j].ResetTile();
                GridManager.Instance.grid[i, j].isSolved = false;
                GridManager.Instance.grid[i, j].GetComponent<Image>().raycastTarget = false;
            }
        }
        
        // 2. Get the current word's translation and check if it's solved
        string baseWord = GameManager.Instance.GetBaseWord(targetWord);
        string newWord = GameManager.Instance.GetTranslation(baseWord, GameManager.Instance.CurrentLanguage);
        targetWord = newWord;
        
        if (GameManager.Instance.IsWordSolved(baseWord))
        {
            Debug.Log($"Word {baseWord} is solved, finding {newWord} in grid to highlight");
            HighlightWordInGrid(newWord);
        }
        
        // 3. Update UI
        UpdateSentenceDisplay(targetWord);
        UpdateProgressBar();
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

    private void LoadSolvedWords()
    {
        // Load solved words from the JSON file
        if (SaveManager.Instance.Data.solvedWords != null)
        {
            solvedWords = new HashSet<string>(SaveManager.Instance.Data.solvedWords);
            Debug.Log("Solved words loaded from JSON: " + string.Join(",", solvedWords));
        }

        // Update the progress bar based on solved words
        UpdateProgressBar();
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

    private void ShowDidYouKnow()
    {
        Debug.Log($"[Android Debug] Showing Did You Know for word: {targetWord} in language: {GameManager.Instance.CurrentLanguage}");
        
        // Make sure the panel exists and is assigned
        if (didYouKnowPanel == null)
        {
            Debug.LogError("[Android Debug] Did You Know panel is null!");
            return;
        }

        Debug.Log($"[Android Debug] Did You Know panel exists, getting fact for word");
        string fact = WordValidator.GetFactForWord(targetWord, GameManager.Instance.CurrentEra, GameManager.Instance.CurrentLanguage);
        Debug.Log($"[Android Debug] Retrieved fact: {fact}");
        
        if (!string.IsNullOrEmpty(fact))
        {
            // Ensure we're on the main thread when modifying UI
            Debug.Log($"[Android Debug] Fact is not empty, checking animation flag: {isAnimationPlaying}");
            if (!isAnimationPlaying)  // Add this check
            {
                isAnimationPlaying = true;
                Debug.Log("[Android Debug] Starting ShowDidYouKnowCoroutine");
                StartCoroutine(ShowDidYouKnowCoroutine(fact));
            }
        }
        else
        {
            Debug.LogWarning("[Android Debug] No fact found for word: {targetWord}");
        }
    }

    private IEnumerator ShowDidYouKnowCoroutine(string fact)
    {
        Debug.Log("[Android Debug] ShowDidYouKnowCoroutine started");
        // Wait for any ongoing animations to complete
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[Android Debug] After wait, setting up panel");

        // Check if the fact is in LOADING state (Android specific)
        if (fact == "LOADING")
        {
            Debug.Log("[Android Debug] Facts are still loading, will retry in 1 second");
            
            // Wait and retry up to 5 times
            int retryCount = 0;
            int maxRetries = 5;
            
            while (retryCount < maxRetries)
            {
                yield return new WaitForSeconds(1f);
                retryCount++;
                
                Debug.Log($"[Android Debug] Retry {retryCount}/{maxRetries} to get fact for word: {targetWord}");
                fact = WordValidator.GetFactForWord(targetWord, GameManager.Instance.CurrentEra, GameManager.Instance.CurrentLanguage);
                
                if (fact != "LOADING" && !string.IsNullOrEmpty(fact))
                {
                    Debug.Log("[Android Debug] Successfully retrieved fact after retry");
                    break;
                }
                
                if (retryCount >= maxRetries)
                {
                    Debug.LogWarning("[Android Debug] Max retries reached, giving up on showing fact");
                    isAnimationPlaying = false;
                    // Make sure to re-enable buttons before exiting
                    EnableNavigationButtons();
                    yield break;
                }
            }
        }
        
        // If we still don't have a valid fact, exit
        if (string.IsNullOrEmpty(fact) || fact == "LOADING")
        {
            Debug.LogWarning("[Android Debug] No valid fact available after retries");
            isAnimationPlaying = false;
            // Make sure to re-enable buttons before exiting
            EnableNavigationButtons();
            yield break;
        }

        // Disable navigation buttons
        DisableNavigationButtons();
        
        // Make sure the panel is active
        Debug.Log("[Android Debug] Setting didYouKnowPanel to active");
        didYouKnowPanel.SetActive(true);
        
        // Use Turkish title if current language is Turkish
        string title = GameManager.Instance.CurrentLanguage == "tr" ? 
            "Biliyor muydunuz?\n\n" : 
            "Did You Know?\n\n";
        
        if (didYouKnowText != null)
        {
            Debug.Log("[Android Debug] Setting didYouKnowText content");
            didYouKnowText.text = title + fact;
        }
        else
        {
            Debug.LogError("[Android Debug] didYouKnowText is null!");
        }

        // Position the panel within the safe area on Android
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (didYouKnowPanel != null)
        {
            RectTransform panelRect = didYouKnowPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                // Get the safe area
                Rect safeArea = Screen.safeArea;
                Canvas canvas = didYouKnowPanel.GetComponentInParent<Canvas>();
                
                if (canvas != null)
                {
                    // Convert safe area to canvas space
                    Vector2 safeMin = canvas.GetComponent<RectTransform>().rect.min;
                    Vector2 safeMax = canvas.GetComponent<RectTransform>().rect.max;
                    
                    // Center the panel within the safe area
                    panelRect.anchoredPosition = new Vector2(0, 0);
                    Debug.Log("[Android Debug] Positioned panel within safe area");
                }
            }
        }
        #endif

        // Keep the panel visible for at least 5 seconds
        yield return new WaitForSeconds(5f);
        
        Debug.Log("[Android Debug] Panel should now be visible");
        
        // Auto-hide the panel after the display time if it's still active
        if (didYouKnowPanel.activeSelf)
        {
            didYouKnowPanel.SetActive(false);
            Debug.Log("[Android Debug] Auto-hiding panel after display time");
            
            // Re-enable navigation buttons when auto-hiding
            EnableNavigationButtons();
        }
        
        // Reset animation flag
        isAnimationPlaying = false;
    }
    
    // Helper method to disable navigation buttons
    private void DisableNavigationButtons()
    {
        Debug.Log($"[Android Debug] Disabling navigation buttons. Next button: {(nextQuestionButton != null ? "exists" : "null")}, Prev button: {(prevQuestionButton != null ? "exists" : "null")}");
        if (nextQuestionButton != null) nextQuestionButton.interactable = false;
        if (prevQuestionButton != null) prevQuestionButton.interactable = false;
        Debug.Log("[Android Debug] Navigation buttons disabled");
    }
    
    // Helper method to enable navigation buttons
    private void EnableNavigationButtons()
    {
        Debug.Log($"[Android Debug] Enabling navigation buttons. Next button: {(nextQuestionButton != null ? "exists" : "null")}, Prev button: {(prevQuestionButton != null ? "exists" : "null")}");
        if (nextQuestionButton != null) nextQuestionButton.interactable = true;
        if (prevQuestionButton != null) prevQuestionButton.interactable = true;
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
}
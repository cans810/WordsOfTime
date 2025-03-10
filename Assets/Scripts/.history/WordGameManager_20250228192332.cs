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

            // Make sure didYouKnowPanel is initialized before loading word
            if (didYouKnowPanel == null)
            {
                Debug.LogWarning("didYouKnowPanel is null in StartNewGameInEra, initializing it");
                InitializeDidYouKnowPanel();
            }

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
}
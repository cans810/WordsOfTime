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
    private const float POINT_ANIMATION_DURATION = 0.5f; // Even faster overall animation
    private const float BUMP_SCALE = 1.15f; // Slightly smaller bump for smoother feel
    private const float BUMP_DURATION = 0.05f; // Faster bumps

    private HashSet<string> solvedWords = new HashSet<string>();
    public delegate void WordSolvedHandler(string word);
    public event WordSolvedHandler OnWordSolved;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!gameInitialized)
        {
            InitializeUI();
            StartNewGameInEra();
            CreateProgressBar();
            gameInitialized = true;
        }
        else
        {
            InitializeUI();
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }

        // Restore solved words for current era
        solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(GameManager.Instance.CurrentEra);
        UpdateProgressBar();
        SetupNewWord();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            // Find references first
            InitializeUI();
            CreateProgressBar();
            
            // Then initialize the game state
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

    private IEnumerator AnimatePointsIncrease(int startPoints, int endPoints)
    {
        float elapsedTime = 0f;
        Vector3 originalScale = pointText.transform.localScale;
        
        while (elapsedTime < POINT_ANIMATION_DURATION)
        {
            elapsedTime += Time.deltaTime;
            
            // Even steeper acceleration curve
            float t = elapsedTime / POINT_ANIMATION_DURATION;
            t = t == 1f ? 1f : 1f - Mathf.Pow(2, -15 * t); // Increased from -12 to -15
            
            int previousPoints = int.Parse(pointText.text);
            int currentPoints = Mathf.RoundToInt(Mathf.Lerp(startPoints, endPoints, t));
            
            // Only animate bump if the value changed
            if (currentPoints != previousPoints)
            {
                // Smoother bump animation
                float bumpTime = 0f;
                while (bumpTime < BUMP_DURATION)
                {
                    bumpTime += Time.deltaTime;
                    float bumpT = bumpTime / BUMP_DURATION;
                    // Smoother easing for the bump
                    float smoothT = 1f - (1f - bumpT) * (1f - bumpT);
                    float scale = Mathf.Lerp(BUMP_SCALE, 1f, smoothT);
                    pointText.transform.localScale = originalScale * scale;
                    yield return null;
                }
            }
            
            pointText.text = currentPoints.ToString();
            pointText.transform.localScale = originalScale;
            
            yield return null;
        }
        
        // Ensure we end up at the exact final value
        pointText.text = endPoints.ToString();
        pointText.transform.localScale = originalScale;
    }

    public void HandleCorrectWord()
    {
        if (pointAnimationCoroutine != null)
        {
            StopCoroutine(pointAnimationCoroutine);
        }
        
        int startPoints = GameManager.Instance.CurrentPoints;
        GameManager.Instance.AddPoints(GameManager.POINTS_PER_WORD);
        pointAnimationCoroutine = StartCoroutine(AnimatePointsIncrease(startPoints, GameManager.Instance.CurrentPoints));
        
        GridManager.Instance.ResetGridForNewWord();
        UpdateProgressBar();
        UpdateSentenceDisplay();
    }

    public void StartNewGameInEra()
    {
        if (GameManager.Instance == null) return;

        currentEraWords = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra);
        solvedWordsInCurrentEra.Clear();
        currentWordIndex = 0;
        
        if (currentEraWords.Count > 0)
        {
            LoadWord(currentWordIndex);
            CreateProgressBar();
            UpdateProgressBar();
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
                else if (hintLevel >= 2) // Second hint: show underscores
                {
                    string displayWord = new string('_', targetWord.Length);
                    displaySentence = originalSentence.Replace("_____", displayWord);
                }
                else // Initial state or first hint: show ...
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

        hintLevel = 0;
        UpdateHintCostDisplay();
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
        if (string.IsNullOrEmpty(targetWord)) return;

        // Check if player can afford the hint
        int nextHintLevel = hintLevel + 1;
        if (!GameManager.Instance.CanUseHint(nextHintLevel))
        {
            ShowMessage($"Need {(nextHintLevel == 1 ? GameManager.HINT_COST : GameManager.SECOND_HINT_COST)} points for hint!");
            return;
        }

        hintLevel = nextHintLevel;
        GameManager.Instance.UseHint(hintLevel);
        UpdatePointsDisplay();
        UpdateHintCostDisplay();

        if (hintLevel == 1)
        {
            GridManager.Instance.HighlightFirstLetter(targetWord[0]);
        }
        else if (hintLevel == 2)
        {
            if (numberAnimationCoroutine != null)
            {
                StopCoroutine(numberAnimationCoroutine);
            }
            numberAnimationCoroutine = StartCoroutine(ShowNumberSequence());
        }
        else
        {
            hintLevel = 0;
            UpdateSentenceDisplay();
        }
    }

    private IEnumerator ShowNumberSequence()
    {
        for (int i = 1; i <= targetWord.Length; i++)
        {
            string numbers = string.Join("", Enumerable.Range(1, i));
            sentenceText.text = originalSentence.Replace("_____", numbers);
            yield return new WaitForSeconds(NUMBER_ANIMATION_DELAY);
        }

        // After showing all numbers, replace with underscores
        yield return new WaitForSeconds(NUMBER_ANIMATION_DELAY);
        string underscores = new string('_', targetWord.Length);
        sentenceText.text = originalSentence.Replace("_____", underscores);
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
        pointAnimationCoroutine = StartCoroutine(AnimatePointsIncrease(startPoints, GameManager.Instance.CurrentPoints));
        
        solvedWordsInCurrentEra.Add(currentWordIndex);
        GameManager.Instance.StoreSolvedWordIndex(GameManager.Instance.CurrentEra, currentWordIndex);
        UpdateProgressBar();
        UpdateSentenceDisplay();
    }
}
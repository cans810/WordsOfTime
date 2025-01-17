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
            // Ensure UI is properly initialized when returning to game scene
            InitializeUI();
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
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

    public void HandleCorrectWord()
    {
        solvedWordsInCurrentEra.Add(currentWordIndex);
        solvedWordCountInCurrentEra = solvedWordsInCurrentEra.Count;
        
        // Store the solved word in GameManager
        GameManager.Instance.AddSolvedWordForCurrentEra(currentWordIndex);
        
        GridManager.Instance.ResetGridForNewWord();
        UpdateProgressBar();
        UpdateSentenceDisplay();
    }

    public void StartNewGameInEra()
    {
        currentEraWords = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra);
        if (currentEraWords == null || currentEraWords.Count == 0) return;

        // Get the solved words for this era
        solvedWordsInCurrentEra = GameManager.Instance.GetSolvedWordsForEra(GameManager.Instance.CurrentEra);
        solvedWordCountInCurrentEra = solvedWordsInCurrentEra.Count;
        currentWordIndex = 0;
        
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ResetGridForNewWord();
        }
        
        LoadWord(currentWordIndex);
        UpdateProgressBar();
        UpdateSentenceDisplay();
        
        if (BackgroundImage != null)
        {
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
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

        // Set initial values
        if (scoreText != null) scoreText.text = "Score: 0";
        if (messageText != null) messageText.text = "";
        if (BackgroundImage != null)
        {
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
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
        if (currentEraWords == null) return false;
        int wordIndex = currentEraWords.IndexOf(word);
        return wordIndex != -1 && solvedWordsInCurrentEra.Contains(wordIndex);
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

        hintLevel++;
        if (hintLevel == 1)
        {
            // First hint: Highlight first letter in grid
            GridManager.Instance.HighlightFirstLetter(targetWord[0]);
        }
        else if (hintLevel == 2)
        {
            // Second hint: Start number animation
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
}
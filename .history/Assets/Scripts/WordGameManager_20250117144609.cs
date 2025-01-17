using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class WordGameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public SpriteRenderer BackgroundImage;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI sentenceText;

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
        GridManager.Instance.ResetGridForNewWord(); // Changed from ClearGrid to ResetGridForNewWord
        UpdateProgressBar();
        UpdateSentenceDisplay();
    }

    public void StartNewGameInEra()
    {
        currentEraWords = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra);
        if (currentEraWords == null || currentEraWords.Count == 0) return;

        solvedWordsInCurrentEra.Clear();
        solvedWordCountInCurrentEra = 0;
        currentWordIndex = 0;
        
        // Reset the grid state
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ResetGridForNewWord();
        }
        
        LoadWord(currentWordIndex);
        UpdateProgressBar();
        UpdateSentenceDisplay();
        
        // Update background
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
                    string displayWord = currentWord.PadRight(targetWord.Length, '_');
                    displaySentence = originalSentence.Replace("_____", displayWord);
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

        currentWordIndex = index;
        targetWord = currentEraWords[currentWordIndex];
        string sentence = WordValidator.GetSentenceForWord(targetWord, GameManager.Instance.CurrentEra);

        if (sentence == null) return;

        SetupGame(targetWord, sentence);
        
        if (GridManager.Instance != null)
        {
            GridManager.Instance.SetupNewPuzzle(targetWord);
        }

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
}
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


    private int currentScore = 0;
    private const float MESSAGE_DISPLAY_TIME = 2f;
    public string targetWord;
    private string originalSentence;
    private string currentWord = "";
    public int solvedWordCountInCurrentEra = 0;
    public static WordGameManager Instance { get; private set; }


    [Header("Progress Bar")]

    public GameObject progressImagePrefab;
    public Transform progressBarContainer;

    private List<GameObject> progressImages = new List<GameObject>();
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
        GameObject gameSceneCanvas = GameObject.Find("GameSceneCanvas");

        if (gameSceneCanvas != null)
        {
            Debug.Log("GameSceneCanvas found successfully.");

            // Find the BackgroundImage child object
            Transform backgroundImageTransform = gameSceneCanvas.transform.Find("BackgroundImage");

            if (backgroundImageTransform != null)
            {
                Debug.Log("BackgroundImage found as a child of GameSceneCanvas.");

                // Get the SpriteRenderer or Image component (depending on your setup)
                BackgroundImage = backgroundImageTransform.GetComponent<SpriteRenderer>(); // For SpriteRenderer

                if (BackgroundImage != null)
                {
                    Debug.Log("SpriteRenderer component found on BackgroundImage.");
                }
                else
                {
                    Debug.LogError("SpriteRenderer component NOT found on BackgroundImage!");
                }
            }
            else
            {
                Debug.LogError("BackgroundImage NOT found as a child of GameSceneCanvas!");
            }

            // Find the SentencePanel child object
            Transform sentencePanel = gameSceneCanvas.transform.Find("SentencePanel");

            if (sentencePanel != null)
            {
                Debug.Log("SentencePanel found as a child of GameSceneCanvas.");

                Transform sentence = sentencePanel.transform.Find("Sentence");

                if (sentence != null)
                {
                    Debug.Log("Sentence found as a child of SentencePanel.");

                    sentenceText = sentence.gameObject.GetComponent<TextMeshProUGUI>();

                    if (sentenceText != null)
                    {
                        Debug.Log("TextMeshProUGUI component found on Sentence.");
                    }
                    else
                    {
                        Debug.LogError("TextMeshProUGUI component NOT found on Sentence!");
                    }
                }
                else
                {
                    Debug.LogError("Sentence NOT found as a child of SentencePanel!");
                }
            }
            else
            {
                Debug.LogError("SentencePanel NOT found as a child of GameSceneCanvas!");
            }

            // Find the ProgressPanel child object
            Transform progressPanel = gameSceneCanvas.transform.Find("ProgressPanel");

            if (progressPanel != null)
            {
                Debug.Log("ProgressPanel found as a child of GameSceneCanvas.");

                // Get the SpriteRenderer or Image component (depending on your setup)
                progressBarContainer = progressPanel;

                if (progressBarContainer != null)
                {
                    Debug.Log("ProgressBarContainer assigned successfully.");
                }
                else
                {
                    Debug.LogError("ProgressBarContainer NOT assigned!");
                }
            }
            else
            {
                Debug.LogError("ProgressPanel NOT found as a child of GameSceneCanvas!");
            }
        }
        else
        {
            Debug.LogError("GameSceneCanvas NOT found in the scene!");
        }


        if (!gameInitialized)
        {
            InitializeUI();
            StartNewGameInEra();
            CreateProgressBar();
            gameInitialized = true;
        }
        else
        {
            // Reinitialize the sentence and grid when returning to the game scene
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
    }

    private void OnEnable()
    {
        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from the sceneLoaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene") // Replace "GameScene" with the name of your game scene
        {
            // Reinitialize the sentence and grid when returning to the game scene
            if (gameInitialized)
            {
                GameObject gameSceneCanvas = GameObject.Find("GameSceneCanvas");

        if (gameSceneCanvas != null)
        {
            Debug.Log("GameSceneCanvas found successfully.");

                // Find the BackgroundImage child object
                Transform backgroundImageTransform = gameSceneCanvas.transform.Find("BackgroundImage");

                if (backgroundImageTransform != null)
                {
                    Debug.Log("BackgroundImage found as a child of GameSceneCanvas.");

                    // Get the SpriteRenderer or Image component (depending on your setup)
                    BackgroundImage = backgroundImageTransform.GetComponent<SpriteRenderer>(); // For SpriteRenderer

                    if (BackgroundImage != null)
                    {
                        Debug.Log("SpriteRenderer component found on BackgroundImage.");
                    }
                    else
                    {
                        Debug.LogError("SpriteRenderer component NOT found on BackgroundImage!");
                    }
                }
                else
                {
                    Debug.LogError("BackgroundImage NOT found as a child of GameSceneCanvas!");
                }

                // Find the SentencePanel child object
                Transform sentencePanel = gameSceneCanvas.transform.Find("SentencePanel");

                if (sentencePanel != null)
                {
                    Debug.Log("SentencePanel found as a child of GameSceneCanvas.");

                    Transform sentence = sentencePanel.transform.Find("Sentence");

                    if (sentence != null)
                    {
                        Debug.Log("Sentence found as a child of SentencePanel.");

                        sentenceText = sentence.gameObject.GetComponent<TextMeshProUGUI>();

                        if (sentenceText != null)
                        {
                            Debug.Log("TextMeshProUGUI component found on Sentence.");
                        }
                        else
                        {
                            Debug.LogError("TextMeshProUGUI component NOT found on Sentence!");
                        }
                    }
                    else
                    {
                        Debug.LogError("Sentence NOT found as a child of SentencePanel!");
                    }
                }
                else
                {
                    Debug.LogError("SentencePanel NOT found as a child of GameSceneCanvas!");
                }

                // Find the ProgressPanel child object
                Transform progressPanel = gameSceneCanvas.transform.Find("ProgressPanel");

                if (progressPanel != null)
                {
                    Debug.Log("ProgressPanel found as a child of GameSceneCanvas.");

                    // Get the SpriteRenderer or Image component (depending on your setup)
                    progressBarContainer = progressPanel;

                    if (progressBarContainer != null)
                    {
                        Debug.Log("ProgressBarContainer assigned successfully.");
                    }
                    else
                    {
                        Debug.LogError("ProgressBarContainer NOT assigned!");
                    }
                }
                else
                {
                    Debug.LogError("ProgressPanel NOT found as a child of GameSceneCanvas!");
                }
            }
            else
            {
                Debug.LogError("GameSceneCanvas NOT found in the scene!");
            }
                CreateProgressBar(); // Reinitialize the progress bar
                LoadWord(currentWordIndex);
                UpdateProgressBar();
                UpdateSentenceDisplay();
            }
        }
    }


    private void CreateProgressBar()
    {
        if (progressImagePrefab == null || progressBarContainer == null)
        {
            Debug.LogError("Progress bar prefab or container not assigned!");
            return;
        }

        // Clear existing images (if any)
        foreach (Transform child in progressBarContainer)
        {
            Destroy(child.gameObject);
        }

        progressImages.Clear();

        int wordCountInEra = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra).Count; // Get word count

        for (int i = 0; i < wordCountInEra; i++) // Use word count
        {
            GameObject progressImage = Instantiate(progressImagePrefab, progressBarContainer);
            progressImages.Add(progressImage);
            // Position the images as needed (using LayoutGroup or manual positioning).
        }
    }

    // WordGameManager.cs
    public void HandleCorrectWord()
    {

        solvedWordsInCurrentEra.Add(currentWordIndex);
        solvedWordCountInCurrentEra = solvedWordsInCurrentEra.Count;

        GridManager.Instance.ClearGrid();

        UpdateProgressBar();
        UpdateSentenceDisplay();
    }


    public void StartNewGameInEra()
    {

        currentEraWords = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra);


        if (currentEraWords == null || currentEraWords.Count == 0)
        {

            Debug.LogError("No words found for the current era: " + GameManager.Instance.CurrentEra);
            return;
        }


        solvedWordsInCurrentEra.Clear();
        solvedWordCountInCurrentEra = 0;
        currentWordIndex = 0;
        LoadWord(currentWordIndex);
        UpdateProgressBar();
        UpdateSentenceDisplay();
    }


    private void InitializeUI()
    {
        if (scoreText != null) scoreText.text = "Score: 0";
        if (messageText != null) messageText.text = "";
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
    }

    public void SetupGame(string word, string sentence)
    {
        targetWord = word;
        originalSentence = sentence;
        currentWord = "";
        
        Debug.Log($"Game setup with word: {word} and sentence: {sentence}");
        
        if (sentenceText != null)
        {
            sentenceText.text = originalSentence;
        }
        else
        {
            Debug.LogError("Sentence Text component is not assigned!");
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
                sentenceText.text = originalSentence.Replace("_____", targetWord); // Reveal the word
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

    public void UpdateScore(int points)
    {
        currentScore += points;
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
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

    public void ContinueButton()
    {
        ClearProgressBar(); // Clear progress bar before switching scenes
        // Ensure GridManager and WordGameManager are not destroyed
        DontDestroyOnLoad(GridManager.Instance.gameObject);
        DontDestroyOnLoad(WordGameManager.Instance.gameObject);

        SceneManager.LoadScene("MainMenuScene");
    }

    public bool IsWordSolved(string word)
    {
        if (currentEraWords == null) return false;

        int wordIndex = currentEraWords.IndexOf(word);
        return wordIndex != -1 && solvedWordsInCurrentEra.Contains(wordIndex); // Check index in HashSet
    }

    // Modify LoadWord to not reset the grid for solved words
    public void LoadWord(int index)
    {
        if (currentEraWords == null || index < 0 || index >= currentEraWords.Count)
        {
            Debug.LogWarning("Invalid word index or no words available.");
            return;
        }

        currentWordIndex = index;
        targetWord = currentEraWords[currentWordIndex];
        string sentence = WordValidator.GetSentenceForWord(targetWord, GameManager.Instance.CurrentEra);

        if (sentence == null)
        {
            Debug.LogError($"Sentence is null for {targetWord}");
            return;
        }

        try
        {
            SetupGame(targetWord, sentence);

            // Restore the grid from the initial configuration
            GridManager.Instance.SetupNewPuzzle(targetWord);

            // Restore solved state visually
            if (IsWordSolved(targetWord))
            {
                GridManager.Instance.ClearGrid();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in LoadWord: {e.Message}\n{e.StackTrace}");
        }

        UpdateSentenceDisplay();  // Make sure to update the sentence!
    }

    // Modify NextWord and PreviousWord to not reset grid for solved words
    public void NextWord()
    {
        if (currentEraWords == null)
        {
            Debug.LogError("currentEraWords is null! Cannot navigate.");
            return;
        }

        if (currentWordIndex < currentEraWords.Count - 1)
        {
            currentWordIndex++;
            LoadWord(currentWordIndex); // This now handles solved/unsolved grid setup
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
        else
        {
            Debug.Log("Already at the last word in this era.");
        }
    }

    public void PreviousWord()
    {
        if (currentWordIndex > 0)
        {
            currentWordIndex--;
            LoadWord(currentWordIndex); // This now handles solved/unsolved grid setup
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
        else
        {
            Debug.Log("Already at the first word in this era.");
        }
    }

    public void ClearProgressBar()
    {
        if (progressImages != null)
        {
            foreach (var image in progressImages)
            {
                if (image != null)
                {
                    Destroy(image.gameObject);
                }
            }
            progressImages.Clear();
        }
    }

    public void UpdateProgressBar()
    {
        if (progressImages == null || progressImages.Count == 0)
        {
            Debug.LogWarning("Progress bar not initialized or empty.");
            return;
        }

        Debug.Log($"Updating progress bar. Solved words: {solvedWordCountInCurrentEra}, Current Index: {currentWordIndex}, Total images: {progressImages.Count}");

        for (int i = 0; i < progressImages.Count; i++)
        {
            if (progressImages[i] == null)
            {
                Debug.LogError($"Progress image at index {i} is null!");
                continue;
            }

            RectTransform rectTransform = progressImages[i].GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"RectTransform not found on progress image {i}!");
                continue;
            }

            Image image = progressImages[i].GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError($"Image component not found on progress image {i}!");
                continue;
            }

            if (solvedWordsInCurrentEra.Contains(i))
            {
                image.color = Color.green;
                if (i == currentWordIndex)
                {
                    // Solved and current word: slightly bigger, green
                    rectTransform.localScale = new Vector3(0.39f, 0.39f, 0.39f);
                }
                else
                {
                    // Solved but not current word: normal size, green
                    rectTransform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                }
            }
            else if (i == currentWordIndex)
            {
                // Current word, but not solved: slightly bigger, white
                rectTransform.localScale = new Vector3(0.39f, 0.39f, 0.39f);
                image.color = Color.white;
            }
            else
            {
                // Not solved and not current word: normal size, white
                rectTransform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                image.color = Color.white;
            }
        }
    }

    public void OnNextButtonClicked()
    {
        NextWord();
    }

    public void OnPreviousButtonClicked()
    {
        PreviousWord();
    }
}
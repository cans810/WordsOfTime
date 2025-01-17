using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WordGameManager : MonoBehaviour
{
    [Header("UI References")]
    public SpriteRenderer BackgroundImage;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI sentenceText;

    [Header("Game Settings")]
    [SerializeField] private int correctWordPoints = 100;
    [SerializeField] private Color correctWordColor = Color.green;
    [SerializeField] private Color incorrectWordColor = Color.red;

    private int currentScore = 0;
    private const float MESSAGE_DISPLAY_TIME = 2f;
    private string targetWord;
    private string originalSentence;
    private string currentWord = "";

    public static WordGameManager Instance { get; private set; }

    [Header("Progress Bar")]
    public GameObject progressImagePrefab;
    public Transform progressBarContainer;
    private List<GameObject> progressImages = new List<GameObject>();

    public int currentWordIndex = 0;
    public List<string> currentEraWords;
    public HashSet<int> solvedWordsInCurrentEra = new HashSet<int>();
    public int solvedWordCountInCurrentEra = 0;
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
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
    }

    private void InitializeUI()
    {
        if (scoreText != null) scoreText.text = "Score: 0";
        if (messageText != null) messageText.text = "";
        if (BackgroundImage != null && GameManager.Instance != null)
        {
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
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

        int wordCountInEra = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra).Count;

        for (int i = 0; i < wordCountInEra; i++)
        {
            GameObject progressImage = Instantiate(progressImagePrefab, progressBarContainer);
            progressImages.Add(progressImage);
        }
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

        SetupGame(targetWord, sentence);

        // Now instruct GridManager to load the puzzle from pre-generated data
        GridManager.Instance.SetupNewPuzzle(GameManager.Instance.CurrentEra, targetWord);

        // If the word is already solved, we might skip re-displaying or mark them solved
        if (IsWordSolved(targetWord))
        {
            GridManager.Instance.ClearGrid(); // or let them see the solved state
        }

        UpdateSentenceDisplay();
    }

    public void SetupGame(string word, string sentence)
    {
        targetWord = word;
        originalSentence = sentence;
        currentWord = "";

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
                // reveal
                sentenceText.text = originalSentence.Replace("_____", targetWord);
            }
            else
            {
                // mask
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

    public void HandleCorrectWord()
    {
        solvedWordsInCurrentEra.Add(currentWordIndex);
        solvedWordCountInCurrentEra = solvedWordsInCurrentEra.Count;

        // Clear grid so it doesn't remain
        GridManager.Instance.ClearGrid();
        UpdateProgressBar();
        UpdateSentenceDisplay();
    }

    public void HandleIncorrectWord()
    {
        ShowMessage("Try again!", incorrectWordColor);
        ClearCurrentWord();
    }

    private void ShowMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
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

    public void ClearCurrentWord()
    {
        currentWord = "";
        UpdateSentenceDisplay();
    }

    public void ContinueButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public bool IsWordSolved(string word)
    {
        if (currentEraWords == null) return false;
        int wordIndex = currentEraWords.IndexOf(word);
        return wordIndex != -1 && solvedWordsInCurrentEra.Contains(wordIndex);
    }

    public void NextWord()
    {
        if (currentWordIndex < currentEraWords.Count - 1)
        {
            currentWordIndex++;
            LoadWord(currentWordIndex);
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
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
        }
        else
        {
            Debug.Log("Already at the first word in this era.");
        }
    }

    private void UpdateProgressBar()
    {
        if (progressImages == null || progressImages.Count == 0)
        {
            Debug.LogError("Progress bar not initialized or no words found for this era.");
            return;
        }

        Debug.Log($"Updating progress bar. Solved words: {solvedWordCountInCurrentEra}, " +
            $"Current Index: {currentWordIndex}, Total images: {progressImages.Count}");

        for (int i = 0; i < progressImages.Count; i++)
        {
            if (progressImages[i] == null)
            {
                Debug.LogError($"Progress image at index {i} is null!");
                continue;
            }

            Image image = progressImages[i].GetComponent<Image>();
            RectTransform rectTransform = progressImages[i].GetComponent<RectTransform>();
            if (image == null)
            {
                Debug.LogError($"Image component not found on progress image {i}!");
                continue;
            }

            if (solvedWordsInCurrentEra.Contains(i))
            {
                image.color = Color.green;
                rectTransform.localScale = (i == currentWordIndex)
                    ? new Vector3(0.39f, 0.39f, 0.39f)
                    : new Vector3(0.32f, 0.32f, 0.32f);
            }
            else if (i == currentWordIndex)
            {
                image.color = Color.white;
                rectTransform.localScale = new Vector3(0.39f, 0.39f, 0.39f);
            }
            else
            {
                image.color = Color.white;
                rectTransform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
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

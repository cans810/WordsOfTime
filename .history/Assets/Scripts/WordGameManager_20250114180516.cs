using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    private string targetWord;
    private string originalSentence;
    private string currentWord = "";
    private int solvedWordCountInCurrentEra = 0;

    public static WordGameManager Instance { get; private set; }

    // WordGameManager.cs
    [Header("Progress Bar")]
    public GameObject progressImagePrefab; // Assign in Inspector
    public Transform progressBarContainer; // Assign in Inspector
    private List<GameObject> progressImages = new List<GameObject>();

    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeUI();

        int initialWordCount = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra).Count;
        Debug.Log($"Initial Word Count: {initialWordCount}, Unsolved: {GameManager.Instance.unsolvedWordsInCurrentEra.Count}");
        CreateProgressBar();
        UpdateProgressBar();
    }

    public void StartNewGameInEra()
    {
        solvedWordCountInCurrentEra = 0; // Reset when starting a new game or era
        string nextWord = GridManager.Instance.SelectTargetWord();
        if (nextWord != null)
        {
            UpdateProgressBar();
            UpdateSentenceDisplay();
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

    private void UpdateProgressBar()
    {
        if (progressImages == null || progressImages.Count == 0)
        {
            Debug.LogError("Progress bar not initialized or no words found for this era.");
            return;
        }

        for (int i = 0; i < progressImages.Count; i++)
        {
            Image image = progressImages[i].GetComponent<Image>();

            if (i < solvedWordCountInCurrentEra)
            {
                image.color = Color.green;
            }
            else if (i == solvedWordCountInCurrentEra) // Highlight current word
            {
                image.color = Color.red;
            }
            else
            {
                image.color = Color.white;
            }
        }
    }

    private void UpdateSentenceDisplay()
    {
        if (sentenceText != null && !string.IsNullOrEmpty(originalSentence))
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

    public void HandleCorrectWord()
    {
        UpdateScore(correctWordPoints);
        ShowMessage("Correct!", correctWordColor);

        solvedWordCountInCurrentEra++; // Increment solved word count

        string nextWord = GridManager.Instance.SelectTargetWord(); // Get the next word (or null if era/game is finished)


        if (nextWord != null) 
        {
            UpdateProgressBar();       // Update the progress bar (if there are more words)
            UpdateSentenceDisplay();  // Update the sentence for the new word
        }
        else 
        {
            UpdateProgressBar(); // Still update the progress bar to show full completion (all green)
            // Handle era/game completion here (e.g., show a "Continue" button, go to next scene, etc.)
            Debug.Log("Handle era or game completion logic here!"); // Placeholder for your completion logic

        }
    }

    public void HandleIncorrectWord()
    {
        ShowMessage("Try again!", incorrectWordColor);
        ClearCurrentWord();
    }

    private void ShowContinueOptions()
    {
        // You might want to show a continue button or automatically progress here
        // For now, we'll just load the main menu after a correct word
        SceneManager.LoadScene("MainMenuScene");
    }

    public void ClearCurrentWord()
    {
        currentWord = "";
        UpdateCurrentWord("");
    }

    public void ContinueButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
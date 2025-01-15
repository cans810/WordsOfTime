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

        int solvedWordCount = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra).Count - GameManager.Instance.unsolvedWordsInCurrentEra.Count;


        for (int i = 1; i <= progressImages.Count; i++)
        {
            Image image = progressImages[i].GetComponent<Image>();

            // Correct comparison: i < solvedWordCount
            if (i < solvedWordCount)  // Compare image index with solved count.
            {
                image.color = Color.green;
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

        Debug.Log("Updating Progress Bar");
    UpdateProgressBar();


        GridManager.Instance.SelectTargetWord(); // Get a new word, reset the grid and then setup WordGameManager
        UpdateSentenceDisplay(); // Update the sentence

        string nextWord = GridManager.Instance.SelectTargetWord();

        if (nextWord == null) 
        {
            return; // No more words; GameManager has already handled scene transition.
        }

        UpdateSentenceDisplay();
        
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
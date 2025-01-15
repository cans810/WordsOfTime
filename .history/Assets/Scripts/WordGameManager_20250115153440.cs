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

    private int currentWordIndex = 0;  // Track the current word's index
    private List<string> currentEraWords; // Store words for the current era
    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); //This line is essential to keep the object between scenes

        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeUI();
        StartNewGameInEra(); // Call this to initialize the era's words
        CreateProgressBar();
        UpdateProgressBar();
    }

    public void StartNewGameInEra()
{
    Debug.Log("Starting new game in era: " + GameManager.Instance.CurrentEra);  // Debug log

    currentEraWords = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra);
    if (currentEraWords == null || currentEraWords.Count == 0)
    {
        Debug.LogError("No words found for the current era: " + GameManager.Instance.CurrentEra); // Log the era name
        return; // Handle this error appropriately (e.g., go back to era selection)
    }
    solvedWordCountInCurrentEra = 0; // Reset the word count when you initialize a new era
    currentWordIndex = 0; // Reset word index at start of new era/selected era.

    LoadWord(currentWordIndex);    // Load the first word
    UpdateProgressBar();           // Update the progress bar
    UpdateSentenceDisplay();        // Update the sentence display


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

        Debug.Log($"Updating progress bar. Solved words: {solvedWordCountInCurrentEra}, Current Index: {currentWordIndex}, Total images: {progressImages.Count}");

        for (int i = 0; i < progressImages.Count; i++)
        {
            if (progressImages[i] == null)
            {
                Debug.LogError($"Progress image at index {i} is null!");
                continue;
            }

            Image image = progressImages[i].GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError($"Image component not found on progress image {i}!");
                continue;
            }


            if (i < solvedWordCountInCurrentEra)
            {
                image.color = Color.green;  // Completed words
            }
            else if (i == currentWordIndex)
            {
                image.color = Color.red;    // Current word
            }
            else
            {
                image.color = Color.white;  // Future/unsolved words
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

        solvedWordCountInCurrentEra = Mathf.Min(solvedWordCountInCurrentEra + 1, currentEraWords.Count);

        currentWordIndex = Mathf.Min(currentWordIndex + 1, currentEraWords.Count); // Ensure valid index for the current era, and add a check before incrementing currentWordIndex and calling LoadWord to prevent errors when navigating beyond the end of the era

        if (currentWordIndex < currentEraWords.Count)
        {
            LoadWord(currentWordIndex);
            GridManager.Instance.ResetGridForNewWord();
        }
        else
        {
            Debug.Log("End of current Era!");
            return;
        }


        UpdateProgressBar();
        UpdateSentenceDisplay();

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ResetGridForNewWord();
        }
        else
        {
            Debug.LogError("GridManager.Instance is null!");
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

    public void LoadWord(int index)
    {
        if (currentEraWords == null || index < 0 || index >= currentEraWords.Count)
        {
            Debug.LogWarning("No more words in this era. Returning to menu.");
            GameManager.Instance.MoveToNextEra(); //Go to next era
            return; //Don't continue
        }

        currentWordIndex = index;
        targetWord = currentEraWords[currentWordIndex];
        string sentence = WordValidator.GetSentenceForWord(targetWord, GameManager.Instance.CurrentEra);
        if (sentence == null)
        {
            Debug.LogError($"Sentence is null for {targetWord}");
            return; // or handle error appropriately
        }

        try
        {
            SetupGame(targetWord, sentence);

            if (GridManager.Instance != null)
            {
                GridManager.Instance.SetupNewPuzzle(targetWord);
            }
            else
            {
                Debug.LogError("GridManager.Instance is null!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in LoadWord: {e.Message}\n{e.StackTrace}");
        }
    }

    public void NextWord()
    {
       if (currentWordIndex < currentEraWords.Count - 1) {
            currentWordIndex++;
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
            GridManager.Instance.ResetGridForNewWord();
       }
    }

    public void PreviousWord()
    {
        if (currentWordIndex > 0) {
            currentWordIndex--;
            LoadWord(currentWordIndex);
            UpdateProgressBar();
            UpdateSentenceDisplay();
            GridManager.Instance.ResetGridForNewWord();
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
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
    public int solvedWordCountInCurrentEra = 0;

    public static WordGameManager Instance { get; private set; }

    // WordGameManager.cs
    [Header("Progress Bar")]
    public GameObject progressImagePrefab; // Assign in Inspector
    public Transform progressBarContainer; // Assign in Inspector
    private List<GameObject> progressImages = new List<GameObject>();

    public int currentWordIndex = 0;  // Track the current word's index
    public List<string> currentEraWords; // Store words for the current era

    public HashSet<int> solvedWordsInCurrentEra = new HashSet<int>();

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
        StartNewGameInEra(); // Start the first game immediately
        CreateProgressBar();
    }

    public void StartNewGameInEra()
    {
        Debug.Log("Starting new game in era: " + GameManager.Instance.CurrentEra);

        currentEraWords = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra);
        if (currentEraWords == null || currentEraWords.Count == 0)
        {
            Debug.LogError("No words found for the current era: " + GameManager.Instance.CurrentEra);
            return;
        }
        
        // Reset tracking variables
        solvedWordsInCurrentEra.Clear();
        solvedWordCountInCurrentEra = 0;
        currentWordIndex = 0;

        LoadWord(currentWordIndex);
        UpdateProgressBar();
        GridManager.Instance.SetupNewPuzzle(currentEraWords[currentWordIndex]); // Initialize grid for first word.
        UpdateSentenceDisplay();
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

    public void HandleCorrectWord()
    {
        UpdateScore(correctWordPoints);
        ShowMessage("Correct!", correctWordColor);

        solvedWordsInCurrentEra.Add(currentWordIndex); // Add the *index* to the HashSet
        solvedWordCountInCurrentEra = solvedWordsInCurrentEra.Count;

        if (solvedWordsInCurrentEra.Count == currentEraWords.Count)
        {
            Debug.Log("All words in current era solved! Moving to next era...");
            GameManager.Instance.MoveToNextEra();
            return;
        }

        if (currentWordIndex < currentEraWords.Count - 1)
        {
            currentWordIndex++;
            LoadWord(currentWordIndex);
            GridManager.Instance.ResetGridForNewWord(); // Call ResetGridForNewWord after LoadWord()
        }
        else
        {
            Debug.Log("End of current Era!");
            GameManager.Instance.MoveToNextEra();
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
            GridManager.Instance.SetupNewPuzzle(targetWord); // Call it only ONCE

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in LoadWord: {e.Message}\n{e.StackTrace}");
        }

        UpdateSentenceDisplay(); 
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

            if (solvedWordsInCurrentEra.Contains(i)) // Check against indices
            {
                image.color = Color.green;
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

    public void OnNextButtonClicked()
    {
        NextWord();
    }

    public void OnPreviousButtonClicked()
    {
        PreviousWord();
    }

}
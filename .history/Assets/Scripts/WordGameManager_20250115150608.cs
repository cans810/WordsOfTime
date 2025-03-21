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

        int initialWordCount = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra).Count;
        Debug.Log($"Initial Word Count: {initialWordCount}, Unsolved: {GameManager.Instance.unsolvedWordsInCurrentEra.Count}");
        CreateProgressBar();
        UpdateProgressBar();
    }

    public void StartNewGameInEra()
    {
        Debug.Log("StartNewGameInEra called.");

        solvedWordCountInCurrentEra = 0;
        currentWordIndex = 0;
        currentEraWords = WordValidator.GetWordsForEra(GameManager.Instance.CurrentEra);
        if (currentEraWords == null || currentEraWords.Count == 0)
        {
            Debug.LogError("No words found for this era!");
            return; 
        }

        LoadWord(0);
        Debug.Log("StartNewGameInEra called."); // Add this debug log

        UpdateProgressBar();       // Add try-catch blocks if still getting errors
        UpdateSentenceDisplay();    // Add try-catch blocks if still getting errors
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

    public void LoadWord(int index)
    {
        if (currentEraWords == null || index < 0 || index >= currentEraWords.Count)
        {
            Debug.LogError($"Invalid word index: {index}");
            return;
        }

        currentWordIndex = index;
        targetWord = currentEraWords[currentWordIndex];
        string sentence = WordValidator.GetSentenceForWord(targetWord, GameManager.Instance.CurrentEra);
        if (sentence == null)
        {
            Debug.LogError($"Sentence not found for word: {targetWord} in era: {GameManager.Instance.CurrentEra}");
            return;
        }

        try 
        {
                SetupGame(targetWord, sentence);

        } catch (System.Exception e) {
            Debug.LogError($"Error setting up the game : {e.Message}\n{e.StackTrace}");

        }


        // Add try catch around the grid reset as well
        try {

                if (GridManager.Instance != null) 
                {
                    GridManager.Instance.ResetGridForNewWord();
                } else {
                    Debug.LogError("GridManager.Instance is null!");

                }



        }
        catch (System.Exception e) {
            Debug.LogError($"Error resetting the grid: {e.Message}\n{e.StackTrace}");


        }
    }

    public void NextWord()
    {
        Debug.Log($"Starting NextWord. Current Index: {currentWordIndex}");
        
        if (currentEraWords == null)
        {
            Debug.LogError("currentEraWords is null! Initializing game data...");
            StartNewGameInEra();
            return;
        }

        int nextIndex = currentWordIndex + 1;
        Debug.Log($"Next Index: {nextIndex}, Total Words: {currentEraWords.Count}");

        if (nextIndex < currentEraWords.Count)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager.Instance is null! Cannot proceed.");
                return;
            }

            if (GameManager.Instance.unsolvedWordsInCurrentEra == null)
            {
                Debug.LogError("unsolvedWordsInCurrentEra is null! Reinitializing...");
                GameManager.Instance.InitializeUnsolvedWords();
                return;
            }

            try
            {
                if (GameManager.Instance.unsolvedWordsInCurrentEra.Contains(targetWord))
                {
                    GameManager.Instance.unsolvedWordsInCurrentEra.Remove(targetWord);
                }

                LoadWord(nextIndex);
                // Only increment solved count if we're not going backwards
                solvedWordCountInCurrentEra++;
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
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing next word: {e.Message}\n{e.StackTrace}");
            }
        }
        else // Era complete
        {
            Debug.Log("Reached end of current era words.");
            
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager.Instance is null when trying to move to next era!");
                return;
            }

            GameManager.Instance.MoveToNextEra();

            if (GameManager.Instance.CurrentEra != null)
            {
                Debug.Log("Starting new era...");
                // Reset solved count for new era
                solvedWordCountInCurrentEra = 0;
                StartNewGameInEra();
                CreateProgressBar(); // Create new progress bar for new era
                
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.ResetGridForNewWord();
                }
            }
            else
            {
                Debug.Log("Game Complete - No more eras!");
                // Handle game completion
            }
        }
    }

    public void PreviousWord()
    {
        int prevIndex = currentWordIndex - 1;
        if (prevIndex >= 0)
        {

            GameManager.Instance.unsolvedWordsInCurrentEra.Remove(targetWord); //Remove word manually since we are changing the order

            LoadWord(prevIndex); // Load the next word
            UpdateProgressBar();
            UpdateSentenceDisplay();
            GridManager.Instance.ResetGridForNewWord(); // Reset the grid

            solvedWordCountInCurrentEra--;

        }
        else {

            Debug.Log("no more word");

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
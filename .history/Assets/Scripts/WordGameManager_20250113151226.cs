using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WordGameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Sprite BackgroundImage;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI sentenceText;

    private int currentScore = 0;
    private const float MESSAGE_DISPLAY_TIME = 2f;
    private string targetWord;
    private string originalSentence;
    private string currentWord = "";

    public static WordGameManager Instance { get; private set; }

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
    }

    private void InitializeUI()
    {
        if (scoreText != null) scoreText.text = "Score: 0";
        if (messageText != null) messageText.text = "";
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

        // Update the sentence with the current word
        UpdateSentenceDisplay();
    }

    private void UpdateSentenceDisplay()
    {
        if (sentenceText != null && !string.IsNullOrEmpty(originalSentence))
        {
            string displaySentence = originalSentence;
            
            // If we have some letters selected
            if (!string.IsNullOrEmpty(currentWord))
            {
                // Pad the current word with underscores to match target word length
                string displayWord = currentWord.PadRight(targetWord.Length, '_');
                // Replace the underscores in the sentence with our current word progress
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

    public void ClearCurrentWord()
    {
        currentWord = "";
        UpdateCurrentWord("");
    }
}
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WordGameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI currentWordText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI sentenceText;

    private int currentScore = 0;
    private const float MESSAGE_DISPLAY_TIME = 2f;
    private string targetWord;
    private string currentSentence;

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
        if (currentWordText != null) currentWordText.text = "";
        if (messageText != null) messageText.text = "";
    }

    public void SetupGame(string word, string sentence)
    {
        targetWord = word;
        currentSentence = sentence;
        
        if (sentenceText != null)
        {
            sentenceText.text = currentSentence;
        }
        else
        {
            Debug.LogError("Sentence Text component is not assigned!");
        }
    }

    public void UpdateCurrentWord(string word)
    {
        if (currentWordText != null)
        {
            currentWordText.text = word;
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
}
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
    private string currentSentence = "";
    private string currentEra;

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

        // Get the current era from GameManager
        currentEra = GameManager.Instance.EraSelected;
    }

    private void Start()
    {
        // Initialize UI elements
        UpdateScore(0);
        UpdateCurrentWord("");
        UpdateSentence("");
    }

    private string GetWordFromTiles(List<LetterTile> tiles)
    {
        return string.Join("", tiles.ConvertAll(t => t.Letter.ToString()));
    }

    private int CalculatePoints(string word)
    {
        return word.Length * 100; // Basic scoring: 100 points per letter
    }

    public void UpdateCurrentWord(string word)
    {
        if (currentWordText != null)
        {
            currentWordText.text = word;
        }
    }

    public void UpdateSentence(string sentence)
    {
        if (sentenceText != null)
        {
            currentSentence = sentence;
            sentenceText.text = sentence;
        }
        else
        {
            Debug.LogError("Sentence Text component is not assigned in the Inspector!");
        }
    }

    public void SetWordAndSentence(string word)
    {
        string sentence = WordValidator.GetSentenceForWord(word, currentEra);
        UpdateSentence(sentence);
    }

    private void UpdateScore(int points)
    {
        currentScore += points;
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
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

    private void ClearSelection(List<LetterTile> tiles)
    {
        foreach (var tile in tiles)
        {
            tile.SetSelected(false);
        }
        tiles.Clear();
    }
}
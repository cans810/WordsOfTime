using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WordGameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI currentWordText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI sentencteText;
    
    private int currentScore = 0;
    private const float MESSAGE_DISPLAY_TIME = 2f;
    
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
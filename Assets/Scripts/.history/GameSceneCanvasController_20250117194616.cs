using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameSceneCanvasController : MonoBehaviour
{
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintButtonText;
    private int hintLevel = 1;

    private void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged += OnPointsChanged;
        }
    }

    private void Start()
    {
        UpdateHintButtonText();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged -= OnPointsChanged;
        }
    }

    private void OnPointsChanged()
    {
        Debug.Log("Points changed, updating hint button"); // Debug log
        UpdateHintButtonText();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HomeButtonClicked()
    {
        // Clear current game state before going home
        WordGameManager.Instance.ClearCurrentWord();
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnNextButtonClicked()
    {
        WordGameManager.Instance.NextWord();
    }

    public void OnPreviousButtonClicked()
    {
        WordGameManager.Instance.PreviousWord();
    }

    public void OnHintButtonClicked()
    {
        WordGameManager.Instance.GiveHint();
    }

    public void UpdateHintButtonText()
    {
        if (hintButton != null && hintButtonText != null)
        {
            int hintCost = GameManager.HINT_COST;
            if (hintLevel == 2)
            {
                hintCost = GameManager.SECOND_HINT_COST;
            }

            hintButtonText.text = $"Hint ({hintCost} pts)";
            
            // Update color based on whether player can afford the hint
            bool canAfford = GameManager.Instance.CurrentPoints >= hintCost;
            hintButtonText.color = canAfford ? Color.white : Color.red;
            Debug.Log($"Updated hint button color. Points: {GameManager.Instance.CurrentPoints}, Cost: {hintCost}, Can afford: {canAfford}"); // Debug log
        }
    }
}

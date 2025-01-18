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

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            UpdateHintButtonText();
            GameManager.Instance.OnPointsChanged += UpdateHintButtonText;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged -= UpdateHintButtonText;
        }
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
            if (GameManager.Instance.CurrentPoints >= hintCost)
            {
                hintButtonText.color = Color.white;
            }
            else
            {
                hintButtonText.color = Color.red;
            }
        }
    }
}

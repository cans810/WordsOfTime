using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneCanvasController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
}

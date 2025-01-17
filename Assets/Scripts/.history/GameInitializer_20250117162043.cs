using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found! Please ensure GameManager prefab is in the scene.");
            return;
        }

        // Load the main menu scene
        SceneManager.LoadScene("MainMenuScene");
    }
} 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public SpriteRenderer BackgroundImage;
    private TextMeshProUGUI pointText;
    private SettingsController settingsController;
    public TextMeshProUGUI eraText;

    // Start is called before the first frame update
    void Start()
    {
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);

        // Find point text in main menu
        Transform pointPanel = GameObject.Find("PointPanel")?.transform;
        if (pointPanel != null)
        {
            pointText = pointPanel.Find("point")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Find settings controller including inactive objects
        settingsController = FindInactiveObjectByType<SettingsController>();
        
        UpdatePointsDisplay();

        // Subscribe to era change events if GameManager has them
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged += UpdateEraDisplay;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged -= UpdateEraDisplay;
        }
    }

    // Helper method to find inactive objects
    private T FindInactiveObjectByType<T>() where T : MonoBehaviour
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        if (objects.Length > 0)
        {
            return objects[0];
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePointsDisplay();
    }

    private void UpdatePointsDisplay()
    {
        if (pointText != null)
        {
            pointText.text = GameManager.Instance.CurrentPoints.ToString();
        }

        UpdateEraDisplay();
    }

    private void UpdateEraDisplay()
    {
        if (eraText != null && GameManager.Instance != null)
        {
            string currentEra = GameManager.Instance.CurrentEra;
            
            if (string.IsNullOrEmpty(currentEra))
            {
                eraText.text = "Select Era";
            }
            else
            {
                // Split the era name into words and capitalize each first letter
                string[] words = currentEra.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length > 0)
                    {
                        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                    }
                }
                eraText.text = string.Join(" ", words);
            }

            // Update background image
            if (BackgroundImage != null)
            {
                BackgroundImage.sprite = GameManager.Instance.getEraImage(currentEra);
            }
        }
    }

    public void PlayButton(){
        SceneManager.LoadScene("GameScene");
    }

    public void SelectEraButton(){
        SceneManager.LoadScene("EraSelectionScene");
    }

    public void SettingsButton()
    {
        if (settingsController != null)
        {
            settingsController.ShowSettings();
        }
        else
        {
            Debug.LogError("Settings Controller not found! Make sure it exists in the scene.");
        }
    }
}

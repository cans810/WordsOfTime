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
        
        // Find settings controller
        settingsController = FindObjectOfType<SettingsController>();
        
        UpdatePointsDisplay();
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
            Debug.LogError("Settings Controller not found!");
        }
    }
}

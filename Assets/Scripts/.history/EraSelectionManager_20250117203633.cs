using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EraSelectionManager : MonoBehaviour
{
    public SpriteRenderer BackgroundImage;
    private Dictionary<string, Button> eraButtons = new Dictionary<string, Button>();
    private TextMeshProUGUI pointText;
    
    void Start()
    {
        Debug.Log("EraSelectionManager: Starting initialization");
        
        // Find point text
        Transform pointPanel = GameObject.Find("PointPanel")?.transform;
        if (pointPanel != null)
        {
            pointText = pointPanel.Find("Points")?.GetComponent<TextMeshProUGUI>();
        }

        if (GameManager.Instance != null)
        {
            InitializeEraButtons();
            UpdatePointsDisplay();
            
            if (BackgroundImage != null)
            {
                BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
            }
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }

        UpdateEraPrices();
    }

    private void Update()
    {
        UpdatePointsDisplay();
    }

    private void UpdatePointsDisplay()
    {
        if (pointText != null && GameManager.Instance != null)
        {
            pointText.text = GameManager.Instance.CurrentPoints.ToString();
        }
    }

    private void InitializeEraButtons()
    {
        eraButtons.Clear();
        
        foreach (var era in GameManager.Instance.EraList)
        {
            Transform eraTransform = transform.Find(era);
            if (eraTransform != null)
            {
                Button button = eraTransform.GetComponent<Button>();
                if (button != null)
                {
                    eraButtons[era] = button;
                    
                    // Set up button text
                    TextMeshProUGUI buttonText = eraTransform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = era;
                    }
                    
                    // Make all buttons interactable
                    button.interactable = true;
                }
            }
        }
    }

    private void UpdateEraPrices()
    {
        // Find all era objects
        foreach (Transform eraObject in transform)
        {
            // Find the Points text object
            Transform pointsTextTransform = eraObject.Find("Points");
            if (pointsTextTransform != null)
            {
                TextMeshProUGUI pointsText = pointsTextTransform.GetComponent<TextMeshProUGUI>();
                if (pointsText != null)
                {
                    string eraName = eraObject.name;
                    int price = GameManager.Instance.GetEraPrice(eraName);
                    pointsText.text = price == 0 ? "FREE" : $"{price} POINTS";
                    pointsText.color = price == 0 ? Color.green : Color.white; // Change color based on price
                }
            }
        }
    }

    public void SelectEra(string era)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SwitchEra(era);
            // Load the game scene or perform any other action
        }
    }

    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
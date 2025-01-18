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
    private Dictionary<string, TextMeshProUGUI> eraPointsTexts = new Dictionary<string, TextMeshProUGUI>();

    void Start()
    {
        Debug.Log("EraSelectionManager: Starting initialization");
        
        // Find point text
        Transform pointPanel = GameObject.Find("PointPanel")?.transform;
        if (pointPanel != null)
        {
            pointText = pointPanel.Find("point")?.GetComponent<TextMeshProUGUI>();
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

        InitializeEraPointsTexts();
        UpdateEraUI();
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

    public void SelectEra(string eraName)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectEra(eraName);
            
            if (WordGameManager.Instance != null)
            {
                WordGameManager.Instance.StartNewGameInEra();
            }
            
            if (BackgroundImage != null)
            {
                BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
            }
        }
    }

    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    private void InitializeEraPointsTexts()
    {
        // Find all Points text objects under each era
        foreach (Transform eraObject in transform)
        {
            Transform pointsText = eraObject.Find("Points");
            if (pointsText != null)
            {
                TextMeshProUGUI tmpText = pointsText.GetComponent<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    string eraName = eraObject.name;
                    eraPointsTexts[eraName] = tmpText;
                    Debug.Log($"Found Points text for era: {eraName}"); // Debug log
                }
            }
        }
    }

    private void UpdateEraUI()
    {
        foreach (var era in eraPointsTexts.Keys)
        {
            if (GameManager.Instance.IsEraUnlocked(era))
            {
                eraPointsTexts[era].text = "UNLOCKED";
                eraPointsTexts[era].color = Color.green;
            }
            else
            {
                int price = GameManager.Instance.GetEraPrice(era);
                eraPointsTexts[era].text = $"{price} POINTS";
                eraPointsTexts[era].color = GameManager.Instance.CanUnlockEra(era) ? Color.white : Color.red;
            }
            Debug.Log($"Updated UI for era: {era} - {eraPointsTexts[era].text}"); // Debug log
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged += UpdateEraUI;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged -= UpdateEraUI;
        }
    }
}
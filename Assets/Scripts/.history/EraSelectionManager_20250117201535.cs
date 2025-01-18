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

    private void Awake()
    {
        // Make sure GameManager is created first
        if (GameManager.Instance == null)
        {
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        // Wait until GameManager is ready
        while (GameManager.Instance == null)
        {
            yield return null;
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
}
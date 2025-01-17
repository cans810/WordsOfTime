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
    private Dictionary<string, TextMeshProUGUI> eraPointTexts = new Dictionary<string, TextMeshProUGUI>();

    void Start()
    {
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        InitializeEraButtons();
        FindPointTexts();
        UpdateEraButtons();
        UpdateAllPointTexts();
    }

    private void InitializeEraButtons()
    {
        // Clear existing buttons
        eraButtons.Clear();

        // Find buttons for each era
        foreach (var era in GameManager.Instance.EraList)
        {
            Transform eraTransform = transform.Find(era);
            if (eraTransform != null)
            {
                Button button = eraTransform.GetComponent<Button>();
                if (button != null)
                {
                    eraButtons[era] = button;
                }
            }
        }
    }

    private void Update()
    {
        UpdateAllPointTexts();
    }

    private void FindPointTexts()
    {
        // Find all Points TextMeshProUGUI components under each era button
        foreach (var era in GameManager.Instance.EraList)
        {
            Transform eraTransform = transform.Find(era);
            if (eraTransform != null)
            {
                TextMeshProUGUI pointText = eraTransform.Find("Points")?.GetComponent<TextMeshProUGUI>();
                if (pointText != null)
                {
                    eraPointTexts[era] = pointText;
                }
            }
        }
    }

    private void UpdateAllPointTexts()
    {
        foreach (var pointText in eraPointTexts.Values)
        {
            pointText.text = GameManager.Instance.CurrentPoints.ToString();
        }
    }

    private void UpdateEraButtons()
    {
        foreach (var era in GameManager.Instance.EraList)
        {
            Transform eraTransform = transform.Find(era);
            if (eraTransform != null)
            {
                Button button = eraButtons[era];
                bool isUnlocked = GameManager.Instance.IsEraUnlocked(era);
                button.interactable = isUnlocked;
                
                // Find the Text (TMP) component directly under the era transform
                TextMeshProUGUI buttonText = eraTransform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                if (buttonText != null && !isUnlocked)
                {
                    int requiredPoints = GameManager.Instance.GetEraUnlockRequirement(era);
                    buttonText.text = $"{era}\n({requiredPoints} points)";
                }
                else if (buttonText != null)
                {
                    buttonText.text = era; // Just show era name if unlocked
                }
            }
        }
    }

    public void SelectEra(string eraName)
    {
        if (!GameManager.Instance.IsEraUnlocked(eraName))
        {
            // Show message that era is locked
            return;
        }

        GameManager.Instance.SelectEra(eraName);
        WordGameManager.Instance.StartNewGameInEra();
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
    }

    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
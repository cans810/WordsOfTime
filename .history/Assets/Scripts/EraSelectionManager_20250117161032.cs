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
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance not found!");
            return;
        }

        InitializeEraButtons();
        FindPointTexts();
        UpdateEraButtons();
        UpdateAllPointTexts();
        
        if (BackgroundImage != null)
        {
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        }
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
        UpdateEraButtons();
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
        int currentPoints = GameManager.Instance.CurrentPoints;
        foreach (var pointText in eraPointTexts.Values)
        {
            if (pointText != null && pointText.text != currentPoints.ToString())
            {
                pointText.text = currentPoints.ToString();
            }
        }
    }

    private void UpdateEraButtons()
    {
        if (GameManager.Instance == null) return;

        foreach (var era in GameManager.Instance.EraList)
        {
            Transform eraTransform = transform.Find(era);
            if (eraTransform != null && eraButtons.ContainsKey(era))
            {
                Button button = eraButtons[era];
                bool isUnlocked = GameManager.Instance.IsEraUnlocked(era);
                button.interactable = isUnlocked;
                
                TextMeshProUGUI buttonText = eraTransform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    string newText = isUnlocked ? era : $"{era}\n({GameManager.Instance.GetEraUnlockRequirement(era)} points)";
                    buttonText.text = newText;
                }
            }
        }
    }

    public void SelectEra(string eraName)
    {
        if (GameManager.Instance.CurrentPoints >= GameManager.Instance.GetEraUnlockRequirement(eraName))
        {
            GameManager.Instance.SelectEra(eraName);
            WordGameManager.Instance.StartNewGameInEra();
            BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        }
        else
        {
            Debug.Log($"Cannot select {eraName} - requires {GameManager.Instance.GetEraUnlockRequirement(eraName)} points");
        }
    }

    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
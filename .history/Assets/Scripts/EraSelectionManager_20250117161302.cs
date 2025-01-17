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
    private List<TextMeshProUGUI> pointTexts = new List<TextMeshProUGUI>();
    
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
    }

    private void FindPointTexts()
    {
        pointTexts.Clear();
        
        Transform canvas = transform.Find("EraSelectionCanvas");
        if (canvas == null)
        {
            Debug.LogError("EraSelectionCanvas not found!");
            return;
        }

        TextMeshProUGUI[] allPointTexts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        Debug.Log($"Found {allPointTexts.Length} TextMeshProUGUI components in total");
        
        foreach (var text in allPointTexts)
        {
            Debug.Log($"Checking text component: {text.name} in {text.transform.parent.name}");
            if (text.name == "Points")
            {
                pointTexts.Add(text);
                Debug.Log($"Added Points text from {text.transform.parent.name}");
            }
        }
        
        Debug.Log($"Total Points texts found and added: {pointTexts.Count}");
    }

    private void UpdateAllPointTexts()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance is null!");
            return;
        }

        int currentPoints = GameManager.Instance.CurrentPoints;
        Debug.Log($"Updating all point texts to: {currentPoints}");
        
        foreach (var pointText in pointTexts)
        {
            if (pointText != null)
            {
                string oldText = pointText.text;
                pointText.text = currentPoints.ToString();
                Debug.Log($"Updated point text in {pointText.transform.parent.name} from {oldText} to {currentPoints}");
            }
            else
            {
                Debug.LogWarning("Found null point text reference!");
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
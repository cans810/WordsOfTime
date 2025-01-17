using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EraSelectionManager : MonoBehaviour
{
    public SpriteRenderer BackgroundImage;
    [SerializeField] private Dictionary<string, Button> eraButtons;

    void Start()
    {
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        UpdateEraButtons();
    }

    private void UpdateEraButtons()
    {
        foreach (var era in GameManager.Instance.EraList)
        {
            if (eraButtons.ContainsKey(era))
            {
                bool isUnlocked = GameManager.Instance.IsEraUnlocked(era);
                eraButtons[era].interactable = isUnlocked;
                
                // Update button text to show required points if locked
                TextMeshProUGUI buttonText = eraButtons[era].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null && !isUnlocked)
                {
                    int requiredPoints = GameManager.Instance.GetEraUnlockRequirement(era);
                    buttonText.text = $"{era}\n({requiredPoints} points)";
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
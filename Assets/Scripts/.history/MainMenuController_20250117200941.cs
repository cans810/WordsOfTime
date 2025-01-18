using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    private Dictionary<string, TextMeshProUGUI> eraPointsTexts = new Dictionary<string, TextMeshProUGUI>();

    private void Start()
    {
        InitializeEraPointsTexts();
        UpdateEraUI();
    }

    private void InitializeEraPointsTexts()
    {
        // Find all Points text objects under each era
        Transform eraSelectionCanvas = transform.Find("EraSelectionCanvas");
        if (eraSelectionCanvas != null)
        {
            foreach (Transform eraObject in eraSelectionCanvas)
            {
                Transform pointsText = eraObject.Find("Points");
                if (pointsText != null)
                {
                    TextMeshProUGUI tmpText = pointsText.GetComponent<TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        string eraName = eraObject.name;
                        eraPointsTexts[eraName] = tmpText;
                    }
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
        }
    }

    // Call this whenever points change or an era is unlocked
    public void OnPointsChanged()
    {
        UpdateEraUI();
    }
} 
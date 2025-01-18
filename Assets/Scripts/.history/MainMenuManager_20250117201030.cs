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
    private Dictionary<string, TextMeshProUGUI> eraPointsTexts = new Dictionary<string, TextMeshProUGUI>();

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
        
        UpdatePointsDisplay();
        InitializeEraPointsTexts();
        UpdateEraUI();
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
                        Debug.Log($"Found Points text for era: {eraName}"); // Debug log
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

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
        Debug.Log("EraSelectionManager: Starting initialization");
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return null; // Wait one frame

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance not found! Creating one...");
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
            yield return null;
        }

        try
        {
            InitializeEraButtons();
            FindPointTexts();
            UpdateEraButtons();
            UpdateAllPointTexts();
            
            if (BackgroundImage != null && GameManager.Instance != null)
            {
                BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
            }
            else
            {
                Debug.LogWarning("BackgroundImage or GameManager.Instance is null");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during EraSelectionManager initialization: {e.Message}\n{e.StackTrace}");
        }
    }

    private void InitializeEraButtons()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null during InitializeEraButtons");
            return;
        }

        eraButtons.Clear();
        
        foreach (var era in GameManager.Instance.EraList)
        {
            try
            {
                Transform eraTransform = transform.Find(era);
                if (eraTransform != null)
                {
                    Button button = eraTransform.GetComponent<Button>();
                    if (button != null)
                    {
                        eraButtons[era] = button;
                        Debug.Log($"Successfully initialized button for era: {era}");
                    }
                    else
                    {
                        Debug.LogWarning($"Button component not found for era: {era}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Transform not found for era: {era}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error initializing button for era {era}: {e.Message}");
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
        foreach (var text in allPointTexts)
        {
            if (text.name == "Points")
            {
                pointTexts.Add(text);
            }
        }
    }

    private void UpdateAllPointTexts()
    {
        if (GameManager.Instance == null) return;

        foreach (var pointText in pointTexts)
        {
            if (pointText != null)
            {
                pointText.text = GameManager.Instance.CurrentPoints.ToString();
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
                button.interactable = true;
                
                TextMeshProUGUI buttonText = eraTransform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = era;
                }
            }
        }
    }

    public void SelectEra(string eraName)
    {
        GameManager.Instance.SelectEra(eraName);
        WordGameManager.Instance.StartNewGameInEra();
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
    }

    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
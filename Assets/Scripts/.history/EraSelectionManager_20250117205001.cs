using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EraSelectionManager : MonoBehaviour
{
    public Transform EraSelectionCanvas;

    [SerializeField] private SpriteRenderer backgroundImage;

    private void Start()
    {
        UpdateEraPrices();
    }

    private void UpdateEraPrices()
    {
        // Iterate through each child of the EraSelectionManager
        foreach (Transform eraObject in EraSelectionCanvas)
        {
            // Find the Points text object
            Transform pointsTextTransform = eraObject.Find("Points");
            if (pointsTextTransform != null)
            {
                TextMeshProUGUI pointsText = pointsTextTransform.GetComponent<TextMeshProUGUI>();
                if (pointsText != null)
                {
                    string eraName = eraObject.name; // Get the name of the era
                    int price = GameManager.Instance.GetEraPrice(eraName); // Get the price for the era
                    pointsText.text = price == 0 ? "FREE" : $"{price} POINTS"; // Update text
                    pointsText.color = price == 0 ? Color.green : Color.white; // Change color based on price
                }
            }
        }
    }

    public void SelectEra(string era)
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CanUnlockEra(era))
            {
                GameManager.Instance.SwitchEra(era);
                UpdateBackgroundImage(era);
                // Load the game scene or perform any other action
            }
            else
            {
                Debug.Log("Not enough points to unlock this era.");
                // Optionally show a message to the player
            }
        }
    }

    private void UpdateBackgroundImage(string era)
    {
        // Get the background image for the selected era
        Sprite newBackground = GameManager.Instance.getEraImage(era);
        if (newBackground != null && backgroundImage != null)
        {
            backgroundImage.sprite = newBackground;
        }
        else
        {
            Debug.LogError($"Background image not found for era: {era}");
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
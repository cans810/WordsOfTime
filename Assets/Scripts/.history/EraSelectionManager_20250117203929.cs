using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EraSelectionManager : MonoBehaviour
{
    private void Start()
    {
        UpdateEraPrices();
    }

    private void UpdateEraPrices()
    {
        // Iterate through each child of the EraSelectionManager
        foreach (Transform eraObject in transform)
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
            GameManager.Instance.SwitchEra(era);
            // Load the game scene or perform any other action
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Change to your main menu scene name
    }
}
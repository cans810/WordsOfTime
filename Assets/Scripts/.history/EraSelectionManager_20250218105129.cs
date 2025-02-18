using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class EraSelectionManager : MonoBehaviour
{
    public Transform EraSelectionCanvas;

    [SerializeField] private Image backgroundImage;

    public Animator animator;

    public void Awake()
    {
        gameObject.GetComponent<Canvas>().sortingLayerName = "BackgroundImage";
        gameObject.GetComponent<Canvas>().sortingOrder = -1;
    }

    public void UpdateEraPrices()
    {
        // Get the unlocked text based on current language
        string unlockedText = GameManager.Instance.CurrentLanguage == "tr" ? "AÇIK" : "UNLOCKED";

        // Find the ScrollArea and then the Eras object
        Transform safeArea = EraSelectionCanvas.Find("SafeArea");

        Transform scrollArea = safeArea.Find("ScrollArea");
        
        if (scrollArea == null)
        {
            Debug.LogError("ScrollArea not found!");
            return;
        }

        Transform erasParent = scrollArea.Find("Eras");
        if (erasParent == null)
        {
            Debug.LogError("Eras object not found!");
            return;
        }

        // Iterate through each era under Eras
        foreach (Transform eraObject in erasParent)
        {
            // Find the Points text object, first in immediate children then in bg's children
            Transform pointsTextTransform = eraObject.Find("Points");
            Transform buyButtonTransform = eraObject.Find("Buy");
            Transform coinTransform = eraObject.Find("coin");
            Transform bgTransform = eraObject.Find("bg");

            // If Points not found in immediate children, look in bg's children
            if (pointsTextTransform == null && bgTransform != null)
            {
                pointsTextTransform = bgTransform.Find("Points");
            }

            string eraName = eraObject.name; // Get the name of the era

            if (pointsTextTransform != null)
            {
                TextMeshProUGUI pointsText = pointsTextTransform.GetComponent<TextMeshProUGUI>();
                if (pointsText != null)
                {
                    if (GameManager.Instance.IsEraUnlocked(eraName)) // Check if the era is unlocked
                    {
                        pointsText.text = unlockedText; // Use language-specific text
                        pointsText.color = Color.green; // Change color to green

                        // If era is unlocked and not Ancient Egypt or Medieval Europe, disable buy button
                        if (buyButtonTransform != null && 
                            eraName != "Ancient Egypt" && 
                            eraName != "Medieval Europe")
                        {
                            buyButtonTransform.gameObject.SetActive(false);
                        }

                        // Disable coin and adjust bg position
                        if (coinTransform != null)
                        {
                            coinTransform.gameObject.SetActive(false);
                        }

                        if (bgTransform != null)
                        {
                            Vector3 position = bgTransform.localPosition;
                            position.x = 0;
                            bgTransform.localPosition = position;
                        }
                    }
                    else
                    {
                        int price = GameManager.Instance.GetEraPrice(eraName); // Get the price for the era
                        bool canAfford = GameManager.Instance.CurrentPoints >= price;
                        pointsText.text = price == 0 ? unlockedText : $"{price}"; // Use language-specific text for free eras
                        pointsText.color = canAfford ? Color.green : Color.red; // Change color based on affordability

                        // Make sure buy button is visible for locked eras
                        if (buyButtonTransform != null)
                        {
                            buyButtonTransform.gameObject.SetActive(true);
                        }

                        // Make sure coin is visible for locked eras
                        if (coinTransform != null)
                        {
                            coinTransform.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
    }

    private void SetInitialBackgroundImage()
    {
        // Get the background image for the current era
        Sprite initialBackground = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        if (initialBackground != null && backgroundImage != null)
        {
            backgroundImage.sprite = initialBackground;
        }
        else
        {
            Debug.LogError($"Background image not found for era: {GameManager.Instance.CurrentEra}");
        }
    }

    public void SelectEra(string era)
    {
        Debug.Log($"=== EraSelectionManager.SelectEra ===");
        Debug.Log($"Attempting to select era: '{era}'");
        Debug.Log($"Current unlocked eras: {string.Join(", ", GameManager.Instance.GetUnlockedEras())}");
        
        if (GameManager.Instance != null)
        {
            bool isUnlocked = GameManager.Instance.IsEraUnlocked(era);
            Debug.Log($"Is era '{era}' unlocked? {isUnlocked}");
            
            if (isUnlocked)
            {
                Debug.Log($"Selecting unlocked era: '{era}'");
                GameManager.Instance.SelectEra(era);
                UpdateBackgroundImage(era);
            }
            else
            {
                Debug.LogWarning($"Cannot select locked era: '{era}'");
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

    public void UnlockEra(string era)
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CanUnlockEra(era))
            {
                int price = GameManager.Instance.GetEraPrice(era);
                GameManager.Instance.CurrentPoints -= price; // Deduct points
                GameManager.Instance.UnlockEra(era); // Unlock the specific era
                UpdateEraPrices(); // Refresh the UI
                Debug.Log($"Era {era} unlocked successfully!");
            }
            else
            {
                Debug.Log($"Not enough points to unlock this {era} era.");
                // Optionally show a message to the player
            }
        }
    }

    public void ShowEraSelectionScreen()
    {
        gameObject.SetActive(true);
        StartCoroutine(InitializeEraSelectionDelayed());
    }

    private IEnumerator InitializeEraSelectionDelayed()
    {
        gameObject.GetComponent<Canvas>().sortingLayerName = "UI";
        gameObject.GetComponent<Canvas>().sortingOrder = 2;
        // Update background first for visual feedback
        SetInitialBackgroundImage();
        
        yield return new WaitForEndOfFrame();
        
        // Update prices after a frame
        UpdateEraPrices();
    }

    public void OnReturnButtonClickedPlayAnimation()
    {
        animator.SetBool("DeLoad", true);
    }

    public void OnReturnLanguageButtonClickedSetDeactive()
    {
        gameObject.GetComponent<Canvas>().sortingLayerName = "BackgroundImage";
        gameObject.GetComponent<Canvas>().sortingOrder = -1;
        gameObject.SetActive(false);
    }
}
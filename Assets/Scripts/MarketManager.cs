using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;

public class MarketManager : MonoBehaviour, IStoreListener
{
    public SpriteRenderer BackgroundImage;
    public TextMeshProUGUI pointsText;
    private int currentPoints;
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    private const string NO_ADS_PRODUCT_ID = "No Ads";

    public Animator animator;

    // Example point packages
    private readonly Dictionary<string, int> pointPackages = new Dictionary<string, int>
    {
        { "200 Points", 200 },
        { "600 Points", 600 },
        { "1500 Points", 1500 },
        { "3500 Points", 3500 },
        { "8000 Points", 8000 },
    };

    private Coroutine pointAnimationCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        // Initialize current points from GameManager or PlayerPrefs
        currentPoints = GameManager.Instance.CurrentPoints;
        UpdatePointsDisplay();
        InitializePurchasing();
    }

    private void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        
        // Add products
        builder.AddProduct("200 Points", ProductType.Consumable);
        builder.AddProduct("600 Points", ProductType.Consumable);
        builder.AddProduct("1500 Points", ProductType.Consumable);
        builder.AddProduct("3500 Points", ProductType.Consumable);
        builder.AddProduct("8000 Points", ProductType.Consumable);

        builder.AddProduct(NO_ADS_PRODUCT_ID, ProductType.NonConsumable);


        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        Debug.Log("IAP Initialization successful!");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"IAP Initialization failed: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"IAP Initialization failed: {error}. Message: {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        
        if (productId == NO_ADS_PRODUCT_ID)
        {
            // Handle No Ads purchase
            GameManager.Instance.SetNoAds(true);
            SaveManager.Instance.SaveGame();
            Debug.Log("No Ads purchased successfully!");
        }
        else if (pointPackages.ContainsKey(productId))
        {
            // Handle points purchase
            int pointsToAdd = pointPackages[productId];
            AddPoints(pointsToAdd);
            Debug.Log($"Purchase successful: {productId}");
        }
        else
        {
            Debug.LogWarning($"Product {productId} not found.");
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"Purchase of {product.definition.id} failed due to {failureReason}");
    }

    private void AddPoints(int points)
    {
        int startPoints = currentPoints;
        currentPoints += points;
        GameManager.Instance.CurrentPoints = currentPoints;
        
        // Stop any existing animation
        if (pointAnimationCoroutine != null)
        {
            StopCoroutine(pointAnimationCoroutine);
        }
        
        // Start new animation
        pointAnimationCoroutine = StartCoroutine(AnimatePointsChange(startPoints, currentPoints));
        
        // Save the game after updating points
        SaveManager.Instance.SaveGame();
        
        Debug.Log($"Added {points} points. Total points: {currentPoints}");
    }

    private IEnumerator AnimatePointsChange(int startPoints, int endPoints)
    {
        float elapsedTime = 0f;
        Vector3 originalScale = pointsText.transform.localScale;
        Color originalColor = pointsText.color;
        bool isIncreasing = endPoints > startPoints;
        float animationDuration = 1.1f;
        int lastPoints = startPoints;
        Coroutine currentBumpCoroutine = null;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Polynomial easing - starts slow, accelerates faster
            float t = elapsedTime / animationDuration;
            t = t * t * (3 - 2 * t); // Smoother cubic easing
            
            // Calculate current points with accelerating step size
            int currentPoints = Mathf.RoundToInt(Mathf.Lerp(startPoints, endPoints, t));
            
            // If points value changed, create a bump effect
            if (currentPoints != lastPoints)
            {
                // Stop any existing bump animation
                if (currentBumpCoroutine != null)
                {
                    StopCoroutine(currentBumpCoroutine);
                    pointsText.transform.localScale = originalScale;
                }
                
                // Start new bump animation
                currentBumpCoroutine = StartCoroutine(BumpScale(pointsText.transform, originalScale));
                lastPoints = currentPoints;
            }
            
            // Update points display
            if (pointsText != null)
            {
                pointsText.text = currentPoints.ToString();
                pointsText.color = isIncreasing ? Color.green : Color.red;
            }
            
            yield return null;
        }
        
        // Ensure we end up at the exact final value and return to original color
        if (pointsText != null)
        {
            pointsText.text = endPoints.ToString();
            pointsText.transform.localScale = originalScale;
            pointsText.color = originalColor;
        }
    }

    private IEnumerator BumpScale(Transform target, Vector3 originalScale)
    {
        float bumpDuration = 0.008f;
        float elapsedTime = 0f;
        float maxScale = 1.2f;
        
        while (elapsedTime < bumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / bumpDuration;
            
            // Smoother bump curve
            float scale = 1f + (maxScale - 1f) * (1f - (2f * t - 1f) * (2f * t - 1f));
            target.localScale = originalScale * scale;
            
            yield return null;
        }
        
        // Ensure we return to original scale
        target.localScale = originalScale;
    }

    private void UpdatePointsDisplay()
    {
        if (pointsText != null)
        {
            // Get the current points from GameManager
            currentPoints = GameManager.Instance.CurrentPoints;
            // Just display the points amount
            pointsText.text = currentPoints.ToString();
        }
    }

    public bool IsNoAdsPurchased()
    {
        if (storeController != null && storeController.products.WithID(NO_ADS_PRODUCT_ID) != null)
        {
            return storeController.products.WithID(NO_ADS_PRODUCT_ID).hasReceipt;
        }
        return false;
    }

    // Method to initiate No Ads purchase
    public void BuyNoAds()
    {
        BuyProduct(NO_ADS_PRODUCT_ID);
    }

    // Method to initiate purchase
    public void BuyProduct(string productId)
    {
        if (storeController != null && storeController.products.WithID(productId) != null)
        {
            storeController.InitiatePurchase(productId);
            Debug.Log($"Initiating purchase for {productId}");
        }
        else
        {
            Debug.LogError($"Failed to purchase {productId}: Store controller not initialized or product not found");
        }
    }

    public void OnReturnButtonPressed()
    {
        animator.SetBool("DeLoad", true);
    }

    public void OnReturnButtonClickedSetDeactive()
    {
        gameObject.GetComponent<Canvas>().sortingLayerName = "BackgroundImage";
        gameObject.GetComponent<Canvas>().sortingOrder = -2;
        gameObject.SetActive(false);
    }

    public void ShowMarketScreen()
    {
        gameObject.SetActive(true);
        gameObject.GetComponent<Canvas>().sortingLayerName = "UI";
        gameObject.GetComponent<Canvas>().sortingOrder = 3;
    
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        // Initialize current points from GameManager or PlayerPrefs
        currentPoints = GameManager.Instance.CurrentPoints;
        UpdatePointsDisplay();
        InitializePurchasing();
    }
}

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

    // Example point packages
    private readonly Dictionary<string, int> pointPackages = new Dictionary<string, int>
    {
        { "100 Points", 100 },
        { "600 Points", 600 },
        { "1500 Points", 1500 },
        { "3500 Points", 3500 },
        { "8000 Points", 8000 },
    };

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
        builder.AddProduct("100 Points", ProductType.Consumable);
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
        currentPoints += points;
        GameManager.Instance.CurrentPoints = currentPoints;
        UpdatePointsDisplay();
        
        // Save the game after updating points
        SaveManager.Instance.SaveGame();
        
        Debug.Log($"Added {points} points. Total points: {currentPoints}");
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
        SceneManager.LoadScene("MainMenuScene");
    }
}

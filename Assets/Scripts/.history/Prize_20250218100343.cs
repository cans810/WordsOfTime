using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Prize : MonoBehaviour
{
    public string prizeName;
    public string prizeValue; // Now a string

    public TextMeshProUGUI prizeText;
    
    // Optional: Add a method to initialize the prize
    public void InitializePrize(string name, string value)
    {
        // Check game language and modify prize name if needed
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentLanguage == "tr")
            {
                // Example: Translate prize name to Turkish
                prizeName = TranslateToTurkish(name);
            }
            else
            {
                // Default to English
                prizeName = name;
            }
        }
        else
        {
            // If GameManager is not available, use default name
            prizeName = name;
        }
        
        prizeValue = value;

        prizeText = prizeValue;
    }

    public void Awake(){
        InitializePrize(prizeName, prizeValue);
    }

    private string TranslateToTurkish(string englishName)
    {
        if (englishName.ToLower().Contains("points"))
        {
            return englishName.ToLower().Replace("points", "Puan");
        }

        switch (englishName.ToLower())
        {
            case "random era unlocked":
                return "Rastgele Çağ Açıldı";
            case "try again later":
                return "Sonra Tekrar Dene";
            case "key":
                return "Anahtar";
            default:
                return englishName; 
        }
    }
}
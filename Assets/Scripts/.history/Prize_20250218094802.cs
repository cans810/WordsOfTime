using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prize : MonoBehaviour
{
    public string prizeName;
    public int prizeValue;
    
    // Optional: Add a method to initialize the prize
    public void InitializePrize(string name, int value)
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
    }

    private string TranslateToTurkish(string englishName)
{
    // Check if the string contains "Points" and translate it
    if (englishName.ToLower().Contains("points"))
    {
        return englishName.ToLower().Replace("points", "Puan");
    }

    // Add your translation logic here
    switch (englishName.ToLower())
    {
        case "coin":
            return "Madeni Para";
        case "gem":
            return "Değerli Taş";
        case "key":
            return "Anahtar";
        // Add more translations as needed
        default:
            return englishName; // Return original if no translation found
    }
}
}
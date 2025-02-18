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
            prizeName = name;
        }
        
        prizeValue = value;
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
            case "try again":
                return "Tekrar Dene";
            case "key":
                return "Anahtar";
            default:
                return englishName; 
        }
    }
}
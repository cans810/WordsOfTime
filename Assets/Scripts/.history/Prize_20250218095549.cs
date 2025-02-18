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
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentLanguage == "tr")
            {
                prizeName = TranslateToTurkish(name);
            }
            else
            {
                // Default to English
                prizeName = name;
            }
            Debug.LogError($"Prize name: {prizeName}");
            Debug.LogError($"Language: {GameManager.Instance.CurrentLanguage}");
        }
        else
        {
            prizeName = name;
        }
        
        prizeValue = value;
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
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
        prizeName = name;
        prizeValue = value;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using TMPro;


public class WheelOfFortuneController : MonoBehaviour
{
    [SerializeField] private float initialSpinSpeed = 1000f;
    [SerializeField] private float spinDuration = 10f;
    [SerializeField] private TrianglePickerController trianglePicker; // Add reference to picker

    public Transform wheel;
    public List<Prize> prizes;
    public Button spinButton;
    public 
    
    private bool isSpinning = false;

    // Update weights to match the wheel's visual order (clockwise from 1000)
    private List<float> prizeWeights = new List<float> { 
        8f,   // 1000
        16f,  // 500
        48f,  // 250
        8f,   // Random Era
        16f,  // 500
        20f,  // Try Again
        48f   // 250
    };

    // Add these at class level to track statistics
    private Dictionary<string, int> prizeStats = new Dictionary<string, int>();
    private int totalSpins = 0;

    // Define segment angles for each prize (clockwise from 1000 at top)
    private readonly float[] segmentStartAngles = {
        270f,     // 1000 (top)
        321.43f,  // 500
        12.86f,   // 250
        64.29f,   // Random Era
        115.72f,  // 500
        167.15f,  // Try Again
        218.58f   // 250
    };

    private const string LAST_SPIN_TIME_KEY = "LastWheelSpinTime";
    private const int COOLDOWN_HOURS = 24;
    
    private bool canSpin = true;
    private float remainingCooldown = 0f;

    public TextMeshProUGUI prizetext;
        
    private void Start()
    {
        ResetCooldown();
        prizetext.gameObject.SetActive(false);
        UpdateSpinAvailability();
    }

    void Update()
    {
        if (!canSpin)
        {
            UpdateSpinAvailability();

            int hours = Mathf.FloorToInt(remainingCooldown / 3600);
            int minutes = Mathf.FloorToInt((remainingCooldown % 3600) / 60);
            Debug.Log($"Next spin in: {hours}h {minutes}m");
        }
    }

    private void UpdateSpinAvailability()
    {
        string lastSpinTimeStr = PlayerPrefs.GetString(LAST_SPIN_TIME_KEY, "");
        
        if (string.IsNullOrEmpty(lastSpinTimeStr))
        {
            canSpin = true;
            remainingCooldown = 0f;
        }
        else
        {
            DateTime lastSpinTime = DateTime.Parse(lastSpinTimeStr);
            DateTime nextAvailableTime = lastSpinTime.AddHours(COOLDOWN_HOURS);
            TimeSpan timeUntilNextSpin = nextAvailableTime - DateTime.Now;
            
            canSpin = timeUntilNextSpin.TotalSeconds <= 0;
            remainingCooldown = (float)Math.Max(0, timeUntilNextSpin.TotalSeconds);
        }
        

        wheel.gameObject.SetActive(canSpin);
        spinButton.gameObject.SetActive(canSpin);
        trianglePicker.gameObject.SetActive(canSpin);
    }
    
    private void SaveSpinTime()
    {
        PlayerPrefs.SetString(LAST_SPIN_TIME_KEY, DateTime.Now.ToString("o")); // ISO 8601 format
        PlayerPrefs.Save();
        UpdateSpinAvailability();
    }
    
    // Modify your existing spin method to check availability
    public void StartSpin()
    {
        if (!isSpinning && canSpin)
        {
            StartCoroutine(SpinWheel());
        }
    }
    
    // Optional: Method to reset the cooldown (for testing)
    public void ResetCooldown()
    {
        PlayerPrefs.DeleteKey(LAST_SPIN_TIME_KEY);
        PlayerPrefs.Save();
        UpdateSpinAvailability();
    }

    private Prize GetRandomPrize()
    {
        // Calculate total weight
        float totalWeight = prizeWeights.Sum();
        float randomValue = UnityEngine.Random.Range(0, totalWeight);
        
        // Find which prize corresponds to the random value
        float weightSum = 0;
        for (int i = 0; i < prizes.Count; i++)
        {
            weightSum += prizeWeights[i];
            if (randomValue < weightSum)
            {
                return prizes[i];
            }
        }
        
        // Fallback to first prize if something goes wrong
        return prizes[0];
    }

    private void ShowPrizeWon(Prize prize)
    {
        prizetext.gameObject.SetActive(true);
        prizetext.text = $"{prize.name}!";
    }

    private void GivePrizeToPlayer(Prize prize)
    {
        // Implement this based on your prize system
        // For example:
        switch (prize.name)
        {
            case "1000 Points":
                GameManager.Instance.AddPoints(1000);
                break;
            case "500 Points":
                GameManager.Instance.AddPoints(500);
                break;
            case "250 Points":
                GameManager.Instance.AddPoints(250);
                break;
            case "Random Era Unlocked":
                //unlock random era
                break;
            case "Try Again Later":
                break;
        }
    }

    private IEnumerator SpinWheel()
    {
        isSpinning = true;
        float elapsedTime = 0f;
        
        // Get random weighted prize
        float totalWeight = prizeWeights.Sum();
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        int selectedIndex = 0;
        
        // Find selected prize based on weights
        for (int i = 0; i < prizeWeights.Count; i++)
        {
            currentWeight += prizeWeights[i];
            if (randomValue <= currentWeight)
            {
                selectedIndex = i;
                break;
            }
        }
        
        float targetAngle = segmentStartAngles[selectedIndex];
        
        float segmentSize = 51.43f;
        float randomOffset = UnityEngine.Random.Range(segmentSize * 0.2f, segmentSize * 0.8f);
        targetAngle = (targetAngle + randomOffset) % 360f;
        
        Debug.Log($"Selected Prize Index: {selectedIndex}");
        Debug.Log($"Expected Prize: {prizes[selectedIndex].name}");
        Debug.Log($"Target Angle: {targetAngle} (base: {segmentStartAngles[selectedIndex]} + offset: {randomOffset})");
        
        float targetRotation = targetAngle + (360f * 5);
        float startRotation = wheel.eulerAngles.z;
        
        Debug.Log($"Start Rotation: {startRotation}, Final Target: {targetRotation}");
        
        while (elapsedTime < spinDuration)
        {
            float t = elapsedTime / spinDuration;
            float easedT = 1 - Mathf.Pow(1 - t, 3);
            float currentRotation = Mathf.Lerp(startRotation, targetRotation, easedT);
            wheel.rotation = Quaternion.Euler(0, 0, currentRotation);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final position is exact
        wheel.rotation = Quaternion.Euler(0, 0, targetRotation);
        
        // Wait a moment for the wheel to settle
        yield return new WaitForSeconds(0.5f);
        
        Prize actualPrize = trianglePicker.GetCurrentPrize();
        if (actualPrize != null)
        {
            totalSpins++;
            if (!prizeStats.ContainsKey(actualPrize.name))
            {
                prizeStats[actualPrize.name] = 0;
            }
            prizeStats[actualPrize.name]++;
            
            Debug.Log($"Spin Complete! You won: {actualPrize.name}");
            Debug.Log($"Final Rotation: {wheel.eulerAngles.z}");
            
            // Show prize won (you'll need to implement this based on your UI)
            ShowPrizeWon(actualPrize);
            
            // Wait for prize display duration
            yield return new WaitForSeconds(2f);
            
            // Give the actual prize to the player
            GivePrizeToPlayer(actualPrize);
            
            // Hide the wheel
            SaveSpinTime();
        }
        
        isSpinning = false;
    }

    // Add this method to reset statistics
    public void ResetStats()
    {
        prizeStats.Clear();
        totalSpins = 0;
        Debug.Log("Statistics reset");
    }
}

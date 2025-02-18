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
    [SerializeField] private float initialSpinSpeed = 2000f;
    [SerializeField] private float spinDuration = 10f;
    [SerializeField] private TrianglePickerController trianglePicker; // Add reference to picker

    public Canvas canvas;

    public Transform wheel;
    public List<Prize> prizes;
    public Button spinButton;
    public GameObject prizeWonPanel;
    
    private bool isSpinning = false;

    // Update weights to match the wheel's visual order (clockwise from 1000)
    private List<float> prizeWeights = new List<float> { 
        14f,   // 1000
        16f,  // 500
        48f,  // 250
        2f,   // Random Era
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
        
    private CanvasBlur canvasBlur;
    [SerializeField] private float blurIntensity = 2f;

    private void Awake()
    {
        if (canvas == null)
        {
            Debug.LogError("Canvas reference is missing in WheelOfFortuneController!");
            return;
        }
        
        // Add CanvasBlur component to the canvas and initialize it
        canvasBlur = canvas.gameObject.AddComponent<CanvasBlur>();
        canvasBlur.Initialize(canvas);
    }

    private void Start()
    {
        ResetCooldown();
        prizetext.gameObject.SetActive(false);
        prizeWonPanel.gameObject.SetActive(false);
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
        Debug.Log($"Last spin time: {lastSpinTimeStr}"); // Debug log
        
        if (string.IsNullOrEmpty(lastSpinTimeStr))
        {
            canSpin = true;
            remainingCooldown = 0f;
            Debug.Log("No last spin time found, can spin: true"); // Debug log
        }
        else
        {
            DateTime lastSpinTime = DateTime.Parse(lastSpinTimeStr);
            DateTime nextAvailableTime = lastSpinTime.AddHours(COOLDOWN_HOURS);
            TimeSpan timeUntilNextSpin = nextAvailableTime - DateTime.Now;
            
            canSpin = timeUntilNextSpin.TotalSeconds <= 0;
            remainingCooldown = (float)Math.Max(0, timeUntilNextSpin.TotalSeconds);
            Debug.Log($"Time until next spin: {timeUntilNextSpin.TotalHours:F1} hours, can spin: {canSpin}"); // Debug log
        }

        Debug.Log($"Setting wheel active: {canSpin}"); // Debug log
        wheel.gameObject.SetActive(canSpin);
        spinButton.gameObject.SetActive(canSpin);
        trianglePicker.gameObject.SetActive(canSpin);
        
        // Update blur effect
        canvasBlur.SetBlurActive(canSpin);
        if (canSpin)
        {
            canvasBlur.SetBlurIntensity(blurIntensity);
        }
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
            ClosePlayButton();
            StartCoroutine(SpinWheel());
        }
    }
    
    // Optional: Method to reset the cooldown (for testing)
    public void ResetCooldown()
    {
        Debug.Log("Resetting cooldown"); // Debug log
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
        prizeWonPanel.gameObject.SetActive(true);
        prizetext.text = $"{prize.name}!";
    }

    private void GivePrizeToPlayer(Prize prize)
    {
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
                UnlockRandomEra();
                break;
            case "Try Again Later":
                break;
        }
    }

    private void UnlockRandomEra()
    {
        // Get all locked eras that can be unlocked
        List<string> lockedEras = new List<string>();
        
        foreach (var era in GameManager.Instance.eraPrices)
        {
            // Skip Ancient Egypt as it's always unlocked
            if (era.Key == "Ancient Egypt" || era.Key == "Medieval Europe") continue;
            
            // Check if era is locked
            if (!GameManager.Instance.IsEraUnlocked(era.Key))
            {
                lockedEras.Add(era.Key);
            }
        }

        // If there are locked eras available
        if (lockedEras.Count > 0)
        {
            // Pick a random era from the locked ones
            int randomIndex = UnityEngine.Random.Range(0, lockedEras.Count);
            string eraToUnlock = lockedEras[randomIndex];
            
            // Unlock the era
            GameManager.Instance.UnlockEra(eraToUnlock);
            
            Debug.Log($"Randomly unlocked era: {eraToUnlock}");
        }
        else
        {
            // If all eras are unlocked, give points instead
            GameManager.Instance.AddPoints(1000);
            Debug.Log("All eras already unlocked. Awarded 1000 points instead.");
        }
    }

    private IEnumerator SpinWheel()
    {
        isSpinning = true;
        float elapsedTime = 0f;
        
        // Initial backward rotation to simulate holding the wheel
        float backwardRotation = 30f; // Degrees to rotate backward
        float backwardDuration = 0.5f; // Duration of the backward rotation
        float startRotation = wheel.eulerAngles.z;
        float backwardTargetRotation = startRotation - backwardRotation;

        while (elapsedTime < backwardDuration)
        {
            float t = elapsedTime / backwardDuration;
            float currentRotation = Mathf.Lerp(startRotation, backwardTargetRotation, t);
            wheel.rotation = Quaternion.Euler(0, 0, currentRotation);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset elapsed time for the main spin
        elapsedTime = 0f;

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
        targetAngle = (targetAngle + randomOffset + 12.545f) % 360f; // Adjusted for initial rotation
        
        Debug.Log($"Selected Prize Index: {selectedIndex}");
        Debug.Log($"Expected Prize: {prizes[selectedIndex].name}");
        Debug.Log($"Target Angle: {targetAngle} (base: {segmentStartAngles[selectedIndex]} + offset: {randomOffset})");
        
        float targetRotation = targetAngle + (360f * 5);
        
        // Main spin with acceleration and deceleration
        float accelerationDuration = 1.0f; // Duration to reach full speed
        float decelerationDuration = 2.0f; // Duration to slow down
        float spinDuration = accelerationDuration + decelerationDuration; // Total duration of the spin
        float fullSpeedRotation = targetRotation + 360f * 3; // Additional rotations for full speed

        while (elapsedTime < spinDuration)
        {
            float t = elapsedTime / spinDuration;
            float easedT;
            
            if (t < accelerationDuration / spinDuration)
            {
                // Acceleration phase
                easedT = Mathf.SmoothStep(0, 1, t / (accelerationDuration / spinDuration));
            }
            else
            {
                // Deceleration phase
                float decelT = (t - accelerationDuration / spinDuration) / (decelerationDuration / spinDuration);
                easedT = 1 - Mathf.SmoothStep(0, 1, decelT);
            }
            
            float currentRotation = Mathf.Lerp(backwardTargetRotation, fullSpeedRotation, easedT);
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

    public void ClosePrizeWonPanel()
    {
        prizeWonPanel.gameObject.SetActive(false);
        wheel.gameObject.SetActive(false);
        spinButton.gameObject.SetActive(false);
        trianglePicker.gameObject.SetActive(false);
        
        // Disable blur
        canvasBlur.SetBlurActive(false);
    }

    public void ClosePlayButton()
    {
        spinButton.gameObject.SetActive(false);
    }
}

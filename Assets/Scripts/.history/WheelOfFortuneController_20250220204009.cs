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

    public GameObject coinForAnimationPrefab;
    public GameObject pointPanel;
    public GameObject safeArea;
    public GameObject coinSpawnPoint;
    
    private bool isSpinning = false;

    // Update weights to match the wheel's visual order (clockwise from 1000)
    private List<float> prizeWeights = new List<float> { 
        10f,  // 1000 (10%)
        30f,  // 500 (30%)
        50f,  // 250 (50%)
        1f,   // Random Era (1%)
        0f,   // 500 (already covered by the first 500)
        9f,   // Try Again (9%)
        0f    // 250 (already covered by the first 250)
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

    private const string LAST_SPIN_TIME_KEY = "LastSpinTime";
    private const int COOLDOWN_HOURS = 24;
    
    private bool canSpin = true;
    private float remainingCooldown = 0f;

    public TextMeshProUGUI prizetext;
        
    public CanvasBlur canvasBlur;
    [SerializeField] private float blurIntensity = 2f;

    [Header("Coin Animation Settings")]
    [SerializeField] private float coinSmoothTime = 5f; // Time to smooth movement
    [SerializeField] private float coinMaxSpeed = 100f;  // Maximum movement speed
    [SerializeField] private float coinDelay = 0.01f;

    [Header("Points Animation Settings")]
    private const float POINT_ANIMATION_DURATION = 2f; // Match WordGameManager
    private const float BUMP_SCALE = 1.50f; // Match WordGameManager
    private const float BUMP_DURATION = 0.35f; // Match WordGameManager
    [SerializeField] private TextMeshProUGUI pointsText;
    private Coroutine currentBumpCoroutine;

    private void Awake()
    {
        if (canvas == null)
        {
            Debug.LogError("Canvas reference is missing in WheelOfFortuneController!");
            return;
        }
        
    }

    private void Start()
    {
        // Ensure the wheel is hidden at start
        gameObject.SetActive(false);
        prizetext.gameObject.SetActive(false);
        prizeWonPanel.gameObject.SetActive(false);
        UpdateSpinAvailability();

        canvasBlur = canvas.gameObject.AddComponent<CanvasBlur>();
        canvasBlur.Initialize(canvas);
        
        // Disable blur at start
        if (canvasBlur != null)
        {
            canvasBlur.SetBlurActive(false);
        }
        
        // Load last spin time from SaveData
        if (SaveManager.Instance != null)
        {
            long lastSpinTime = SaveManager.Instance.Data.lastDailySpinTimestamp;
            DateTime nextAvailableTime = DateTimeOffset.FromUnixTimeSeconds(lastSpinTime).DateTime.AddHours(COOLDOWN_HOURS);
            TimeSpan timeUntilNextSpin = nextAvailableTime - DateTime.Now;
            
            canSpin = timeUntilNextSpin.TotalSeconds <= 0;
            remainingCooldown = (float)Math.Max(0, timeUntilNextSpin.TotalSeconds);
        }
        
        UpdateSpinAvailability();
    }

    public void InitBlur(){
        canvasBlur = canvas.gameObject.AddComponent<CanvasBlur>();
        canvasBlur.Initialize(canvas);
    }

    void Update()
    {
        if (!canSpin)
        {
            UpdateSpinAvailability();

            int hours = Mathf.FloorToInt(remainingCooldown / 3600);
            int minutes = Mathf.FloorToInt((remainingCooldown % 3600) / 60);
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
        
        // Update blur effect
        canvasBlur.SetBlurActive(canSpin);
        if (canSpin)
        {
            canvasBlur.SetBlurIntensity(blurIntensity);
        }
    }
    
    private void SaveSpinTime()
    {
        // Save to SaveData instead of PlayerPrefs
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.lastDailySpinTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SaveManager.Instance.SaveGame();
        }
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
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetDailySpinCooldown();
            UpdateSpinAvailability();
        }
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
        if (prize.name == "Random Era Unlocked" || prize.name == "Try Again")
        {
            prizetext.gameObject.SetActive(true);
            prizeWonPanel.gameObject.SetActive(true);
            prizetext.text = $"{prize.prizeName}!";
        }
    }

    private void GivePrizeToPlayer(Prize prize)
    {
        switch (prize.name)
        {
            case "1000 Points":
                StartCoroutine(SpawnCoins(15, 1000));
                break;
            case "500 Points":
                StartCoroutine(SpawnCoins(10, 500));
                break;
            case "250 Points":
                StartCoroutine(SpawnCoins(5, 250));
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
            SoundManager.Instance.PlaySound("PointGain");
            Debug.Log("All eras already unlocked. Awarded 1000 points instead.");
        }
    }

    private IEnumerator AnimatePointsIncrease(int pointsToAdd)
    {
        float elapsedTime = 0f;
        Vector3 originalScale = pointsText.transform.localScale;
        Color originalColor = pointsText.color;
        int startPoints = GameManager.Instance.CurrentPoints;
        int targetPoints = startPoints + pointsToAdd;
        int lastPoints = startPoints;

        while (elapsedTime < POINT_ANIMATION_DURATION)
        {
            elapsedTime += Time.deltaTime;
            
            // Polynomial easing - starts slow, accelerates faster
            float t = elapsedTime / POINT_ANIMATION_DURATION;
            t = t * t * (3 - 2 * t); // Smoother cubic easing
            
            // Calculate current points with accelerating step size
            int currentPoints = Mathf.RoundToInt(Mathf.Lerp(startPoints, targetPoints, t));
            
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
                pointsText.color = Color.green;
            }
            
            yield return null;
        }
        
        // Ensure we end up at the exact final value and return to original color
        if (pointsText != null)
        {
            pointsText.text = targetPoints.ToString();
            pointsText.transform.localScale = originalScale;
            pointsText.color = originalColor;
        }

        // Actually add the points to the GameManager after animation
        GameManager.Instance.AddPoints(pointsToAdd);
        SoundManager.Instance.PlaySound("PointGain");
    }

    private IEnumerator BumpScale(Transform target, Vector3 originalScale)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < BUMP_DURATION)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / BUMP_DURATION;
            
            // Smoother bump curve
            float scale = 1f + (BUMP_SCALE - 1f) * (1f - (2f * t - 1f) * (2f * t - 1f));
            target.localScale = originalScale * scale;
            
            yield return null;
        }
        
        // Ensure we return to original scale
        target.localScale = originalScale;
    }

    private IEnumerator SpawnCoins(int coinCount, int pointsToAdd)
    {
        bool pointsAnimationStarted = false;
        
        // Instantiate coins and start moving them immediately
        for (int i = 0; i < coinCount; i++)
        {
            GameObject coin = Instantiate(coinForAnimationPrefab, coinSpawnPoint.transform.position, Quaternion.identity, safeArea.transform);
            coin.tag = "Coin";
            // Increase coin size by 50%
            coin.transform.localScale *= 2.7f;
            StartCoroutine(MoveCoinToPanel(coin, () => {
                if (!pointsAnimationStarted)
                {
                    pointsAnimationStarted = true;
                    StartCoroutine(AnimatePointsIncrease(pointsToAdd));
                }
            }));
            yield return new WaitForSeconds(coinDelay);
        }
    }

    private IEnumerator MoveCoinToPanel(GameObject coin, System.Action onDestroy)
    {
        Vector3 startPoint = coin.transform.position;
        Vector3 endPoint = pointPanel.transform.position;

        // Add randomness to control points for independent paths
        Vector3 controlPoint1 = startPoint + new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(1f, 3f), 0);
        Vector3 controlPoint2 = endPoint + new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(1f, 3f), 0);

        // Increase the base duration significantly
        float baseDuration = 5f; // Increased to 5 seconds
        float randomDuration = UnityEngine.Random.Range(baseDuration * 0.9f, baseDuration * 1.1f);
        float elapsedTime = 0f;

        while (elapsedTime < randomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / randomDuration;

            // Calculate the position on the Bezier curve with slower movement
            Vector3 position = CalculateBezierPoint(t, startPoint, controlPoint1, controlPoint2, endPoint);
            coin.transform.position = position;

            // Check if coin has reached the point panel's border
            if (Vector3.Distance(coin.transform.position, endPoint) < 0.1f)
            {
                break;
            }

            // Add a small delay to further slow down the movement
            yield return new WaitForSeconds(0.02f); // Added delay
        }

        // Call the onDestroy callback before destroying the coin
        onDestroy?.Invoke();

        // Destroy the coin immediately when it reaches the border
        Destroy(coin);
    }

    // Calculate a point on a cubic Bezier curve
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0; // First term
        p += 3 * uu * t * p1; // Second term
        p += 3 * u * tt * p2; // Third term
        p += ttt * p3;        // Fourth term

        return p;
    }

    private IEnumerator SpinWheel()
    {
        isSpinning = true;
        
        // Add pre-spin animation (visual effect only, doesn't affect final rotation)
        float preSpinDuration = 1.1f;
        float preSpinElapsed = 0f;
        float startRotation = wheel.eulerAngles.z;
        float preSpinTarget = startRotation - 50f; // Reduced pull-back amount
        
        // Pre-spin animation (slow pull back)
        while (preSpinElapsed < preSpinDuration)
        {
            float t = preSpinElapsed / preSpinDuration;
            float currentRotation = Mathf.Lerp(startRotation, preSpinTarget, t);
            wheel.rotation = Quaternion.Euler(0, 0, currentRotation);
            preSpinElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Continue with normal spin
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
        
        // Calculate target angle with offset and adjustment
        float targetAngle = segmentStartAngles[selectedIndex];
        float segmentSize = 51.43f;
        float randomOffset = UnityEngine.Random.Range(segmentSize * 0.2f, segmentSize * 0.8f);
        targetAngle = (targetAngle + randomOffset + 12.545f) % 360f; // Keep the adjustment
        
        // Calculate target rotation (don't account for pre-spin)
        float targetRotation = targetAngle + (360f * 5);
        
        Debug.Log($"Selected Prize Index: {selectedIndex}");
        Debug.Log($"Expected Prize: {prizes[selectedIndex].name}");
        Debug.Log($"Target Angle: {targetAngle} (base: {segmentStartAngles[selectedIndex]} + offset: {randomOffset} + adjustment: 12.545)");
        Debug.Log($"Start Rotation: {startRotation}, Final Target: {targetRotation}");
        
        // Main spin animation
        while (elapsedTime < spinDuration)
        {
            float t = elapsedTime / spinDuration;
            float easedT = 1 - Mathf.Pow(1 - t, 3);
            float currentRotation = Mathf.Lerp(preSpinTarget, targetRotation, easedT);
            wheel.rotation = Quaternion.Euler(0, 0, currentRotation);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final position is exact
        wheel.rotation = Quaternion.Euler(0, 0, targetRotation);
        
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
            
            // Show prize won
            ShowPrizeWon(actualPrize);
            
            // Turn off the background blur before spawning coins
            if (canvasBlur != null)
            {
                canvasBlur.SetBlurActive(false);
            }

            // Give the actual prize to the player immediately
            GivePrizeToPlayer(actualPrize);
            
            // Wait until all coins have reached their destination
            yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Coin").Length == 0);
            
            // Hide the wheel and related UI elements
            wheel.gameObject.SetActive(false);
            spinButton.gameObject.SetActive(false);
            trianglePicker.gameObject.SetActive(false);
            gameObject.SetActive(false);
            
            // Ensure blur is disabled
            if (canvasBlur != null)
            {
                canvasBlur.SetBlurActive(false);
            }
            
            // Save the spin time
            SaveSpinTime();
        }
        
        isSpinning = false;
        yield break;
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

    public void ShowWheel()
    {
        gameObject.SetActive(true);
        UpdateSpinAvailability();
        
        // Enable blur when wheel is shown
        if (canvasBlur != null)
        {
            canvasBlur.SetBlurActive(true);
            canvasBlur.SetBlurIntensity(blurIntensity);
        }
    }

    public void HideWheel()
    {
        gameObject.SetActive(false);
        
        // Disable blur when wheel is hidden
        canvasBlur.SetBlurActive(true);
        if (canvasBlur != null)
        {
            canvasBlur.SetBlurActive(false);
        }
    }
}

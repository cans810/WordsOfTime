using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class GameSceneCanvasController : MonoBehaviour
{
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintButtonText;
    [SerializeField] private TextMeshProUGUI pointsText;
    private int hintLevel = 1;
    private int lastCheckedPoints = 0;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged += OnPointsChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged -= OnPointsChanged;
        }
    }

    private void OnPointsChanged(int points)
    {
        if (pointsText != null)
        {
            pointsText.text = $"Points: {points}";
        }
        UpdateHintButtonText();
    }

    private void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged += OnPointsChanged;
        }

        // Set initial state of buttons before they're visible
        if (watchAdButton != null)
        {
            watchAdButton.gameObject.SetActive(false); // Start hidden
            if (SaveManager.Instance != null)
            {
                long lastAdTime = SaveManager.Instance.Data.lastRewardedAdTimestamp;
                bool canWatchAd = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastAdTime >= REWARDED_AD_COOLDOWN;
                watchAdButton.gameObject.SetActive(canWatchAd);
            }
        }

        if (spinWheelButton != null)
        {
            spinWheelButton.gameObject.SetActive(false); // Start hidden
            if (SaveManager.Instance != null)
            {
                long lastSpinTime = SaveManager.Instance.Data.lastDailySpinTimestamp;
                bool canSpin = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastSpinTime >= DAILY_SPIN_COOLDOWN;
                spinWheelButton.gameObject.SetActive(canSpin);
            }
        }
    }

    private void Start()
    {
        UpdateHintButtonText();
        StartCoroutine(CheckPointsRoutine());
    }

    private IEnumerator CheckPointsRoutine()
    {
        while (true)
        {
            if (GameManager.Instance != null)
            {
                int currentPoints = GameManager.Instance.CurrentPoints;
                if (currentPoints != lastCheckedPoints)
                {
                    lastCheckedPoints = currentPoints;
                    UpdateHintButtonText();
                }
            }
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged -= OnPointsChanged;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HomeButtonClicked()
    {
        // Clear current game state before going home
        WordGameManager.Instance.ClearCurrentWord();
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnNextButtonClicked()
    {
        WordGameManager.Instance.NextWord();
    }

    public void OnPreviousButtonClicked()
    {
        WordGameManager.Instance.PreviousWord();
    }

    public void OnHintButtonClicked()
    {
        // Check if current word is already solved
        if (WordGameManager.Instance.IsWordSolved(WordGameManager.Instance.targetWord))
        {
            return; // Don't give hint for solved words
        }

        // Check if player can afford the hint
        int hintCost = hintLevel == 2 ? GameManager.SECOND_HINT_COST : GameManager.HINT_COST;
        if (GameManager.Instance.CurrentPoints >= hintCost)
        {
            WordGameManager.Instance.GiveHint();
        }
    }

    public void UpdateHintButtonText()
    {
        if (hintButton != null && hintButtonText != null)
        {
            int hintCost = GameManager.HINT_COST;
            if (hintLevel == 2)
            {
                hintCost = GameManager.SECOND_HINT_COST;
            }

            hintButtonText.text = $"Hint ({hintCost} pts)";
            
            // Update color based on whether player can afford the hint
            bool canAfford = GameManager.Instance.CurrentPoints >= hintCost;
            hintButtonText.color = canAfford ? Color.white : Color.red;
        }
    }

    public int GetHintCost()
    {
        return hintLevel == 2 ? GameManager.SECOND_HINT_COST : GameManager.HINT_COST;
    }

    public int GetHintLevel()
    {
        return hintLevel;
    }

    public void IncrementHintLevel()
    {
        hintLevel = Mathf.Min(hintLevel + 1, 2);
        UpdateHintButtonText();
    }

    public void UpdateHintButtonInteractable(bool interactable)
    {
        hintButton.interactable = interactable;
    }
}

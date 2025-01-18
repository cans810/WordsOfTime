using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameSceneCanvasController : MonoBehaviour
{
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintButtonText;
    private int hintLevel = 1;
    private int lastCheckedPoints = 0;

    private void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged += OnPointsChanged;
        }
    }

    private void Start()
    {
        Debug.Log("[GameSceneCanvasController] Starting point check routine");
        UpdateHintButtonText();
        StartCoroutine(CheckPointsRoutine());
    }

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
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        Debug.Log("[GameSceneCanvasController] OnDestroy called");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged -= OnPointsChanged;
        }
        StopAllCoroutines();
    }

    private void OnPointsChanged()
    {
        Debug.Log("Points changed, updating hint button"); // Debug log
        UpdateHintButtonText();
    }

    private IEnumerator CheckPointsRoutine()
    {
        while (true)
        {
            if (GameManager.Instance != null)
            {
                int currentPoints = GameManager.Instance.CurrentPoints;
                Debug.Log($"[GameSceneCanvasController] Checking points - Current: {currentPoints}, Last: {lastCheckedPoints}");
                
                if (currentPoints != lastCheckedPoints)
                {
                    Debug.Log($"[GameSceneCanvasController] Points changed from {lastCheckedPoints} to {currentPoints}");
                    lastCheckedPoints = currentPoints;
                    UpdateHintButtonText();
                }
            }
            yield return new WaitForSeconds(0.1f);
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

            int currentPoints = GameManager.Instance.CurrentPoints;
            bool canAfford = currentPoints >= hintCost;
            
            hintButtonText.text = $"Hint ({hintCost} pts)";
            hintButtonText.color = canAfford ? Color.white : Color.red;
            
            Debug.Log($"[GameSceneCanvasController] Updating hint button - Points: {currentPoints}, Cost: {hintCost}, Can afford: {canAfford}, Color: {(canAfford ? "White" : "Red")}");
        }
    }
}

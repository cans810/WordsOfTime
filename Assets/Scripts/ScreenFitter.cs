using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        FitToScreen();
    }

    private void OnRectTransformDimensionsChange()
    {
        FitToScreen();
    }

    private void FitToScreen()
    {
        if (rectTransform == null || canvas == null) return;

        // Get screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Get current rect dimensions
        Vector2 size = rectTransform.rect.size;
        float rectWidth = size.x;
        float rectHeight = size.y;

        // Calculate scale needed to fit screen
        float scaleX = screenWidth / rectWidth;
        float scaleY = screenHeight / rectHeight;

        // Use the smaller scale to ensure it fits within screen
        float scale = Mathf.Min(scaleX, scaleY, 1f); // Never scale up, only down if needed

        // Apply the scale
        rectTransform.localScale = new Vector3(scale, scale, 1f);

        // Center the element
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
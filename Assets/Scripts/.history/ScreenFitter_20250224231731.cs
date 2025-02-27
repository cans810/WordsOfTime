using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas canvas;

    [SerializeField] private float scaleMultiplier = 1.5f;

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

    public void FitToScreen()
    {
        if (rectTransform == null || canvas == null) return;

        // Get screen dimensions in canvas space
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            screenSize = screenSize / canvas.scaleFactor;
        }

        // Calculate the current rect size
        Vector2 rectSize = rectTransform.rect.size;
        
        // First calculate base scale to fit screen
        float scaleX = screenSize.x / rectSize.x;
        float scaleY = screenSize.y / rectSize.y;
        float baseScale = Mathf.Min(scaleX, scaleY);

        // Apply multiplier to get desired scale
        float desiredScale = baseScale * scaleMultiplier;

        // Only scale down if the final size would exceed screen bounds
        if (desiredScale * rectSize.x > screenSize.x || desiredScale * rectSize.y > screenSize.y)
        {
            rectTransform.localScale = Vector3.one * baseScale;
        }
        else
        {
            rectTransform.localScale = Vector3.one * desiredScale;
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas canvas;

    [SerializeField] private float scaleMultiplier = 1.5f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 2.0f;
    [SerializeField] private bool fitOnStart = true;
    [SerializeField] private bool fitOnScreenChange = true;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        // Log initialization for debugging
        Debug.Log($"[ScreenFitter] Initialized on {gameObject.name}. Canvas: {(canvas != null ? canvas.name : "null")}");
    }

    private void Start()
    {
        if (fitOnStart)
        {
            FitToScreen();
        }
    }

    private void OnEnable()
    {
        // Always fit when enabled
        FitToScreen();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (fitOnScreenChange)
        {
            FitToScreen();
        }
    }

    public void FitToScreen()
    {
        if (rectTransform == null || canvas == null) 
        {
            Debug.LogWarning($"[ScreenFitter] Cannot fit {gameObject.name} to screen: rectTransform or canvas is null");
            
            // Try to get references again
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
                
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
                
            if (rectTransform == null || canvas == null)
                return;
        }

        // Get screen dimensions in canvas space
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            screenSize = screenSize / canvas.scaleFactor;
        }

        // Calculate the current rect size
        Vector2 rectSize = rectTransform.rect.size;
        
        // Log sizes for debugging
        Debug.Log($"[ScreenFitter] Screen size: {screenSize}, Panel size: {rectSize}, Canvas scale factor: {canvas.scaleFactor}");
        
        // First calculate base scale to fit screen
        float scaleX = screenSize.x / rectSize.x;
        float scaleY = screenSize.y / rectSize.y;
        float baseScale = Mathf.Min(scaleX, scaleY);

        // Apply multiplier to get desired scale
        float desiredScale = baseScale * scaleMultiplier;
        
        // Clamp scale to min/max values
        desiredScale = Mathf.Clamp(desiredScale, minScale, maxScale);

        // Only scale down if the final size would exceed screen bounds
        if (desiredScale * rectSize.x > screenSize.x || desiredScale * rectSize.y > screenSize.y)
        {
            rectTransform.localScale = Vector3.one * baseScale;
            Debug.Log($"[ScreenFitter] Applied base scale: {baseScale} to fit screen");
        }
        else
        {
            rectTransform.localScale = Vector3.one * desiredScale;
            Debug.Log($"[ScreenFitter] Applied desired scale: {desiredScale}");
        }
        
        // Ensure the panel is centered
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Special handling for Android devices
        if (Application.platform == RuntimePlatform.Android)
        {
            // Get safe area
            Rect safeArea = Screen.safeArea;
            
            // Calculate safe area in canvas space
            Rect scaledSafeArea = new Rect(
                safeArea.x / canvas.scaleFactor,
                safeArea.y / canvas.scaleFactor,
                safeArea.width / canvas.scaleFactor,
                safeArea.height / canvas.scaleFactor
            );
            
            // Ensure we're not outside the safe area
            Vector2 finalSize = rectSize * desiredScale;
            if (finalSize.x > scaledSafeArea.width * 0.9f || finalSize.y > scaledSafeArea.height * 0.9f)
            {
                float safeScaleX = scaledSafeArea.width * 0.9f / rectSize.x;
                float safeScaleY = scaledSafeArea.height * 0.9f / rectSize.y;
                float safeScale = Mathf.Min(safeScaleX, safeScaleY);
                
                rectTransform.localScale = Vector3.one * safeScale;
                Debug.Log($"[ScreenFitter] Applied safe area scale: {safeScale} for Android");
            }
        }
    }
}
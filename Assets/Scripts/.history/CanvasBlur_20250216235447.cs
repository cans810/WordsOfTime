using UnityEngine;
using UnityEngine.UI;

public class CanvasBlur : MonoBehaviour
{
    [SerializeField] private Material blurMaterial;
    [SerializeField] private float blurSize = 2f;
    private Image blurImage;
    private Canvas targetCanvas;

    public void Initialize(Canvas canvas)
    {
        targetCanvas = canvas;
        
        // Create GameObject for blur
        GameObject blurObject = new GameObject("CanvasBlur");
        blurObject.transform.SetParent(targetCanvas.transform, false);
        
        // Add and setup blur image
        blurImage = blurObject.AddComponent<Image>();
        blurImage.material = new Material(blurMaterial);
        blurImage.material.SetFloat("_BlurSize", blurSize);
        
        // Make it cover the entire canvas
        RectTransform rect = blurImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        // Set it as the first sibling so it's behind other UI elements
        blurObject.transform.SetAsFirstSibling();
        
        // Initially disabled
        SetBlurActive(false);
    }

    public void SetBlurActive(bool active)
    {
        if (blurImage != null)
        {
            blurImage.enabled = active;
        }
    }

    public void SetBlurIntensity(float intensity)
    {
        blurSize = intensity;
        if (blurImage != null && blurImage.material != null)
        {
            blurImage.material.SetFloat("_BlurSize", blurSize);
        }
    }
} 
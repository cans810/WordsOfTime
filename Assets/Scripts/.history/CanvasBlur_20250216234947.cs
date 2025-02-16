using UnityEngine;
using UnityEngine.UI;

public class CanvasBlur : MonoBehaviour
{
    [SerializeField] private Material blurMaterial;
    [SerializeField] private float blurSize = 2f;
    private Image blurImage;

    private void Awake()
    {
        // Create a full-screen image for the blur effect
        blurImage = gameObject.AddComponent<Image>();
        blurImage.material = new Material(blurMaterial);
        blurImage.material.SetFloat("_BlurSize", blurSize);
        
        // Make the image cover the entire canvas
        RectTransform rect = blurImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    public void SetBlurActive(bool active)
    {
        blurImage.enabled = active;
    }

    public void SetBlurIntensity(float intensity)
    {
        blurSize = intensity;
        blurImage.material.SetFloat("_BlurSize", blurSize);
    }
} 
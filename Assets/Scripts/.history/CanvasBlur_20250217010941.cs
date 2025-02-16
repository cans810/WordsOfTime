using UnityEngine;
using UnityEngine.UI;

public class CanvasBlur : MonoBehaviour
{
    private Material blurMaterial;
    [SerializeField] private float blurSize = 2f;
    private Image blurImage;
    private Canvas targetCanvas;

    public void Initialize(Canvas canvas)
    {
        targetCanvas = canvas;
        
        // Create the blur material
        Shader blurShader = Shader.Find("Custom/UIBlur");
        if (blurShader == null)
        {
            Debug.LogError("Could not find Custom/UIBlur shader!");
            return;
        }
        
        blurMaterial = new Material(blurShader);
        
        // Create GameObject for blur
        GameObject blurObject = new GameObject("MainCanvasBlur");
        blurObject.transform.SetParent(targetCanvas.transform, false);
        
        // Add and setup blur image
        blurImage = blurObject.AddComponent<Image>();
        blurImage.material = blurMaterial;
        blurImage.material.SetFloat("_BlurSize", blurSize);
        
        // Make it cover the entire canvas
        RectTransform rect = blurImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        // Set it before the wheel canvas and set sorting order
        Canvas wheelCanvas = FindObjectOfType<WheelOfFortuneController>()?.canvas;
        if (wheelCanvas != null)
        {
            // Set the blur object before the wheel canvas
            blurObject.transform.SetSiblingIndex(wheelCanvas.transform.GetSiblingIndex());
            
            // Set up sorting layers
            wheelCanvas.sortingLayerName = "WheelUI";
            wheelCanvas.sortingOrder = 1;
            
            targetCanvas.sortingLayerName = "BackgroundUI";
            targetCanvas.sortingOrder = 0;
            
            // Add a CanvasGroup to the wheel canvas if it doesn't have one
            if (!wheelCanvas.gameObject.GetComponent<CanvasGroup>())
            {
                CanvasGroup wheelGroup = wheelCanvas.gameObject.AddComponent<CanvasGroup>();
                wheelGroup.interactable = true;
                wheelGroup.blocksRaycasts = true;
                wheelGroup.ignoreParentGroups = true;
            }
        }
        else{
        
        // Initially disabled
        SetBlurActive(false);
    }

    public void SetBlurActive(bool active)
    {
        if (blurImage != null)
        {
            blurImage.enabled = active;
            Debug.Log($"Blur active: {active}"); // Debug log
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
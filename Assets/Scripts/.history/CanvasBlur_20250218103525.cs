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
        if (canvas == null)
        {
            return;
        }
        
        targetCanvas = canvas;
        Debug.Log($"Target canvas found: {targetCanvas != null}");
        
        // Create the blur material
        Shader blurShader = Shader.Find("Custom/UIBlur");
        if (blurShader == null)
        {
            return;
        }
        
        blurMaterial = new Material(blurShader);
        
        // Create GameObject for blur
        GameObject blurObject = new GameObject("MainCanvasBlur");
        if (blurObject == null)
        {
            return;
        }
        
        blurObject.transform.SetParent(targetCanvas.transform, false);
        
        // Add and setup blur image
        blurImage = blurObject.AddComponent<Image>();
        if (blurImage == null)
        {
            return;
        }
        
        blurImage.material = blurMaterial;
        blurImage.material.SetFloat("_BlurSize", blurSize);
        
        // Make it cover the entire canvas
        RectTransform rect = blurImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        // Find wheel controller in the scene
        WheelOfFortuneController wheelController = GameObject.FindObjectOfType<WheelOfFortuneController>();
        if (wheelController == null)
        {
            return;
        }
        
        if (wheelController.canvas == null)
        {
            return;
        }

        // Move the wheel canvas under the blur object
        wheelController.canvas.transform.SetParent(blurObject.transform, true);
        
        // Set up sorting layers
        wheelController.canvas.sortingLayerName = "WheelUI";
        wheelController.canvas.sortingOrder = 2;
        
        targetCanvas.sortingLayerName = "BackgroundUI";
        targetCanvas.sortingOrder = 0;
        
        // Add a CanvasGroup to the wheel canvas if it doesn't have one
        CanvasGroup wheelGroup = wheelController.canvas.gameObject.GetComponent<CanvasGroup>();
        if (wheelGroup == null)
        {
            wheelGroup = wheelController.canvas.gameObject.AddComponent<CanvasGroup>();
            wheelGroup.interactable = true;
            wheelGroup.blocksRaycasts = true;
            wheelGroup.ignoreParentGroups = true;
        }
        
        // Set the blur object's sorting order between background and wheel
        Canvas blurCanvas = blurImage.canvas;
        if (blurCanvas != null)
        {
            blurCanvas.sortingOrder = 1;
        }
        
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
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    private Camera cam;
    private float defaultWidth = 1080f;  // Your reference resolution width
    private float defaultHeight = 1920f; // Your reference resolution height
    
    void Awake()
    {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
    }

    void UpdateCameraSize()
    {
        float targetAspect = defaultWidth / defaultHeight;
        float screenAspect = (float)Screen.width / Screen.height;
        float orthographicSize = defaultHeight / 200f; // Base ortho size for your reference resolution
        
        if (screenAspect < targetAspect)
        {
            // Screen is taller than target, adjust ortho size
            orthographicSize *= targetAspect / screenAspect;
        }
        
        cam.orthographicSize = orthographicSize;
        Debug.Log($"Camera Ortho Size set to: {orthographicSize}");
    }
} 
using UnityEngine;

public class BackgroundFit : MonoBehaviour
{
    void Start()
    {
        FitToScreen();
    }

    void FitToScreen()
    {
        // Get Camera and SpriteRenderer
        Camera cam = Camera.main;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null || cam == null)
        {
            Debug.LogError("Missing SpriteRenderer or Camera!");
            return;
        }

        // Get world-space screen height & width
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        // Get sprite's original size
        float spriteHeight = sr.bounds.size.y;
        float spriteWidth = sr.bounds.size.x;

        // Calculate scale factors
        float scaleY = screenHeight / spriteHeight;
        float scaleX = screenWidth / spriteWidth;

        // Apply the larger scale factor to ensure full coverage
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}

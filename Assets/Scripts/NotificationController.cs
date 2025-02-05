using UnityEngine;
using System.Collections;
#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class NotificationController : MonoBehaviour
{
    [SerializeField] private AndroidNotifications androidNotifications;
    [SerializeField] private IOSNotifications iosNotifications;

    // Start is called before the first frame update
    private void Start()
    {
        #if UNITY_ANDROID
        if (androidNotifications != null)
        {
            androidNotifications.RequestAuthorization();
            androidNotifications.RegisterNotificationChannel();
        }
        #elif UNITY_IOS
        if (iosNotifications != null)
        {
            StartCoroutine(iosNotifications.RequestAuthorization());
        }
        #else
        Debug.Log("Notifications are not supported on this platform");
        #endif
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus == false)
        {
            #if UNITY_ANDROID
            if (androidNotifications != null)
            {
                AndroidNotificationCenter.CancelAllNotifications();
                androidNotifications.SendNotification("Full Lives", "Your lives are now restored!", 2);
            }
            #elif UNITY_IOS
            if (iosNotifications != null)
            {
                iOSNotificationCenter.RemoveAllScheduledNotifications();
                iosNotifications.SendNotification("Full Lives", "Your lives are now restored!", "Come back to Origami Match!", 2);
            }
            #else
            Debug.Log("Notifications are not supported on this platform");
            #endif
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

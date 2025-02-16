using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class NotificationController : MonoBehaviour
{
    private static NotificationController _instance;
    public static NotificationController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NotificationController>();
            }
            return _instance;
        }
    }

    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Text notificationText;
    private Coroutine currentNotification;

    [SerializeField] private AndroidNotifications androidNotifications;
    [SerializeField] private IOSNotifications iosNotifications;

    private string[] notificationTitles = new string[]
    {
        "Your Journey Awaits!",
        "Time Travel Ready",
        "History Calls!",
        "New Adventures",
        "Time to Explore!"
    };

    private string[] notificationMessages = new string[]
    {
        "Travel through time and test your knowledge!",
        "New historical mysteries await your return",
        "Your time machine is ready for another adventure",
        "Embark on a journey through the ages",
        "Challenge yourself with historical puzzles"
    };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

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
        if (focus == false && GameManager.Instance != null)
        {
            // Only send notifications if they are enabled in settings
            if (GameManager.Instance.IsNotificationsOn())
            {
                int randomIndex = Random.Range(0, notificationTitles.Length);
                string title = notificationTitles[randomIndex];
                string message = notificationMessages[randomIndex];

                #if UNITY_ANDROID
                if (androidNotifications != null)
                {
                    AndroidNotificationCenter.CancelAllNotifications();
                    androidNotifications.SendNotification(title, message, 2);
                }
                #elif UNITY_IOS
                if (iosNotifications != null)
                {
                    iOSNotificationCenter.RemoveAllScheduledNotifications();
                    iosNotifications.SendNotification(title, message, "Return to your historical adventure!", 2);
                }
                #else
                Debug.Log("Notifications are not supported on this platform");
                #endif
            }
            else
            {
                Debug.Log("Notifications are disabled in settings");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowNotification(string message, float duration = 2f)
    {
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
        }
        currentNotification = StartCoroutine(ShowNotificationCoroutine(message, duration));
    }

    private IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            yield return new WaitForSeconds(duration);
            notificationPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Notification panel or text component not assigned!");
        }
    }
}

using UnityEngine;
using System.Collections;
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class IOSNotifications : MonoBehaviour
{
    #if UNITY_IOS
    // Request access to send notifications
    public IEnumerator RequestAuthorization()
    {
        using var request = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge, true);
        while (!request.IsFinished)
        {
            yield return null;
        }
    }

    // Set up notification template
    public void SendNotification(string title, string body, string subtitle, int fireTimeInHours)
    {
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new System.TimeSpan(fireTimeInHours, 0, 0),
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = "lives_full",
            Title = title,
            Body = body,
            Subtitle = subtitle,
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Badge),
            CategoryIdentifier = "default_category",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
    }
    #else
    public IEnumerator RequestAuthorization()
    {
        Debug.Log("iOS Notifications not available on this platform");
        yield break;
    }

    public void SendNotification(string title, string body, string subtitle, int fireTimeInHours)
    {
        Debug.Log("iOS Notifications not available on this platform");
    }
    #endif
}

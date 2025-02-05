#if UNITY_ANDROID
using System.Collections.Generic;
using UnityEngine;
using Unity.Notifications.Android;
using UnityEngine.Android;

public class AndroidNotifications : MonoBehaviour
{
    // Request authorization to send notifications
    public void RequestAuthorization()
    {
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }
    }

    // Register a notification channel
    public void RegisterNotificationChannel()
    {
        var channel = new AndroidNotificationChannel
        {
            Id = "default_channel",
            Name = "Default Channel",
            Importance = Importance.Default,
            Description = "Full Lives"
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    // set up notification template
    public void SendNotification(string title, string text, int fireTimeInHours)
    {
        var notification = new AndroidNotification();
        notification.Title = title;
        notification.Text = text;
        notification.FireTime = System.DateTime.Now.AddHours(fireTimeInHours);
        notification.SmallIcon = "icon_0";
        notification.LargeIcon = "icon_0";

        AndroidNotificationCenter.SendNotification(notification, "default_channel");
    }
}
#endif

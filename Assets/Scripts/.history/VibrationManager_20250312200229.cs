using UnityEngine;

/// <summary>
/// Utility class to handle vibration feedback on mobile devices.
/// Works on Android and iOS platforms.
/// </summary>
public static class VibrationManager
{
    // Flag to enable/disable vibration globally
    private static bool isVibrationEnabled = true;

    /// <summary>
    /// Vibrates the device with a short pulse.
    /// </summary>
    public static void Vibrate()
    {
        if (!isVibrationEnabled)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Use Android's Vibrator service
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        
        // Check if device has vibrator
        if (vibrator.Call<bool>("hasVibrator"))
        {
            // For Android API 26+ (Oreo and newer)
            if (AndroidVersion >= 26)
            {
                vibrator.Call("vibrate", VibrationEffect("createOneShot", new object[] { 50L, -1 }));
            }
            else
            {
                // For older Android versions
                vibrator.Call("vibrate", 50L);
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // Use iOS haptic feedback
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Vibrates the device with a pattern for success feedback.
    /// </summary>
    public static void VibrateSuccess()
    {
        if (!isVibrationEnabled)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Use Android's Vibrator service
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        
        // Check if device has vibrator
        if (vibrator.Call<bool>("hasVibrator"))
        {
            // For Android API 26+ (Oreo and newer)
            if (AndroidVersion >= 26)
            {
                // Two short vibrations for success
                long[] pattern = new long[] { 0, 30, 60, 30 };
                vibrator.Call("vibrate", VibrationEffect("createWaveform", new object[] { pattern, -1 }));
            }
            else
            {
                // For older Android versions
                long[] pattern = new long[] { 0, 30, 60, 30 };
                vibrator.Call("vibrate", pattern, -1);
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // Use iOS haptic feedback
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Vibrates the device with a pattern for error feedback.
    /// </summary>
    public static void VibrateError()
    {
        if (!isVibrationEnabled)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Use Android's Vibrator service
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        
        // Check if device has vibrator
        if (vibrator.Call<bool>("hasVibrator"))
        {
            // For Android API 26+ (Oreo and newer)
            if (AndroidVersion >= 26)
            {
                // One longer vibration for error
                vibrator.Call("vibrate", VibrationEffect("createOneShot", new object[] { 100L, -1 }));
            }
            else
            {
                // For older Android versions
                vibrator.Call("vibrate", 100L);
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // Use iOS haptic feedback
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Enable or disable vibration.
    /// </summary>
    public static void SetVibrationEnabled(bool enabled)
    {
        isVibrationEnabled = enabled;
    }

    /// <summary>
    /// Check if vibration is enabled.
    /// </summary>
    public static bool IsVibrationEnabled()
    {
        return isVibrationEnabled;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Helper method to get Android version
    private static int AndroidVersion
    {
        get
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }
    }

    // Helper method to create VibrationEffect for Android API 26+
    private static AndroidJavaObject VibrationEffect(string methodName, object[] parameters)
    {
        using (var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
        {
            return vibrationEffectClass.CallStatic<AndroidJavaObject>(methodName, parameters);
        }
    }
#endif
} 
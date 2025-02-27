using System;

[Serializable]
public struct AdState
{
    public bool canWatch;
    public DateTime nextAvailableTime;
    
    // Added for JSON serialization since DateTime isn't directly serializable
    public long nextAvailableTimeTicks;
}
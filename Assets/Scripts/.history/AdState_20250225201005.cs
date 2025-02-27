using System;
using UnityEngine;

[System.Serializable]
public struct AdState
{
    public bool canWatch;
    public DateTime nextAvailableTime;
    
    // For JSON serialization (DateTime is not directly serializable)
    public long nextAvailableTimeTicks;
}
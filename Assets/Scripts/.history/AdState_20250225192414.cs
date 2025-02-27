using System;

[Serializable]
public class AdState
{
    public bool canWatch = true;
    
    // DateTime doesn't serialize well in JSON, so we'll use a string representation
    [NonSerialized]
    public DateTime nextAvailableTime = DateTime.Now;
    
    // String representation for JSON serialization
    public string nextAvailableTimeString;
    
    // Convert DateTime to string before serialization
    public void PrepareForSerialization()
    {
        nextAvailableTimeString = nextAvailableTime.ToString("o"); // ISO 8601 format
    }
    
    // Convert string back to DateTime after deserialization
    public void ProcessAfterDeserialization()
    {
        if (!string.IsNullOrEmpty(nextAvailableTimeString))
        {
            try
            {
                nextAvailableTime = DateTime.Parse(nextAvailableTimeString);
            }
            catch
            {
                nextAvailableTime = DateTime.Now;
            }
        }
        else
        {
            nextAvailableTime = DateTime.Now;
        }
    }
} 
namespace ElectricRaspberry.Models.Regulation;

/// <summary>
/// Represents the level of activity in a channel or conversation
/// </summary>
public enum ActivityLevel
{
    /// <summary>
    /// No recent activity
    /// </summary>
    Inactive = 0,
    
    /// <summary>
    /// Very low level of activity (occasional messages)
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Moderate activity level (regular conversation)
    /// </summary>
    Moderate = 2,
    
    /// <summary>
    /// High activity level (active conversation)
    /// </summary>
    High = 3,
    
    /// <summary>
    /// Very high activity level (intense conversation or many participants)
    /// </summary>
    VeryHigh = 4
}
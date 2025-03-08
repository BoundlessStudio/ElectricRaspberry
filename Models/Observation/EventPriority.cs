namespace ElectricRaspberry.Models.Observation;

/// <summary>
/// Represents the priority level for events in the observation system
/// </summary>
public enum EventPriority
{
    /// <summary>
    /// Lowest priority events (e.g., general channel messages with no relevance to the bot)
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Normal priority events (e.g., typical messages in active conversations)
    /// </summary>
    Normal = 100,
    
    /// <summary>
    /// High priority events (e.g., messages from users with strong relationships)
    /// </summary>
    High = 200,
    
    /// <summary>
    /// Critical priority events (e.g., direct mentions, DMs, or admin commands)
    /// </summary>
    Critical = 300
}
namespace ElectricRaspberry.Services.Regulation.Configuration;

/// <summary>
/// Configuration options for the self-regulation service
/// </summary>
public class SelfRegulationOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SelfRegulation = "SelfRegulation";
    
    /// <summary>
    /// Minimum time between bot messages in seconds
    /// </summary>
    public int MinResponseDelaySeconds { get; set; } = 1;
    
    /// <summary>
    /// Maximum time between bot messages in seconds
    /// </summary>
    public int MaxResponseDelaySeconds { get; set; } = 5;
    
    /// <summary>
    /// Minimum time between conversation initiations in minutes
    /// </summary>
    public int MinInitiationDelayMinutes { get; set; } = 15;
    
    /// <summary>
    /// Maximum time between conversation initiations in minutes
    /// </summary>
    public int MaxInitiationDelayMinutes { get; set; } = 60;
    
    /// <summary>
    /// Maximum messages to send in response to a message before throttling
    /// </summary>
    public int MaxConsecutiveResponses { get; set; } = 3;
    
    /// <summary>
    /// Base probability of engagement in a conversation
    /// </summary>
    public double BaseEngagementProbability { get; set; } = 0.5;
    
    /// <summary>
    /// Time between idle behaviors in minutes
    /// </summary>
    public int IdleBehaviorIntervalMinutes { get; set; } = 30;
    
    /// <summary>
    /// Time window for activity tracking in minutes
    /// </summary>
    public int ActivityTrackingWindowMinutes { get; set; } = 5;
    
    /// <summary>
    /// Message count per minute threshold for high activity
    /// </summary>
    public double HighActivityThreshold { get; set; } = 5;
    
    /// <summary>
    /// Message count per minute threshold for moderate activity
    /// </summary>
    public double ModerateActivityThreshold { get; set; } = 2;
    
    /// <summary>
    /// Message count per minute threshold for low activity
    /// </summary>
    public double LowActivityThreshold { get; set; } = 0.5;
    
    /// <summary>
    /// Maximum ratio of bot messages to total messages in a channel
    /// </summary>
    public double MaxBotMessageRatio { get; set; } = 0.3;
    
    /// <summary>
    /// Relationship stage transition thresholds
    /// </summary>
    public Dictionary<string, double> RelationshipStageThresholds { get; set; } = new Dictionary<string, double>
    {
        { "Acquaintance", 0.2 },
        { "Casual", 0.4 },
        { "Friend", 0.7 },
        { "CloseFriend", 0.9 }
    };
}
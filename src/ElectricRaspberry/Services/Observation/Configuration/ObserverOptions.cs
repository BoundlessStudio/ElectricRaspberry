namespace ElectricRaspberry.Services.Observation.Configuration;

/// <summary>
/// Configuration options for the observer service
/// </summary>
public class ObserverOptions
{
    /// <summary>
    /// Maximum number of events to process in a single batch
    /// </summary>
    public int MaxEventsPerBatch { get; set; } = 10;
    
    /// <summary>
    /// Bot's user ID
    /// </summary>
    public string BotUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Delay in milliseconds to add between processing events
    /// </summary>
    public int DelayBetweenEventProcessingMs { get; set; } = 250;
    
    /// <summary>
    /// Timeout in minutes for removing inactive buffers
    /// </summary>
    public int InactiveBufferTimeoutMinutes { get; set; } = 60;
}
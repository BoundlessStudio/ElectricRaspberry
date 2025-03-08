namespace ElectricRaspberry.Services.Observation.Configuration;

/// <summary>
/// Configuration options for event prioritization
/// </summary>
public class EventPrioritizationOptions
{
    /// <summary>
    /// Relationship strength threshold for high priority events (0.0-1.0)
    /// </summary>
    public double HighPriorityRelationshipThreshold { get; set; } = 0.7;
    
    /// <summary>
    /// Maximum number of events to process in a single batch
    /// </summary>
    public int MaxEventsPerBatch { get; set; } = 25;
    
    /// <summary>
    /// Maximum age for events to be considered (in minutes)
    /// </summary>
    public int MaxEventAgeMinutes { get; set; } = 15;
}
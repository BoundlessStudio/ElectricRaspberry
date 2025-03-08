namespace ElectricRaspberry.Services.Observation.Configuration;

/// <summary>
/// Configuration options for the observer background service
/// </summary>
public class ObserverBackgroundOptions
{
    /// <summary>
    /// Interval in milliseconds between processing cycles
    /// </summary>
    public int ProcessingIntervalMs { get; set; } = 1000;
    
    /// <summary>
    /// Interval in milliseconds between maintenance operations
    /// </summary>
    public int MaintenanceIntervalMs { get; set; } = 60000; // 1 minute
}
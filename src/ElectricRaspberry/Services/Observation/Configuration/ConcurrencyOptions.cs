namespace ElectricRaspberry.Services.Observation.Configuration;

/// <summary>
/// Configuration options for concurrency management
/// </summary>
public class ConcurrencyOptions
{
    /// <summary>
    /// Timeout in milliseconds for acquiring a lock
    /// </summary>
    public int LockAcquisitionTimeoutMs { get; set; } = 5000; // 5 seconds
    
    /// <summary>
    /// Timeout in milliseconds for removing unused resources
    /// </summary>
    public int ResourceTimeoutMs { get; set; } = 300000; // 5 minutes
    
    /// <summary>
    /// Maximum number of concurrent operations
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 10;
}
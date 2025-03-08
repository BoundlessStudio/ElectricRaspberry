namespace ElectricRaspberry.Services.Observation.Configuration;

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Global rate limit in milliseconds between operations of the same type
    /// </summary>
    public int GlobalRateLimitMs { get; set; } = 1000;
    
    /// <summary>
    /// Channel-specific limit window size in milliseconds
    /// </summary>
    public int ChannelLimitWindowMs { get; set; } = 60000; // 1 minute
    
    /// <summary>
    /// Maximum number of operations per channel within a window
    /// </summary>
    public int OperationsPerChannelPerWindow { get; set; } = 5;
    
    /// <summary>
    /// Backoff multiplier for consecutive throttled operations
    /// </summary>
    public double BackoffMultiplier { get; set; } = 1.5;
}
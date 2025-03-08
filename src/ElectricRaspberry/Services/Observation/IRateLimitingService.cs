namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Interface for rate limiting and throttling operations
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// Determines whether an operation can be performed based on rate limits
    /// </summary>
    /// <param name="operationKey">A unique key identifying the operation type</param>
    /// <param name="channelId">The channel ID where the operation would be performed</param>
    /// <returns>True if the operation can be performed; otherwise, false</returns>
    bool CanPerformOperation(string operationKey, string channelId);
    
    /// <summary>
    /// Records that an operation has been performed, updating rate limit tracking
    /// </summary>
    /// <param name="operationKey">A unique key identifying the operation type</param>
    /// <param name="channelId">The channel ID where the operation was performed</param>
    void RecordOperation(string operationKey, string channelId);
    
    /// <summary>
    /// Gets the time to wait before the next operation can be performed
    /// </summary>
    /// <param name="operationKey">A unique key identifying the operation type</param>
    /// <param name="channelId">The channel ID where the operation would be performed</param>
    /// <returns>A timespan to wait, or TimeSpan.Zero if no waiting is needed</returns>
    TimeSpan GetTimeToWait(string operationKey, string channelId);
}
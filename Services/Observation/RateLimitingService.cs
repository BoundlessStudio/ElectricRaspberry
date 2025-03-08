using ElectricRaspberry.Services.Observation.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Service for rate limiting and throttling operations
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly ConcurrentDictionary<string, DateTime> _lastOperationTimes = new();
    private readonly ConcurrentDictionary<string, (int Count, DateTime ResetTime)> _channelLimits = new();
    private readonly ILogger<RateLimitingService> _logger;
    private readonly RateLimitingOptions _options;
    
    /// <summary>
    /// Creates a new instance of the rate limiting service
    /// </summary>
    /// <param name="options">Rate limiting options</param>
    /// <param name="logger">Logger</param>
    public RateLimitingService(
        IOptions<RateLimitingOptions> options,
        ILogger<RateLimitingService> logger)
    {
        _logger = logger;
        _options = options.Value;
    }
    
    /// <summary>
    /// Determines whether an operation can be performed based on rate limits
    /// </summary>
    /// <param name="operationKey">A unique key identifying the operation type</param>
    /// <param name="channelId">The channel ID where the operation would be performed</param>
    /// <returns>True if the operation can be performed; otherwise, false</returns>
    public bool CanPerformOperation(string operationKey, string channelId)
    {
        // Check global rate limit for this operation type
        if (_lastOperationTimes.TryGetValue(operationKey, out var lastTime))
        {
            var elapsed = DateTime.UtcNow - lastTime;
            if (elapsed < TimeSpan.FromMilliseconds(_options.GlobalRateLimitMs))
            {
                _logger.LogDebug("Rate limit hit for operation {OperationKey} - throttled for {Throttled}ms more", 
                    operationKey, (_options.GlobalRateLimitMs - elapsed.TotalMilliseconds));
                return false;
            }
        }
        
        // Check channel-specific rate limits
        var channelKey = $"{operationKey}:{channelId}";
        if (_channelLimits.TryGetValue(channelKey, out var limitInfo))
        {
            // If we're past the reset time, reset counter
            if (DateTime.UtcNow > limitInfo.ResetTime)
            {
                limitInfo = (0, DateTime.UtcNow.AddMilliseconds(_options.ChannelLimitWindowMs));
                _channelLimits[channelKey] = limitInfo;
            }
            
            // Check if we've hit the limit for this channel
            if (limitInfo.Count >= _options.OperationsPerChannelPerWindow)
            {
                var timeToReset = limitInfo.ResetTime - DateTime.UtcNow;
                _logger.LogDebug("Channel rate limit hit for channel {ChannelId}, operation {OperationKey} - reset in {ResetTime}ms", 
                    channelId, operationKey, timeToReset.TotalMilliseconds);
                return false;
            }
        }
        else
        {
            // Initialize a new channel limit
            limitInfo = (0, DateTime.UtcNow.AddMilliseconds(_options.ChannelLimitWindowMs));
            _channelLimits[channelKey] = limitInfo;
        }
        
        return true;
    }
    
    /// <summary>
    /// Records that an operation has been performed, updating rate limit tracking
    /// </summary>
    /// <param name="operationKey">A unique key identifying the operation type</param>
    /// <param name="channelId">The channel ID where the operation was performed</param>
    public void RecordOperation(string operationKey, string channelId)
    {
        // Update global rate limit tracker
        _lastOperationTimes[operationKey] = DateTime.UtcNow;
        
        // Update channel-specific counter
        var channelKey = $"{operationKey}:{channelId}";
        _channelLimits.AddOrUpdate(
            channelKey,
            // If key doesn't exist, add initial value
            key => (1, DateTime.UtcNow.AddMilliseconds(_options.ChannelLimitWindowMs)),
            // If key exists, update the value
            (key, existing) =>
            {
                // If we're past the reset time, start a new window
                if (DateTime.UtcNow > existing.ResetTime)
                {
                    return (1, DateTime.UtcNow.AddMilliseconds(_options.ChannelLimitWindowMs));
                }
                
                // Otherwise increment the counter
                return (existing.Count + 1, existing.ResetTime);
            });
    }
    
    /// <summary>
    /// Gets the time to wait before the next operation can be performed
    /// </summary>
    /// <param name="operationKey">A unique key identifying the operation type</param>
    /// <param name="channelId">The channel ID where the operation would be performed</param>
    /// <returns>A timespan to wait, or TimeSpan.Zero if no waiting is needed</returns>
    public TimeSpan GetTimeToWait(string operationKey, string channelId)
    {
        // Check global rate limit
        if (_lastOperationTimes.TryGetValue(operationKey, out var lastTime))
        {
            var elapsed = DateTime.UtcNow - lastTime;
            var globalRateLimit = TimeSpan.FromMilliseconds(_options.GlobalRateLimitMs);
            
            if (elapsed < globalRateLimit)
            {
                return globalRateLimit - elapsed;
            }
        }
        
        // Check channel-specific rate limit
        var channelKey = $"{operationKey}:{channelId}";
        if (_channelLimits.TryGetValue(channelKey, out var limitInfo))
        {
            // If count is at limit, return time until reset
            if (limitInfo.Count >= _options.OperationsPerChannelPerWindow)
            {
                return limitInfo.ResetTime - DateTime.UtcNow;
            }
        }
        
        return TimeSpan.Zero;
    }
}
using ElectricRaspberry.Models.Regulation;

namespace ElectricRaspberry.Services.Regulation;

/// <summary>
/// Service for managing the bot's self-regulation and engagement behaviors
/// </summary>
public interface ISelfRegulationService
{
    /// <summary>
    /// Determines whether the bot should engage in a conversation
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>True if the bot should engage, false otherwise</returns>
    Task<bool> ShouldEngageAsync(EngagementContext context);
    
    /// <summary>
    /// Gets the recommended delay before sending a message
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>The recommended delay timespan</returns>
    Task<TimeSpan> GetResponseDelayAsync(EngagementContext context);
    
    /// <summary>
    /// Gets the current activity level for a channel
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>The activity level</returns>
    Task<ActivityLevel> GetChannelActivityLevelAsync(string channelId);
    
    /// <summary>
    /// Updates the activity tracking for a channel
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <param name="messageCount">Number of new messages</param>
    /// <param name="interval">Time interval for these messages</param>
    /// <returns>The updated activity level</returns>
    Task<ActivityLevel> UpdateChannelActivityAsync(string channelId, int messageCount, TimeSpan interval);
    
    /// <summary>
    /// Records a bot message to a channel for throttling purposes
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <param name="messageId">The message ID</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RecordBotMessageAsync(string channelId, string messageId);
    
    /// <summary>
    /// Calculates the probability that the bot should engage proactively
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>Probability between 0 and 1</returns>
    Task<double> CalculateEngagementProbabilityAsync(EngagementContext context);
    
    /// <summary>
    /// Gets the time until the bot should consider initiating a conversation
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>Time until next potential initiation</returns>
    Task<TimeSpan> GetTimeUntilNextInitiationAsync(string channelId);
    
    /// <summary>
    /// Records that the bot initiated a conversation
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RecordInitiationAsync(string channelId);
    
    /// <summary>
    /// Builds the engagement context for a channel
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <param name="participantIds">IDs of users participating in the conversation</param>
    /// <returns>The built engagement context</returns>
    Task<EngagementContext> BuildEngagementContextAsync(string channelId, IEnumerable<string> participantIds);
    
    /// <summary>
    /// Checks whether the bot should perform an idle behavior
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>True if an idle behavior should be performed</returns>
    Task<bool> ShouldPerformIdleBehaviorAsync(string channelId);
    
    /// <summary>
    /// Gets the type of idle behavior to perform
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>The type of idle behavior to perform</returns>
    Task<string> GetIdleBehaviorTypeAsync(EngagementContext context);
}
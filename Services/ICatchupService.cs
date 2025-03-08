using ElectricRaspberry.Models.Conversation;

namespace ElectricRaspberry.Services;

/// <summary>
/// Service for managing missed messages during sleep mode and handling catchup
/// </summary>
public interface ICatchupService
{
    /// <summary>
    /// Adds a message to the catchup queue for processing when the bot wakes up
    /// </summary>
    /// <param name="messageEvent">The message event to queue</param>
    /// <returns>Task representing the operation</returns>
    Task AddToCatchupQueueAsync(MessageEvent messageEvent);
    
    /// <summary>
    /// Gets messages in the catchup queue
    /// </summary>
    /// <param name="count">Maximum number of messages to return</param>
    /// <param name="channelId">Optional channel ID to filter by</param>
    /// <returns>List of queued message events</returns>
    Task<IEnumerable<MessageEvent>> GetCatchupQueueAsync(int count = 100, ulong? channelId = null);
    
    /// <summary>
    /// Processes the catchup queue after waking up
    /// </summary>
    /// <param name="maxBatchSize">Maximum number of messages to process at once</param>
    /// <returns>Number of messages processed</returns>
    Task<int> ProcessCatchupQueueAsync(int maxBatchSize = 50);
    
    /// <summary>
    /// Gets a summary of missed messages during sleep
    /// </summary>
    /// <returns>A summary text of missed activity</returns>
    Task<string> GetMissedActivitySummaryAsync();
    
    /// <summary>
    /// Marks a message as processed in the catchup queue
    /// </summary>
    /// <param name="messageId">The message ID to mark as processed</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> MarkAsProcessedAsync(ulong messageId);
    
    /// <summary>
    /// Clears the catchup queue
    /// </summary>
    /// <returns>Number of messages cleared</returns>
    Task<int> ClearCatchupQueueAsync();
    
    /// <summary>
    /// Prioritizes the catchup queue based on importance
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task PrioritizeCatchupQueueAsync();
}
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
    /// <param name="queueItem">The catchup queue item to add</param>
    /// <returns>Task representing the operation</returns>
    Task AddToCatchupQueueAsync(CatchupQueueItem queueItem);
    
    /// <summary>
    /// Gets messages in the catchup queue
    /// </summary>
    /// <param name="count">Maximum number of messages to return</param>
    /// <param name="channelId">Optional channel ID to filter by</param>
    /// <returns>List of queued message events</returns>
    Task<List<MessageEvent>> GetCatchupQueueAsync(int count = 100, ulong? channelId = null);
    
    /// <summary>
    /// Processes the catchup queue after waking up
    /// </summary>
    /// <param name="maxBatchSize">Maximum number of messages to process at once</param>
    /// <returns>Number of messages processed</returns>
    Task<int> ProcessCatchupQueueAsync(int maxBatchSize = 50);
    
    /// <summary>
    /// Clears the catchup queue
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task ClearCatchupQueueAsync();
    
    /// <summary>
    /// Gets the size of the catchup queue
    /// </summary>
    /// <returns>Number of items in the queue</returns>
    Task<int> GetQueueSizeAsync();
    
    /// <summary>
    /// Gets the number of unprocessed items in the catchup queue
    /// </summary>
    /// <returns>Number of unprocessed items</returns>
    Task<int> GetUnprocessedQueueSizeAsync();
}
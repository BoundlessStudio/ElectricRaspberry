namespace ElectricRaspberry.Models.Conversation;

/// <summary>
/// Represents a message in the catchup queue
/// </summary>
public class CatchupQueueItem
{
    /// <summary>
    /// The message event to process
    /// </summary>
    public MessageEvent MessageEvent { get; set; }
    
    /// <summary>
    /// The channel ID where the message was posted
    /// </summary>
    public string ChannelId { get; set; }
    
    /// <summary>
    /// When the message was added to the queue
    /// </summary>
    public DateTime QueuedAt { get; set; }
    
    /// <summary>
    /// Whether the message has been processed
    /// </summary>
    public bool IsProcessed { get; set; }
    
    /// <summary>
    /// Priority of this message (higher = more important)
    /// </summary>
    public double Priority { get; set; }
    
    /// <summary>
    /// When the message was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; }
    
    /// <summary>
    /// Creates a new catchup queue item
    /// </summary>
    public CatchupQueueItem()
    {
        QueuedAt = DateTime.UtcNow;
        IsProcessed = false;
        Priority = 0;
        ProcessedAt = DateTime.MinValue;
    }
    
    /// <summary>
    /// Creates a new catchup queue item
    /// </summary>
    public CatchupQueueItem(MessageEvent messageEvent, string channelId, double priority = 0)
    {
        MessageEvent = messageEvent;
        ChannelId = channelId;
        QueuedAt = DateTime.UtcNow;
        IsProcessed = false;
        Priority = priority;
        ProcessedAt = DateTime.MinValue;
    }
}
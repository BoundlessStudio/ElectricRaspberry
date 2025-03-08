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
    public int Priority { get; set; }
    
    /// <summary>
    /// Creates a new catchup queue item
    /// </summary>
    public CatchupQueueItem(MessageEvent messageEvent, int priority = 0)
    {
        MessageEvent = messageEvent;
        QueuedAt = DateTime.UtcNow;
        IsProcessed = false;
        Priority = priority;
    }
}
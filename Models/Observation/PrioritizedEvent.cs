using ElectricRaspberry.Models.Conversation;

namespace ElectricRaspberry.Models.Observation;

/// <summary>
/// Represents a message event with an assigned priority level for processing
/// </summary>
public class PrioritizedEvent : IComparable<PrioritizedEvent>
{
    /// <summary>
    /// Gets the underlying message event
    /// </summary>
    public MessageEvent MessageEvent { get; }
    
    /// <summary>
    /// Gets the priority level of this event
    /// </summary>
    public EventPriority Priority { get; }
    
    /// <summary>
    /// Gets the timestamp when this event was prioritized
    /// </summary>
    public DateTimeOffset PrioritizedAt { get; }
    
    /// <summary>
    /// Gets the channel ID where this event originated
    /// </summary>
    public string ChannelId { get; }
    
    /// <summary>
    /// Creates a new prioritized event
    /// </summary>
    /// <param name="messageEvent">The message event</param>
    /// <param name="priority">The priority level</param>
    /// <param name="channelId">The channel ID</param>
    public PrioritizedEvent(MessageEvent messageEvent, EventPriority priority, string channelId)
    {
        MessageEvent = messageEvent;
        Priority = priority;
        PrioritizedAt = DateTimeOffset.UtcNow;
        ChannelId = channelId;
    }
    
    /// <summary>
    /// Compares this prioritized event with another, first by priority level then by timestamp
    /// </summary>
    /// <param name="other">The other prioritized event</param>
    /// <returns>Comparison result for sorting</returns>
    public int CompareTo(PrioritizedEvent? other)
    {
        if (other == null) return 1;
        
        // First compare by priority level (higher priority comes first)
        var priorityCompare = other.Priority.CompareTo(Priority);
        if (priorityCompare != 0) return priorityCompare;
        
        // Then compare by timestamp (older events come first)
        return MessageEvent.Timestamp.CompareTo(other.MessageEvent.Timestamp);
    }
}
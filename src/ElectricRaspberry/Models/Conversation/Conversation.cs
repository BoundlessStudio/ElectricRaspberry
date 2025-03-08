using Discord;

namespace ElectricRaspberry.Models.Conversation;

/// <summary>
/// Represents an ongoing conversation with one or more users
/// </summary>
public class Conversation
{
    /// <summary>
    /// Unique identifier for this conversation
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// When the conversation started
    /// </summary>
    public DateTime StartedAt { get; set; }
    
    /// <summary>
    /// When the conversation was last active
    /// </summary>
    public DateTime LastActiveAt { get; set; }
    
    /// <summary>
    /// Alias for LastActiveAt to maintain backward compatibility
    /// </summary>
    public DateTime LastActivityTime => LastActiveAt;
    
    /// <summary>
    /// The channel where this conversation is happening
    /// </summary>
    public ulong ChannelId { get; set; }
    
    /// <summary>
    /// The channel name (for reference)
    /// </summary>
    public string ChannelName { get; set; }
    
    /// <summary>
    /// Whether this is a direct message conversation
    /// </summary>
    public bool IsDirectMessage { get; set; }
    
    /// <summary>
    /// List of user IDs participating in this conversation
    /// </summary>
    public List<ulong> ParticipantIds { get; set; } = new();
    
    /// <summary>
    /// The message IDs in this conversation
    /// </summary>
    public List<MessageReference> Messages { get; set; } = new();
    
    /// <summary>
    /// The primary topic or subject of the conversation
    /// </summary>
    public string Topic { get; set; } = string.Empty;
    
    /// <summary>
    /// The current conversation state
    /// </summary>
    public ConversationState State { get; set; } = ConversationState.Active;
    
    /// <summary>
    /// How important this conversation is (0-1)
    /// </summary>
    public double Importance { get; set; } = 0.5;
    
    /// <summary>
    /// Custom properties or context for this conversation
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();
    
    /// <summary>
    /// Creates a new conversation from a message event
    /// </summary>
    public Conversation(MessageEvent messageEvent)
    {
        Id = Guid.NewGuid().ToString();
        StartedAt = DateTime.UtcNow;
        LastActiveAt = DateTime.UtcNow;
        
        ChannelId = messageEvent.Channel.Id;
        ChannelName = messageEvent.Channel is ITextChannel textChannel 
            ? textChannel.Name 
            : messageEvent.Channel.ToString() ?? "Unknown";
        
        IsDirectMessage = messageEvent.IsDirectMessage;
        
        // Add the message author as a participant if they're not a bot
        if (!messageEvent.IsFromBot)
        {
            ParticipantIds.Add(messageEvent.Message.Author.Id);
        }
        
        // Add the initial message
        Messages.Add(new MessageReference
        {
            MessageId = messageEvent.Message.Id,
            AuthorId = messageEvent.Message.Author.Id,
            Timestamp = messageEvent.Timestamp.UtcDateTime,
            Content = messageEvent.Message.Content
        });
    }
    
    /// <summary>
    /// Adds a message to this conversation
    /// </summary>
    public void AddMessage(MessageEvent messageEvent)
    {
        Messages.Add(new MessageReference
        {
            MessageId = messageEvent.Message.Id,
            AuthorId = messageEvent.Message.Author.Id,
            Timestamp = messageEvent.Timestamp.UtcDateTime,
            Content = messageEvent.Message.Content
        });
        
        // Update last active timestamp
        LastActiveAt = DateTime.UtcNow;
        
        // Add the participant if they're not already in the list and not a bot
        if (!messageEvent.IsFromBot && !ParticipantIds.Contains(messageEvent.Message.Author.Id))
        {
            ParticipantIds.Add(messageEvent.Message.Author.Id);
        }
    }
    
    /// <summary>
    /// Checks if the conversation is idle based on elapsed time
    /// </summary>
    public bool IsIdle(TimeSpan idleThreshold)
    {
        return DateTime.UtcNow - LastActiveAt > idleThreshold;
    }
    
    /// <summary>
    /// Gets the most recent messages in the conversation
    /// </summary>
    public List<MessageReference> GetRecentMessages(int count)
    {
        return Messages.OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToList();
    }
    
    /// <summary>
    /// Gets a summary of the conversation
    /// </summary>
    public string GetSummary()
    {
        var participants = ParticipantIds.Count;
        var messageCount = Messages.Count;
        var duration = LastActiveAt - StartedAt;
        
        return $"Conversation in {ChannelName} with {participants} participants, " +
               $"{messageCount} messages over {duration.TotalMinutes:0.0} minutes. " +
               $"Topic: {(string.IsNullOrEmpty(Topic) ? "Unspecified" : Topic)}";
    }
}
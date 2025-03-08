namespace ElectricRaspberry.Models.Conversation;

/// <summary>
/// Represents a reference to a message in a conversation
/// </summary>
public class MessageReference
{
    /// <summary>
    /// Discord message ID
    /// </summary>
    public ulong MessageId { get; set; }
    
    /// <summary>
    /// Discord user ID of the author
    /// </summary>
    public ulong AuthorId { get; set; }
    
    /// <summary>
    /// When the message was sent
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this message contains a mention of the bot
    /// </summary>
    public bool ContainsBotMention { get; set; }
    
    /// <summary>
    /// Whether this message was sent by the bot
    /// </summary>
    public bool IsFromBot { get; set; }
}
using Discord;
using Discord.WebSocket;

namespace ElectricRaspberry.Models.Conversation;

/// <summary>
/// Represents a Discord message event with additional context
/// </summary>
public class MessageEvent
{
    /// <summary>
    /// The original Discord message
    /// </summary>
    public IMessage Message { get; set; }
    
    /// <summary>
    /// When the message was received
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// The channel where the message was sent
    /// </summary>
    public IChannel Channel { get; set; }
    
    /// <summary>
    /// Whether the message mentions the bot
    /// </summary>
    public bool MentionsBot { get; set; }
    
    /// <summary>
    /// Whether the message was sent in a DM
    /// </summary>
    public bool IsDirectMessage { get; set; }
    
    /// <summary>
    /// Whether the message is from the bot itself
    /// </summary>
    public bool IsFromBot { get; set; }
    
    /// <summary>
    /// Whether the bot has processed this message
    /// </summary>
    public bool IsProcessed { get; set; }
    
    /// <summary>
    /// The conversation this message belongs to
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the message
    /// </summary>
    public ulong MessageId => Message.Id;
    
    /// <summary>
    /// The ID of the message author
    /// </summary>
    public ulong AuthorId => Message.Author.Id;
    
    /// <summary>
    /// The ID of the channel where the message was sent
    /// </summary>
    public ulong ChannelId => Channel.Id;
    
    /// <summary>
    /// Creates a new message event from a Discord message
    /// </summary>
    public MessageEvent(IMessage message, DateTimeOffset timestamp)
    {
        Message = message;
        Timestamp = timestamp;
        Channel = message.Channel;
        
        // Determine if it's a direct message
        IsDirectMessage = Channel is IPrivateChannel;
        
        // Set from bot flag
        IsFromBot = message.Author.IsBot;
        
        // Initialize other properties
        IsProcessed = false;
        MentionsBot = false;
        
        // Default conversation ID is channel ID, can be updated later
        ConversationId = Channel.Id.ToString();
    }
    
    /// <summary>
    /// Gets the user-friendly representation of the message source
    /// </summary>
    public string GetSourceDescription()
    {
        if (IsDirectMessage)
        {
            return $"DM from {Message.Author.Username}";
        }
        else
        {
            var textChannel = Channel as ITextChannel;
            return $"#{textChannel?.Name ?? "channel"} in {(textChannel?.Guild.Name ?? "unknown")}";
        }
    }
    
    /// <summary>
    /// Checks if the specified user is mentioned in this message
    /// </summary>
    public bool IsMentioned(ulong userId)
    {
        return Message.MentionedUserIds.Contains(userId);
    }
}
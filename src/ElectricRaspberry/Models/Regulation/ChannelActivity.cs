namespace ElectricRaspberry.Models.Regulation;

/// <summary>
/// Represents activity tracking for a channel
/// </summary>
public class ChannelActivity
{
    /// <summary>
    /// Gets or sets the channel ID
    /// </summary>
    public string ChannelId { get; set; }
    
    /// <summary>
    /// Gets or sets the current activity level
    /// </summary>
    public ActivityLevel Level { get; set; } = ActivityLevel.Inactive;
    
    /// <summary>
    /// Gets or sets the timestamp of the last message in the channel
    /// </summary>
    public DateTimeOffset LastMessageTimestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Gets or sets the timestamp of the bot's last message in the channel
    /// </summary>
    public DateTimeOffset? LastBotMessageTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp of the bot's last conversation initiation
    /// </summary>
    public DateTimeOffset? LastInitiationTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the next planned initiation timestamp
    /// </summary>
    public DateTimeOffset? NextInitiationTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the average time between messages in seconds
    /// </summary>
    public double AverageTimeBetweenMessagesSeconds { get; set; } = 60;
    
    /// <summary>
    /// Gets or sets the number of messages in the current tracking window
    /// </summary>
    public int MessageCount { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the most active conversation in the channel
    /// </summary>
    public string ActiveConversationId { get; set; }
    
    /// <summary>
    /// Gets or sets the list of recent participant IDs
    /// </summary>
    public List<string> RecentParticipants { get; set; } = new List<string>();
    
    /// <summary>
    /// Gets or sets the count of the bot's messages in recent history
    /// </summary>
    public int RecentBotMessageCount { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp of the last idle behavior performed
    /// </summary>
    public DateTimeOffset? LastIdleBehaviorTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets whether the bot is currently in a conversation in this channel
    /// </summary>
    public bool IsEngaged { get; set; }
    
    /// <summary>
    /// Gets or sets the start time of the current engagement
    /// </summary>
    public DateTimeOffset? EngagementStartTimestamp { get; set; }
}
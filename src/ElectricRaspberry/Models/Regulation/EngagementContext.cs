using ElectricRaspberry.Models.Emotions;

namespace ElectricRaspberry.Models.Regulation;

/// <summary>
/// Contains context information used to make engagement decisions
/// </summary>
public class EngagementContext
{
    /// <summary>
    /// Gets or sets the channel ID
    /// </summary>
    public string ChannelId { get; set; }
    
    /// <summary>
    /// Gets or sets the current activity level in the channel
    /// </summary>
    public ActivityLevel ActivityLevel { get; set; }
    
    /// <summary>
    /// Gets or sets the user IDs participating in the conversation
    /// </summary>
    public List<string> ParticipantIds { get; set; } = new List<string>();
    
    /// <summary>
    /// Gets or sets the average relationship strength with participants (0-1)
    /// </summary>
    public double AverageRelationshipStrength { get; set; }
    
    /// <summary>
    /// Gets or sets the current bot stamina level (0-100)
    /// </summary>
    public double CurrentStamina { get; set; }
    
    /// <summary>
    /// Gets or sets the current emotional state
    /// </summary>
    public EmotionalState CurrentEmotionalState { get; set; }
    
    /// <summary>
    /// Gets or sets the relevance score for the current topic to the bot's interests (0-1)
    /// </summary>
    public double TopicRelevance { get; set; }
    
    /// <summary>
    /// Gets or sets the time since the bot's last message in this channel
    /// </summary>
    public TimeSpan TimeSinceLastMessage { get; set; }
    
    /// <summary>
    /// Gets or sets how many messages have been sent since the bot's last message
    /// </summary>
    public int MessagesSinceLastBotMessage { get; set; }
    
    /// <summary>
    /// Gets or sets whether the current topic is one the bot has knowledge about
    /// </summary>
    public bool HasKnowledgeOfTopic { get; set; }
    
    /// <summary>
    /// Gets or sets the direct mention status (true if bot was mentioned recently)
    /// </summary>
    public bool WasRecentlyMentioned { get; set; }
    
    /// <summary>
    /// Gets or sets the conversation importance (0-1)
    /// </summary>
    public double ConversationImportance { get; set; }
}
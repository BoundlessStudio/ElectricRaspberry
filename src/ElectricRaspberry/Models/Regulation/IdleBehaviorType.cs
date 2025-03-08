namespace ElectricRaspberry.Models.Regulation;

/// <summary>
/// Represents the types of idle behaviors the bot can perform
/// </summary>
public static class IdleBehaviorType
{
    /// <summary>
    /// Reacting to a message with an emoji
    /// </summary>
    public const string EmojiReaction = "EmojiReaction";
    
    /// <summary>
    /// Changing status or activity
    /// </summary>
    public const string StatusChange = "StatusChange";
    
    /// <summary>
    /// Joining a voice channel without speaking
    /// </summary>
    public const string VoicePresence = "VoicePresence";
    
    /// <summary>
    /// Initiating a brief conversation about a shared interest
    /// </summary>
    public const string InterestPrompt = "InterestPrompt";
    
    /// <summary>
    /// Sharing a relevant observation about the channel or server
    /// </summary>
    public const string ChannelObservation = "ChannelObservation";
    
    /// <summary>
    /// Asking an open-ended question to the channel
    /// </summary>
    public const string OpenQuestion = "OpenQuestion";
    
    /// <summary>
    /// Sharing a thought about a previous conversation
    /// </summary>
    public const string RecallPreviousConversation = "RecallPreviousConversation";
}
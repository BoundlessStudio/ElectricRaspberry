namespace ElectricRaspberry.Models.Conversation;

/// <summary>
/// Represents the current state of a conversation
/// </summary>
public enum ConversationState
{
    /// <summary>
    /// Active conversation requiring normal attention
    /// </summary>
    Active,
    
    /// <summary>
    /// Idle conversation with no recent activity
    /// </summary>
    Idle,
    
    /// <summary>
    /// Conversation has been marked as completed
    /// </summary>
    Completed,
    
    /// <summary>
    /// Conversation is currently paused (e.g., during sleep mode)
    /// </summary>
    Paused,
    
    /// <summary>
    /// Conversation is waiting for a specific event or response
    /// </summary>
    Waiting,
    
    /// <summary>
    /// Conversation requires urgent attention
    /// </summary>
    Urgent
}
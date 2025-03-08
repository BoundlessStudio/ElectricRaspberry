using ElectricRaspberry.Models.Conversation;

namespace ElectricRaspberry.Services;

/// <summary>
/// Service for managing conversations and their contexts
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Processes a message event and adds it to the appropriate conversation
    /// </summary>
    /// <param name="messageEvent">The message event to process</param>
    /// <returns>The conversation containing the message</returns>
    Task<Conversation> ProcessMessageAsync(MessageEvent messageEvent);
    
    /// <summary>
    /// Gets a specific conversation by ID
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <returns>The conversation if found, null otherwise</returns>
    Task<Conversation?> GetConversationAsync(string conversationId);
    
    /// <summary>
    /// Gets all active conversations
    /// </summary>
    /// <returns>List of active conversations</returns>
    Task<IEnumerable<Conversation>> GetActiveConversationsAsync();
    
    /// <summary>
    /// Gets active conversations in a specific channel
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>List of active conversations in the channel</returns>
    Task<IEnumerable<Conversation>> GetChannelConversationsAsync(ulong channelId);
    
    /// <summary>
    /// Marks a conversation as completed
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> CompleteConversationAsync(string conversationId);
    
    /// <summary>
    /// Updates the topic of a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="topic">The new topic</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateConversationTopicAsync(string conversationId, string topic);
    
    /// <summary>
    /// Sets the importance of a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="importance">The importance value (0-1)</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SetConversationImportanceAsync(string conversationId, double importance);
    
    /// <summary>
    /// Gets recent messages from a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="count">Maximum number of messages to return</param>
    /// <returns>List of recent messages</returns>
    Task<IEnumerable<MessageReference>> GetRecentMessagesAsync(string conversationId, int count = 10);
    
    /// <summary>
    /// Creates a context string for an AI response in a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <returns>Context string with conversation history</returns>
    Task<string> CreateConversationContextAsync(string conversationId);
    
    /// <summary>
    /// Performs maintenance on conversations (cleanup, state updates)
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task PerformMaintenanceAsync();
}
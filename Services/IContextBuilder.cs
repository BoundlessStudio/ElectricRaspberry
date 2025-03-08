using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Emotions;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Service responsible for building and maintaining contextual information for the bot's thinking process
    /// </summary>
    public interface IContextBuilder
    {
        /// <summary>
        /// Gets the current context for a thinking operation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to build context for</param>
        /// <returns>The current context as a structured object</returns>
        Task<ThinkingContext> GetContextAsync(string conversationId);

        /// <summary>
        /// Updates the context with new information
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to update</param>
        /// <param name="messageEvent">The message event to add to context</param>
        Task UpdateContextAsync(string conversationId, MessageEvent messageEvent);

        /// <summary>
        /// Gets information about a specific user to include in context
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Contextual information about the user</returns>
        Task<UserContext> GetUserContextAsync(string userId);

        /// <summary>
        /// Gets information about the current environment (channel, server, etc.)
        /// </summary>
        /// <param name="channelId">The ID of the channel</param>
        /// <returns>Environment context information</returns>
        Task<EnvironmentContext> GetEnvironmentContextAsync(string channelId);

        /// <summary>
        /// Retrieves relevant memories and knowledge for the given context
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="query">Optional query to narrow down relevant memories</param>
        /// <returns>Knowledge context with relevant memories and information</returns>
        Task<KnowledgeContext> GetKnowledgeContextAsync(string conversationId, string query = null);
        
        /// <summary>
        /// Prunes old or irrelevant context to maintain optimal context size
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to prune</param>
        /// <returns>The number of items pruned from context</returns>
        Task<int> PruneContextAsync(string conversationId);
    }

    /// <summary>
    /// Represents the complete context for a thinking operation
    /// </summary>
    public class ThinkingContext
    {
        /// <summary>
        /// The ID of the conversation this context is for
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// The current conversation state
        /// </summary>
        public ConversationState State { get; set; }

        /// <summary>
        /// Recent messages in the conversation for context
        /// </summary>
        public List<MessageEvent> RecentMessages { get; set; } = new List<MessageEvent>();

        /// <summary>
        /// Information about the environment (channel, server, etc.)
        /// </summary>
        public EnvironmentContext Environment { get; set; }

        /// <summary>
        /// Information about users in the conversation
        /// </summary>
        public Dictionary<string, UserContext> Users { get; set; } = new Dictionary<string, UserContext>();

        /// <summary>
        /// Relevant knowledge and memories
        /// </summary>
        public KnowledgeContext Knowledge { get; set; }

        /// <summary>
        /// The bot's current emotional state
        /// </summary>
        public EmotionalState BotEmotionalState { get; set; }

        /// <summary>
        /// The current timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata for the context
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents information about a user for context
    /// </summary>
    public class UserContext
    {
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Relationship strength with the bot (0.0-1.0)
        /// </summary>
        public double RelationshipStrength { get; set; }

        /// <summary>
        /// User's known interests with their relevance scores
        /// </summary>
        public Dictionary<string, double> Interests { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Recent interaction count with this user
        /// </summary>
        public int RecentInteractionCount { get; set; }

        /// <summary>
        /// Whether this user is an administrator
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// Additional metadata about the user
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents information about the environment for context
    /// </summary>
    public class EnvironmentContext
    {
        /// <summary>
        /// Channel ID
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// Server/Guild ID
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        /// Channel name
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// Server/Guild name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Channel type (text, voice, etc.)
        /// </summary>
        public string ChannelType { get; set; }

        /// <summary>
        /// Topic or description of the channel
        /// </summary>
        public string ChannelTopic { get; set; }

        /// <summary>
        /// Whether the channel is private
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Last active timestamp in this channel
        /// </summary>
        public DateTime LastActiveTime { get; set; }

        /// <summary>
        /// Number of users in this channel
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Additional metadata about the environment
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents knowledge and memories for context
    /// </summary>
    public class KnowledgeContext
    {
        /// <summary>
        /// Relevant memories for the current context
        /// </summary>
        public List<MemoryItem> RelevantMemories { get; set; } = new List<MemoryItem>();

        /// <summary>
        /// Relevant facts about topics in the conversation
        /// </summary>
        public List<FactItem> RelevantFacts { get; set; } = new List<FactItem>();

        /// <summary>
        /// Importance score for the knowledge (0.0-1.0)
        /// </summary>
        public double ImportanceScore { get; set; }

        /// <summary>
        /// Timestamp when this knowledge was retrieved
        /// </summary>
        public DateTime RetrievalTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a memory item for context
    /// </summary>
    public class MemoryItem
    {
        /// <summary>
        /// Unique ID of the memory
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Content of the memory
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// When the memory was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Importance score of the memory (0.0-1.0)
        /// </summary>
        public double Importance { get; set; }

        /// <summary>
        /// Relevance score to the current context (0.0-1.0)
        /// </summary>
        public double ContextRelevance { get; set; }

        /// <summary>
        /// User IDs associated with this memory
        /// </summary>
        public List<string> AssociatedUserIds { get; set; } = new List<string>();

        /// <summary>
        /// Additional metadata about the memory
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a fact item for context
    /// </summary>
    public class FactItem
    {
        /// <summary>
        /// Unique ID of the fact
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The topic this fact is about
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Content of the fact
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Confidence score in this fact (0.0-1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// When this fact was learned
        /// </summary>
        public DateTime LearnedAt { get; set; }

        /// <summary>
        /// Source of this fact
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Relevance score to the current context (0.0-1.0)
        /// </summary>
        public double ContextRelevance { get; set; }
    }
}
namespace ElectricRaspberry.Configuration
{
    /// <summary>
    /// Configuration options for the context builder
    /// </summary>
    public class ContextOptions
    {
        public const string ConfigSection = "Context";

        /// <summary>
        /// Maximum number of recent messages to keep in context
        /// </summary>
        public int MaxRecentMessages { get; set; } = 20;

        /// <summary>
        /// Maximum number of relevant memories to include in context
        /// </summary>
        public int MaxRelevantMemories { get; set; } = 10;

        /// <summary>
        /// Maximum number of relevant facts to include in context
        /// </summary>
        public int MaxRelevantFacts { get; set; } = 15;

        /// <summary>
        /// Minimum relevance score (0.0-1.0) for memories to be included
        /// </summary>
        public double MinMemoryRelevance { get; set; } = 0.5;

        /// <summary>
        /// Minimum relevance score (0.0-1.0) for facts to be included
        /// </summary>
        public double MinFactRelevance { get; set; } = 0.6;

        /// <summary>
        /// How many users to include detailed context for
        /// </summary>
        public int MaxUsersInContext { get; set; } = 5;

        /// <summary>
        /// How long to keep messages in context (in minutes)
        /// </summary>
        public int MessageExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Whether to include quoted messages when updating context
        /// </summary>
        public bool IncludeQuotedMessages { get; set; } = true;

        /// <summary>
        /// How often to refresh user context information (in minutes)
        /// </summary>
        public int UserContextRefreshMinutes { get; set; } = 15;

        /// <summary>
        /// Whether to include environment information in context
        /// </summary>
        public bool IncludeEnvironmentContext { get; set; } = true;

        /// <summary>
        /// Whether to enable automatic context pruning
        /// </summary>
        public bool EnableAutoPruning { get; set; } = true;

        /// <summary>
        /// Maximum context size in characters (soft limit)
        /// </summary>
        public int MaxContextSize { get; set; } = 16000;
    }
}
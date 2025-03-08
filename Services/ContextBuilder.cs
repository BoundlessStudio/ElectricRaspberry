using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Emotions;
using ElectricRaspberry.Models.Knowledge.Edges;
using ElectricRaspberry.Models.Knowledge.Vertices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Implementation of IContextBuilder for managing thinking context
    /// </summary>
    public class ContextBuilder : IContextBuilder
    {
        private readonly ILogger<ContextBuilder> _logger;
        private readonly ContextOptions _options;
        private readonly IKnowledgeService _knowledgeService;
        private readonly IConversationService _conversationService;
        private readonly IEmotionalService _emotionalService;
        private readonly IPersonaService _personaService;
        
        // Cache for thinking contexts by conversation ID
        private readonly ConcurrentDictionary<string, ThinkingContext> _contextCache = new();
        
        // Cache for user contexts by user ID
        private readonly ConcurrentDictionary<string, (UserContext Context, DateTime LastRefresh)> _userContextCache = new();
        
        // Cache for environment contexts by channel ID
        private readonly ConcurrentDictionary<string, (EnvironmentContext Context, DateTime LastRefresh)> _environmentContextCache = new();

        public ContextBuilder(
            ILogger<ContextBuilder> logger,
            IOptions<ContextOptions> options,
            IKnowledgeService knowledgeService,
            IConversationService conversationService,
            IEmotionalService emotionalService,
            IPersonaService personaService)
        {
            _logger = logger;
            _options = options.Value;
            _knowledgeService = knowledgeService;
            _conversationService = conversationService;
            _emotionalService = emotionalService;
            _personaService = personaService;
        }

        /// <inheritdoc/>
        public async Task<ThinkingContext> GetContextAsync(string conversationId)
        {
            try
            {
                // Try to get from cache first
                if (_contextCache.TryGetValue(conversationId, out var cachedContext))
                {
                    // Check if context needs updating (e.g., emotional state)
                    await RefreshDynamicContextDataAsync(cachedContext);
                    return cachedContext;
                }

                // Create new context if not found in cache
                var context = new ThinkingContext
                {
                    ConversationId = conversationId,
                    Timestamp = DateTime.UtcNow
                };

                // Get conversation data
                var conversation = await _conversationService.GetConversationAsync(conversationId);
                if (conversation != null)
                {
                    context.State = conversation.State;
                    context.RecentMessages = conversation.Messages
                        .OrderByDescending(m => m.Timestamp)
                        .Take(_options.MaxRecentMessages)
                        .ToList();
                }
                else
                {
                    _logger.LogWarning("No conversation found for ID {conversationId} when building context", conversationId);
                    context.State = ConversationState.New;
                }

                // Get environment context for the channel (if we have channel info)
                var channelId = context.RecentMessages.FirstOrDefault()?.ChannelId;
                if (!string.IsNullOrEmpty(channelId) && _options.IncludeEnvironmentContext)
                {
                    context.Environment = await GetEnvironmentContextAsync(channelId);
                }

                // Get user contexts for all participants
                var userIds = context.RecentMessages
                    .Select(m => m.AuthorId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .Take(_options.MaxUsersInContext)
                    .ToList();

                foreach (var userId in userIds)
                {
                    context.Users[userId] = await GetUserContextAsync(userId);
                }

                // Get knowledge context with relevant memories and facts
                context.Knowledge = await GetKnowledgeContextAsync(conversationId);

                // Get bot's current emotional state
                context.BotEmotionalState = await _emotionalService.GetCurrentEmotionalStateAsync();

                // Cache the context
                _contextCache[conversationId] = context;
                
                _logger.LogInformation("Built new thinking context for conversation {conversationId} with {messageCount} messages and {userCount} users",
                    conversationId, context.RecentMessages.Count, context.Users.Count);

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building context for conversation {conversationId}", conversationId);
                
                // Return minimal context in case of errors
                return new ThinkingContext
                {
                    ConversationId = conversationId,
                    State = ConversationState.Unknown,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { { "Error", "Error building full context" } }
                };
            }
        }

        /// <inheritdoc/>
        public async Task UpdateContextAsync(string conversationId, MessageEvent messageEvent)
        {
            try
            {
                // Try to get existing context
                if (!_contextCache.TryGetValue(conversationId, out var context))
                {
                    // Create new context if it doesn't exist
                    context = await GetContextAsync(conversationId);
                }

                // Add the new message to the context
                context.RecentMessages.Insert(0, messageEvent);
                
                // Ensure we don't exceed the maximum recent messages limit
                if (context.RecentMessages.Count > _options.MaxRecentMessages)
                {
                    context.RecentMessages = context.RecentMessages
                        .Take(_options.MaxRecentMessages)
                        .ToList();
                }

                // Update user context for the message author
                if (!string.IsNullOrEmpty(messageEvent.AuthorId))
                {
                    var userContext = await GetUserContextAsync(messageEvent.AuthorId);
                    userContext.RecentInteractionCount++;
                    context.Users[messageEvent.AuthorId] = userContext;
                }

                // Update timestamp
                context.Timestamp = DateTime.UtcNow;

                // Update the bot's emotional state
                context.BotEmotionalState = await _emotionalService.GetCurrentEmotionalStateAsync();

                // Auto-prune context if enabled
                if (_options.EnableAutoPruning)
                {
                    await PruneContextAsync(conversationId);
                }

                _logger.LogDebug("Updated thinking context for conversation {conversationId} with new message", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating context for conversation {conversationId}", conversationId);
            }
        }

        /// <inheritdoc/>
        public async Task<UserContext> GetUserContextAsync(string userId)
        {
            try
            {
                // Check if we have a cached user context that's still fresh
                if (_userContextCache.TryGetValue(userId, out var cachedContext))
                {
                    var refreshThreshold = DateTime.UtcNow.AddMinutes(-_options.UserContextRefreshMinutes);
                    if (cachedContext.LastRefresh > refreshThreshold)
                    {
                        return cachedContext.Context;
                    }
                }

                // Create new user context
                var userContext = new UserContext { UserId = userId };

                // Get user relationship data from knowledge graph
                var relationships = await _knowledgeService.GetEdgesByTypeAsync<RelationshipEdge>(
                    edge => edge.TargetId == userId);
                
                if (relationships != null && relationships.Any())
                {
                    var relationship = relationships.First();
                    userContext.RelationshipStrength = relationship.Strength;
                }
                else
                {
                    userContext.RelationshipStrength = 0.1; // Default for new users
                }

                // Get user interests from knowledge graph
                var interestEdges = await _knowledgeService.GetEdgesByTypeAsync<InterestEdge>(
                    edge => edge.Properties.ContainsKey("UserId") && 
                           edge.Properties["UserId"].ToString() == userId);

                if (interestEdges != null)
                {
                    foreach (var interest in interestEdges)
                    {
                        try
                        {
                            var topic = await _knowledgeService.GetVertexByIdAsync<TopicVertex>(interest.TargetId);
                            if (topic != null)
                            {
                                userContext.Interests[topic.Name] = interest.Level;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error retrieving topic for interest {interestId}", interest.Id);
                        }
                    }
                }

                // Try to get username and other details from Discord service or other sources
                // This would typically come from a Discord user resolution service or database

                // Cache the user context
                _userContextCache[userId] = (userContext, DateTime.UtcNow);
                
                _logger.LogDebug("Created user context for user {userId} with {interestCount} interests", 
                    userId, userContext.Interests.Count);

                return userContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building user context for user {userId}", userId);
                
                // Return minimal user context on error
                return new UserContext
                {
                    UserId = userId,
                    RelationshipStrength = 0.1,
                    Metadata = new Dictionary<string, object> { { "Error", "Error building full user context" } }
                };
            }
        }

        /// <inheritdoc/>
        public async Task<EnvironmentContext> GetEnvironmentContextAsync(string channelId)
        {
            try
            {
                // Check if we have a cached environment context
                if (_environmentContextCache.TryGetValue(channelId, out var cachedContext))
                {
                    var refreshThreshold = DateTime.UtcNow.AddHours(-1); // Refresh environment context hourly
                    if (cachedContext.LastRefresh > refreshThreshold)
                    {
                        return cachedContext.Context;
                    }
                }

                // Create new environment context
                var envContext = new EnvironmentContext
                {
                    ChannelId = channelId,
                    LastActiveTime = DateTime.UtcNow
                };

                // In a real implementation, we would get channel/server details from Discord API
                // For now we'll use placeholder data
                envContext.ChannelName = $"channel-{channelId}";
                envContext.ChannelType = "text";
                envContext.UserCount = 10;
                
                // Cache the environment context
                _environmentContextCache[channelId] = (envContext, DateTime.UtcNow);
                
                return envContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building environment context for channel {channelId}", channelId);
                
                // Return minimal environment context on error
                return new EnvironmentContext
                {
                    ChannelId = channelId,
                    LastActiveTime = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { { "Error", "Error building full environment context" } }
                };
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeContext> GetKnowledgeContextAsync(string conversationId, string query = null)
        {
            try
            {
                var knowledgeContext = new KnowledgeContext();
                
                // Get conversation to extract topics and user IDs
                var conversation = await _conversationService.GetConversationAsync(conversationId);
                if (conversation == null || !conversation.Messages.Any())
                {
                    return knowledgeContext;
                }

                // Extract user IDs from conversation
                var userIds = conversation.Messages
                    .Select(m => m.AuthorId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                // Extract potential topics from recent messages
                var recentContent = string.Join(" ", conversation.Messages
                    .OrderByDescending(m => m.Timestamp)
                    .Take(_options.MaxRecentMessages)
                    .Select(m => m.Content));

                // A more sophisticated implementation would use NLP to extract topics
                // For simplicity, we'll simulate topic extraction from content
                var topics = ExtractTopicsFromContent(recentContent);

                // Get relevant memories for these users and topics
                var memories = await GetRelevantMemoriesAsync(userIds, topics, query);
                knowledgeContext.RelevantMemories = memories
                    .OrderByDescending(m => m.ContextRelevance * m.Importance)
                    .Take(_options.MaxRelevantMemories)
                    .ToList();
                
                // Get relevant facts about these topics
                var facts = await GetRelevantFactsAsync(topics, query);
                knowledgeContext.RelevantFacts = facts
                    .OrderByDescending(f => f.ContextRelevance * f.Confidence)
                    .Take(_options.MaxRelevantFacts)
                    .ToList();

                // Calculate overall importance score
                knowledgeContext.ImportanceScore = CalculateKnowledgeImportance(knowledgeContext);
                
                _logger.LogDebug("Built knowledge context with {memoryCount} memories and {factCount} facts for conversation {conversationId}",
                    knowledgeContext.RelevantMemories.Count, knowledgeContext.RelevantFacts.Count, conversationId);

                return knowledgeContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building knowledge context for conversation {conversationId}", conversationId);
                return new KnowledgeContext();
            }
        }

        /// <inheritdoc/>
        public async Task<int> PruneContextAsync(string conversationId)
        {
            try
            {
                if (!_contextCache.TryGetValue(conversationId, out var context))
                {
                    return 0;
                }

                int itemsPruned = 0;

                // Prune expired messages
                var expirationThreshold = DateTime.UtcNow.AddMinutes(-_options.MessageExpirationMinutes);
                var initialMessageCount = context.RecentMessages.Count;
                context.RecentMessages = context.RecentMessages
                    .Where(m => m.Timestamp > expirationThreshold)
                    .ToList();
                itemsPruned += initialMessageCount - context.RecentMessages.Count;

                // Prune least relevant knowledge if we have too many items
                if (context.Knowledge != null)
                {
                    // Prune memories keeping only the most relevant ones
                    var initialMemoryCount = context.Knowledge.RelevantMemories.Count;
                    if (initialMemoryCount > _options.MaxRelevantMemories)
                    {
                        context.Knowledge.RelevantMemories = context.Knowledge.RelevantMemories
                            .OrderByDescending(m => m.ContextRelevance * m.Importance)
                            .Take(_options.MaxRelevantMemories)
                            .ToList();
                        itemsPruned += initialMemoryCount - context.Knowledge.RelevantMemories.Count;
                    }

                    // Prune facts keeping only the most relevant ones
                    var initialFactCount = context.Knowledge.RelevantFacts.Count;
                    if (initialFactCount > _options.MaxRelevantFacts)
                    {
                        context.Knowledge.RelevantFacts = context.Knowledge.RelevantFacts
                            .OrderByDescending(f => f.ContextRelevance * f.Confidence)
                            .Take(_options.MaxRelevantFacts)
                            .ToList();
                        itemsPruned += initialFactCount - context.Knowledge.RelevantFacts.Count;
                    }
                }

                // Prune users keeping only the most relevant ones
                var initialUserCount = context.Users.Count;
                if (initialUserCount > _options.MaxUsersInContext)
                {
                    var priorityUsers = context.Users
                        .OrderByDescending(u => u.Value.RelationshipStrength)
                        .Take(_options.MaxUsersInContext)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    
                    context.Users = priorityUsers;
                    itemsPruned += initialUserCount - context.Users.Count;
                }

                _logger.LogDebug("Pruned {itemCount} items from context for conversation {conversationId}", 
                    itemsPruned, conversationId);

                return itemsPruned;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pruning context for conversation {conversationId}", conversationId);
                return 0;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Refreshes dynamic data in the context like emotional state
        /// </summary>
        private async Task RefreshDynamicContextDataAsync(ThinkingContext context)
        {
            // Update the bot's emotional state
            context.BotEmotionalState = await _emotionalService.GetCurrentEmotionalStateAsync();
            
            // Update timestamp
            context.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Extracts potential topics from message content
        /// </summary>
        /// <param name="content">The content to extract topics from</param>
        /// <returns>A list of potential topics</returns>
        private List<string> ExtractTopicsFromContent(string content)
        {
            // In a real implementation, this would use NLP or keyword extraction
            // For now, we'll use a simple approach with common words as topics
            var result = new List<string>();
            
            if (string.IsNullOrEmpty(content))
                return result;

            // Get existing interests from persona service to match against
            var interests = _personaService.GetInterestsAsync().Result;

            // Simple word-based topic extraction
            var words = content.Split(new[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ':', ';' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            var potentialTopics = words
                .Where(w => w.Length > 4)
                .Select(w => w.Trim().ToLowerInvariant())
                .Distinct()
                .Take(10)
                .ToList();

            // Add any matched interests as high-priority topics
            foreach (var interest in interests)
            {
                if (content.Contains(interest.Key, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(interest.Key);
                }
            }

            // Add other potential topics
            result.AddRange(potentialTopics.Where(t => !result.Contains(t)).Take(5));

            return result;
        }

        /// <summary>
        /// Gets relevant memories for users and topics
        /// </summary>
        private async Task<List<MemoryItem>> GetRelevantMemoriesAsync(
            List<string> userIds, List<string> topics, string query = null)
        {
            var result = new List<MemoryItem>();

            try
            {
                // In a real implementation, this would query the memory graph in KnowledgeService
                // For now, we'll simulate some relevant memories
                
                // Simulate retrieving memories - typically this would come from KnowledgeService
                if (userIds.Any())
                {
                    foreach (var userId in userIds)
                    {
                        // Add some example memories for each user
                        result.Add(new MemoryItem
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = $"User {userId} mentioned they like programming",
                            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10)),
                            Importance = 0.7,
                            ContextRelevance = topics.Contains("programming") ? 0.9 : 0.5,
                            AssociatedUserIds = new List<string> { userId }
                        });

                        result.Add(new MemoryItem
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = $"User {userId} shared their favorite movie",
                            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                            Importance = 0.6,
                            ContextRelevance = topics.Contains("movie") ? 0.9 : 0.4,
                            AssociatedUserIds = new List<string> { userId }
                        });
                    }
                }

                // Add topic-related memories
                foreach (var topic in topics)
                {
                    result.Add(new MemoryItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = $"There was a discussion about {topic} recently",
                        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 5)),
                        Importance = 0.7,
                        ContextRelevance = 0.8,
                        AssociatedUserIds = userIds.Take(Random.Shared.Next(1, userIds.Count + 1)).ToList()
                    });
                }

                // Filter by query if provided
                if (!string.IsNullOrEmpty(query))
                {
                    result = result
                        .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Filter by minimum relevance
                result = result
                    .Where(m => m.ContextRelevance >= _options.MinMemoryRelevance)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving relevant memories");
            }

            return result;
        }

        /// <summary>
        /// Gets relevant facts about topics
        /// </summary>
        private async Task<List<FactItem>> GetRelevantFactsAsync(List<string> topics, string query = null)
        {
            var result = new List<FactItem>();

            try
            {
                // In a real implementation, this would query the knowledge graph
                // For now, we'll simulate some relevant facts
                
                foreach (var topic in topics)
                {
                    // Add some example facts for each topic
                    result.Add(new FactItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Topic = topic,
                        Content = $"{topic} is a subject that users often discuss",
                        Confidence = 0.8,
                        LearnedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                        Source = "conversation",
                        ContextRelevance = 0.7
                    });

                    result.Add(new FactItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Topic = topic,
                        Content = $"The server has had {Random.Shared.Next(1, 10)} discussions about {topic}",
                        Confidence = 0.7,
                        LearnedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                        Source = "stats",
                        ContextRelevance = 0.6
                    });
                }

                // Filter by query if provided
                if (!string.IsNullOrEmpty(query))
                {
                    result = result
                        .Where(f => f.Content.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                   f.Topic.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Filter by minimum relevance
                result = result
                    .Where(f => f.ContextRelevance >= _options.MinFactRelevance)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving relevant facts");
            }

            return result;
        }

        /// <summary>
        /// Calculates the overall importance score for knowledge context
        /// </summary>
        private double CalculateKnowledgeImportance(KnowledgeContext knowledgeContext)
        {
            if (knowledgeContext == null)
                return 0.0;

            // Calculate weighted importance score based on memories and facts
            double memoryScore = knowledgeContext.RelevantMemories
                .Select(m => m.Importance * m.ContextRelevance)
                .DefaultIfEmpty(0)
                .Average();

            double factScore = knowledgeContext.RelevantFacts
                .Select(f => f.Confidence * f.ContextRelevance)
                .DefaultIfEmpty(0)
                .Average();

            // Weight memories slightly higher than facts (60/40 split)
            return (memoryScore * 0.6) + (factScore * 0.4);
        }

        #endregion
    }
}
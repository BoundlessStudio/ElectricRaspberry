using System.Collections.Concurrent;
using ElectricRaspberry.Models.Regulation;
using ElectricRaspberry.Services.Regulation.Configuration;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Services.Regulation;

/// <summary>
/// Service for managing the bot's self-regulation and engagement behaviors
/// </summary>
public class SelfRegulationService : ISelfRegulationService
{
    private readonly IStaminaService _staminaService;
    private readonly IEmotionalService _emotionalService;
    private readonly IConversationService _conversationService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IPersonalityService _personalityService;
    private readonly IPersonaService _personaService;
    private readonly ILogger<SelfRegulationService> _logger;
    private readonly SelfRegulationOptions _options;
    private readonly Random _random = new();
    
    private readonly ConcurrentDictionary<string, ChannelActivity> _channelActivities = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _botMessageTimestamps = new();
    private readonly ConcurrentDictionary<string, RelationshipStage> _relationshipStages = new();
    
    /// <summary>
    /// Creates a new instance of the self-regulation service
    /// </summary>
    /// <param name="staminaService">Stamina service</param>
    /// <param name="emotionalService">Emotional service</param>
    /// <param name="conversationService">Conversation service</param>
    /// <param name="knowledgeService">Knowledge service</param>
    /// <param name="personalityService">Personality service</param>
    /// <param name="personaService">Persona service</param>
    /// <param name="options">Self-regulation options</param>
    /// <param name="logger">Logger</param>
    public SelfRegulationService(
        IStaminaService staminaService,
        IEmotionalService emotionalService,
        IConversationService conversationService,
        IKnowledgeService knowledgeService,
        IPersonalityService personalityService,
        IPersonaService personaService,
        IOptions<SelfRegulationOptions> options,
        ILogger<SelfRegulationService> logger)
    {
        _staminaService = staminaService;
        _emotionalService = emotionalService;
        _conversationService = conversationService;
        _knowledgeService = knowledgeService;
        _personalityService = personalityService;
        _personaService = personaService;
        _logger = logger;
        _options = options.Value;
    }
    
    /// <summary>
    /// Determines whether the bot should engage in a conversation
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>True if the bot should engage, false otherwise</returns>
    public async Task<bool> ShouldEngageAsync(EngagementContext context)
    {
        // Check if the bot is sleeping
        if (await _staminaService.IsSleepingAsync())
        {
            return false;
        }
        
        // Always engage if mentioned
        if (context.WasRecentlyMentioned)
        {
            return true;
        }
        
        // Get the current activity for the channel
        var activity = GetOrCreateChannelActivity(context.ChannelId);
        
        // Check if we're already engaged and if the engagement is recent
        if (activity.IsEngaged && activity.EngagementStartTimestamp.HasValue)
        {
            var engagementDuration = DateTimeOffset.UtcNow - activity.EngagementStartTimestamp.Value;
            if (engagementDuration < TimeSpan.FromMinutes(30))
            {
                // Already engaged in this channel, continue engagement
                return true;
            }
        }
        
        // Calculate engagement probability
        var probability = await CalculateEngagementProbabilityAsync(context);
        
        // Apply randomness to make behavior less predictable
        var roll = _random.NextDouble();
        
        var shouldEngage = roll < probability;
        
        if (shouldEngage)
        {
            // Record engagement
            activity.IsEngaged = true;
            activity.EngagementStartTimestamp = DateTimeOffset.UtcNow;
            
            _logger.LogInformation(
                "Decided to engage in conversation in channel {ChannelId} with probability {Probability:P2}",
                context.ChannelId, probability);
        }
        else
        {
            _logger.LogDebug(
                "Decided not to engage in conversation in channel {ChannelId} with probability {Probability:P2}",
                context.ChannelId, probability);
        }
        
        return shouldEngage;
    }
    
    /// <summary>
    /// Gets the recommended delay before sending a message
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>The recommended delay timespan</returns>
    public async Task<TimeSpan> GetResponseDelayAsync(EngagementContext context)
    {
        // Factor in the channel's activity level
        double activityFactor = context.ActivityLevel switch
        {
            ActivityLevel.VeryHigh => 0.6, // Respond quicker in very active channels
            ActivityLevel.High => 0.8,
            ActivityLevel.Moderate => 1.0,
            ActivityLevel.Low => 1.2,
            ActivityLevel.Inactive => 1.5, // Respond slower in inactive channels
            _ => 1.0
        };
        
        // Factor in relationship with participants
        double relationshipFactor = context.AverageRelationshipStrength switch
        {
            >= 0.8 => 0.7, // Respond quicker to close friends
            >= 0.5 => 0.8, // Respond a bit quicker to friends
            >= 0.3 => 1.0, // Normal response time for acquaintances
            _ => 1.2 // Respond slower to strangers
        };
        
        // Factor in stamina level
        double staminaFactor = context.CurrentStamina switch
        {
            >= 80 => 0.9, // Respond quicker when energetic
            >= 50 => 1.0, // Normal response time
            >= 30 => 1.3, // Slower when tired
            _ => 1.5 // Very slow when extremely tired
        };
        
        // Factor in emotional state
        double emotionalFactor = 1.0;
        var emotionalState = context.CurrentEmotionalState;
        if (emotionalState != null)
        {
            // Respond quicker when excited or angry, slower when sad or calm
            if (emotionalState.GetEmotion(CoreEmotions.Joy) > 0.7 ||
                emotionalState.GetEmotion(CoreEmotions.Anger) > 0.7)
            {
                emotionalFactor = 0.8;
            }
            else if (emotionalState.GetEmotion(CoreEmotions.Sadness) > 0.7)
            {
                emotionalFactor = 1.3;
            }
        }
        
        // Calculate base delay in seconds
        var baseDelay = _options.MinResponseDelaySeconds;
        var delayRange = _options.MaxResponseDelaySeconds - _options.MinResponseDelaySeconds;
        
        // Apply personality adjustment
        var personalityTraits = await _personalityService.GetCurrentTraitsAsync();
        double personalityFactor = 1.0;
        
        if (personalityTraits.TryGetValue("Impulsive", out var impulsive) && impulsive > 0.5)
        {
            personalityFactor *= 0.8; // More impulsive means quicker responses
        }
        
        if (personalityTraits.TryGetValue("Thoughtful", out var thoughtful) && thoughtful > 0.5)
        {
            personalityFactor *= 1.2; // More thoughtful means slower, more deliberate responses
        }
        
        // Combine all factors
        var combinedFactor = activityFactor * relationshipFactor * staminaFactor * emotionalFactor * personalityFactor;
        
        // Calculate final delay
        var delaySeconds = baseDelay + (delayRange * combinedFactor * _random.NextDouble());
        
        return TimeSpan.FromSeconds(delaySeconds);
    }
    
    /// <summary>
    /// Gets the current activity level for a channel
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>The activity level</returns>
    public Task<ActivityLevel> GetChannelActivityLevelAsync(string channelId)
    {
        var activity = GetOrCreateChannelActivity(channelId);
        return Task.FromResult(activity.Level);
    }
    
    /// <summary>
    /// Updates the activity tracking for a channel
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <param name="messageCount">Number of new messages</param>
    /// <param name="interval">Time interval for these messages</param>
    /// <returns>The updated activity level</returns>
    public Task<ActivityLevel> UpdateChannelActivityAsync(string channelId, int messageCount, TimeSpan interval)
    {
        var activity = GetOrCreateChannelActivity(channelId);
        
        // Update last message timestamp
        activity.LastMessageTimestamp = DateTimeOffset.UtcNow;
        
        // Update message count
        activity.MessageCount += messageCount;
        
        // Calculate messages per minute for this update
        double messagesPerMinute = interval.TotalMinutes > 0 
            ? messageCount / interval.TotalMinutes 
            : 0;
        
        // Update average time between messages (exponential moving average)
        if (messageCount > 0 && interval.TotalSeconds > 0)
        {
            var avgSecondsBetweenMessages = interval.TotalSeconds / messageCount;
            const double alpha = 0.3; // Smoothing factor for the moving average
            activity.AverageTimeBetweenMessagesSeconds = 
                (alpha * avgSecondsBetweenMessages) + ((1 - alpha) * activity.AverageTimeBetweenMessagesSeconds);
        }
        
        // Determine activity level based on messages per minute
        activity.Level = DetermineActivityLevel(messagesPerMinute);
        
        _logger.LogDebug("Updated channel {ChannelId} activity to {ActivityLevel} ({MessagesPerMinute:F1} msgs/min)",
            channelId, activity.Level, messagesPerMinute);
        
        return Task.FromResult(activity.Level);
    }
    
    /// <summary>
    /// Records a bot message to a channel for throttling purposes
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <param name="messageId">The message ID</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task RecordBotMessageAsync(string channelId, string messageId)
    {
        var activity = GetOrCreateChannelActivity(channelId);
        
        // Update bot message timestamp
        activity.LastBotMessageTimestamp = DateTimeOffset.UtcNow;
        
        // Update bot message count
        activity.RecentBotMessageCount++;
        
        // Record message timestamp
        _botMessageTimestamps.TryAdd(messageId, DateTimeOffset.UtcNow);
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Calculates the probability that the bot should engage proactively
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>Probability between 0 and 1</returns>
    public async Task<double> CalculateEngagementProbabilityAsync(EngagementContext context)
    {
        // Start with base probability
        double probability = _options.BaseEngagementProbability;
        
        // Adjust based on stamina (tired = less likely to engage)
        double staminaFactor = context.CurrentStamina / 100.0;
        probability *= staminaFactor;
        
        // Adjust based on relationship strength
        double relationshipFactor = 0.5 + (context.AverageRelationshipStrength * 0.5);
        probability *= relationshipFactor;
        
        // Adjust based on topic relevance
        double topicFactor = 0.8 + (context.TopicRelevance * 0.4);
        probability *= topicFactor;
        
        // Adjust based on activity level
        double activityFactor = context.ActivityLevel switch
        {
            ActivityLevel.Inactive => 0.5,  // Less likely to engage in inactive channels
            ActivityLevel.Low => 0.8,       // Good time to engage
            ActivityLevel.Moderate => 1.0,  // Normal engagement
            ActivityLevel.High => 0.7,      // Less likely to engage in busy channels
            ActivityLevel.VeryHigh => 0.4,  // Much less likely to engage in very busy channels
            _ => 1.0
        };
        probability *= activityFactor;
        
        // Adjust based on time since last message
        if (context.TimeSinceLastMessage > TimeSpan.FromHours(1))
        {
            // Much less likely to engage in stale conversations
            probability *= 0.3;
        }
        else if (context.TimeSinceLastMessage > TimeSpan.FromMinutes(15))
        {
            // Less likely to engage in older conversations
            probability *= 0.7;
        }
        
        // Adjust based on personality traits
        var personalityTraits = await _personalityService.GetCurrentTraitsAsync();
        
        if (personalityTraits.TryGetValue("Extroverted", out var extroversion) && extroversion > 0.5)
        {
            probability *= 1.0 + ((extroversion - 0.5) * 0.6); // More extroverted = more likely to engage
        }
        
        if (personalityTraits.TryGetValue("Reserved", out var reserved) && reserved > 0.5)
        {
            probability *= 1.0 - ((reserved - 0.5) * 0.6); // More reserved = less likely to engage
        }
        
        // Apply conversation importance
        probability *= (0.7 + (context.ConversationImportance * 0.6));
        
        // Apply topic knowledge
        if (context.HasKnowledgeOfTopic)
        {
            probability *= 1.2; // More likely to engage in topics the bot knows about
        }
        
        // Apply some randomness
        probability *= 0.9 + (_random.NextDouble() * 0.2);
        
        // Cap at reasonable bounds
        probability = Math.Max(0.05, Math.Min(0.95, probability));
        
        return probability;
    }
    
    /// <summary>
    /// Gets the time until the bot should consider initiating a conversation
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>Time until next potential initiation</returns>
    public Task<TimeSpan> GetTimeUntilNextInitiationAsync(string channelId)
    {
        var activity = GetOrCreateChannelActivity(channelId);
        
        if (activity.NextInitiationTimestamp.HasValue)
        {
            var timeUntilNext = activity.NextInitiationTimestamp.Value - DateTimeOffset.UtcNow;
            
            // If time is negative, return zero (ready to initiate)
            if (timeUntilNext < TimeSpan.Zero)
            {
                return Task.FromResult(TimeSpan.Zero);
            }
            
            return Task.FromResult(timeUntilNext);
        }
        
        // No next initiation planned, schedule one
        var minDelay = TimeSpan.FromMinutes(_options.MinInitiationDelayMinutes);
        var maxDelay = TimeSpan.FromMinutes(_options.MaxInitiationDelayMinutes);
        var range = (maxDelay - minDelay).TotalMilliseconds;
        var delay = minDelay.Add(TimeSpan.FromMilliseconds(_random.NextDouble() * range));
        
        activity.NextInitiationTimestamp = DateTimeOffset.UtcNow.Add(delay);
        
        return Task.FromResult(delay);
    }
    
    /// <summary>
    /// Records that the bot initiated a conversation
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task RecordInitiationAsync(string channelId)
    {
        var activity = GetOrCreateChannelActivity(channelId);
        
        // Update initiation timestamp
        activity.LastInitiationTimestamp = DateTimeOffset.UtcNow;
        
        // Reset next initiation timestamp
        activity.NextInitiationTimestamp = null;
        
        // Set engagement status
        activity.IsEngaged = true;
        activity.EngagementStartTimestamp = DateTimeOffset.UtcNow;
        
        _logger.LogInformation("Initiated conversation in channel {ChannelId}", channelId);
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Builds the engagement context for a channel
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <param name="participantIds">IDs of users participating in the conversation</param>
    /// <returns>The built engagement context</returns>
    public async Task<EngagementContext> BuildEngagementContextAsync(string channelId, IEnumerable<string> participantIds)
    {
        var participantList = participantIds.ToList();
        var activity = GetOrCreateChannelActivity(channelId);
        
        // Get current stamina
        var stamina = await _staminaService.GetCurrentStaminaAsync();
        
        // Get current emotional state
        var emotionalState = await _emotionalService.GetCurrentEmotionalStateAsync();
        
        // Calculate relationship strength for each participant
        double totalStrength = 0;
        foreach (var participantId in participantList)
        {
            var relationship = await _knowledgeService.GetUserRelationshipAsync(participantId);
            if (relationship != null)
            {
                totalStrength += relationship.Strength;
                
                // Update relationship stage if needed
                UpdateRelationshipStage(participantId, relationship.Strength);
            }
        }
        
        // Calculate average relationship strength
        var averageStrength = participantList.Count > 0 ? totalStrength / participantList.Count : 0;
        
        // Get time since last bot message
        var timeSinceLastMessage = activity.LastBotMessageTimestamp.HasValue
            ? DateTimeOffset.UtcNow - activity.LastBotMessageTimestamp.Value
            : TimeSpan.FromDays(1); // Default to a long time if no previous message
        
        // Create the context
        var context = new EngagementContext
        {
            ChannelId = channelId,
            ActivityLevel = activity.Level,
            ParticipantIds = participantList,
            AverageRelationshipStrength = averageStrength,
            CurrentStamina = stamina,
            CurrentEmotionalState = emotionalState,
            TimeSinceLastMessage = timeSinceLastMessage,
            MessagesSinceLastBotMessage = activity.MessageCount,
            WasRecentlyMentioned = false, // This should be set by the caller if relevant
            ConversationImportance = 0.5, // Default importance
            TopicRelevance = 0.5 // Default relevance
        };
        
        return context;
    }
    
    /// <summary>
    /// Checks whether the bot should perform an idle behavior
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>True if an idle behavior should be performed</returns>
    public Task<bool> ShouldPerformIdleBehaviorAsync(string channelId)
    {
        var activity = GetOrCreateChannelActivity(channelId);
        
        // Check if enough time has passed since the last idle behavior
        if (activity.LastIdleBehaviorTimestamp.HasValue)
        {
            var timeSinceLastIdle = DateTimeOffset.UtcNow - activity.LastIdleBehaviorTimestamp.Value;
            var minIdleInterval = TimeSpan.FromMinutes(_options.IdleBehaviorIntervalMinutes);
            
            if (timeSinceLastIdle < minIdleInterval)
            {
                // Not enough time has passed
                return Task.FromResult(false);
            }
        }
        
        // Check if the channel is active enough
        if (activity.Level == ActivityLevel.Inactive)
        {
            // Don't perform idle behaviors in completely inactive channels
            return Task.FromResult(false);
        }
        
        // Check if we're already engaged in a conversation
        if (activity.IsEngaged && activity.EngagementStartTimestamp.HasValue)
        {
            var engagementDuration = DateTimeOffset.UtcNow - activity.EngagementStartTimestamp.Value;
            if (engagementDuration < TimeSpan.FromMinutes(30))
            {
                // Already engaged, no need for idle behavior
                return Task.FromResult(false);
            }
        }
        
        // Apply randomness to make behavior less predictable
        var probability = 0.3; // Base probability for idle behavior
        var roll = _random.NextDouble();
        
        var shouldPerform = roll < probability;
        
        if (shouldPerform)
        {
            // Record the idle behavior
            activity.LastIdleBehaviorTimestamp = DateTimeOffset.UtcNow;
        }
        
        return Task.FromResult(shouldPerform);
    }
    
    /// <summary>
    /// Gets the type of idle behavior to perform
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>The type of idle behavior to perform</returns>
    public Task<string> GetIdleBehaviorTypeAsync(EngagementContext context)
    {
        // List of possible idle behaviors
        var behaviors = new List<string>
        {
            IdleBehaviorType.EmojiReaction,
            IdleBehaviorType.StatusChange,
            IdleBehaviorType.ChannelObservation,
            IdleBehaviorType.OpenQuestion
        };
        
        // Add more engaging behaviors for channels with higher relationship strength
        if (context.AverageRelationshipStrength > 0.4)
        {
            behaviors.Add(IdleBehaviorType.InterestPrompt);
            behaviors.Add(IdleBehaviorType.RecallPreviousConversation);
        }
        
        // Add voice-related behaviors for active channels
        if (context.ActivityLevel >= ActivityLevel.Moderate)
        {
            behaviors.Add(IdleBehaviorType.VoicePresence);
        }
        
        // Weight the behaviors based on context
        var weightedBehaviors = new Dictionary<string, double>();
        
        foreach (var behavior in behaviors)
        {
            double weight = 1.0;
            
            switch (behavior)
            {
                case IdleBehaviorType.EmojiReaction:
                    // More likely in high activity channels
                    weight *= context.ActivityLevel >= ActivityLevel.High ? 2.0 : 1.0;
                    // Less likely when tired
                    weight *= context.CurrentStamina < 30 ? 1.5 : 1.0;
                    break;
                    
                case IdleBehaviorType.StatusChange:
                    // More likely when stamina is low
                    weight *= context.CurrentStamina < 50 ? 1.5 : 1.0;
                    break;
                    
                case IdleBehaviorType.InterestPrompt:
                    // More likely with good relationship strength
                    weight *= context.AverageRelationshipStrength > 0.6 ? 1.5 : 1.0;
                    // Less likely when tired
                    weight *= context.CurrentStamina < 30 ? 0.5 : 1.0;
                    break;
                    
                case IdleBehaviorType.ChannelObservation:
                    // More likely in low activity channels
                    weight *= context.ActivityLevel <= ActivityLevel.Low ? 1.5 : 1.0;
                    break;
                    
                case IdleBehaviorType.OpenQuestion:
                    // More likely in moderate activity channels
                    weight *= context.ActivityLevel == ActivityLevel.Moderate ? 1.5 : 1.0;
                    // More likely with good relationship strength
                    weight *= context.AverageRelationshipStrength > 0.5 ? 1.3 : 1.0;
                    break;
                    
                case IdleBehaviorType.RecallPreviousConversation:
                    // Only likely with good relationship strength
                    weight *= context.AverageRelationshipStrength > 0.7 ? 1.5 : 0.5;
                    break;
                    
                case IdleBehaviorType.VoicePresence:
                    // More likely in high activity channels
                    weight *= context.ActivityLevel >= ActivityLevel.High ? 1.5 : 0.8;
                    // Less likely when tired
                    weight *= context.CurrentStamina < 40 ? 0.5 : 1.0;
                    break;
            }
            
            weightedBehaviors[behavior] = weight;
        }
        
        // Normalize weights to probabilities
        var totalWeight = weightedBehaviors.Values.Sum();
        var cumulativeWeight = 0.0;
        var roll = _random.NextDouble() * totalWeight;
        
        foreach (var (behavior, weight) in weightedBehaviors)
        {
            cumulativeWeight += weight;
            if (roll <= cumulativeWeight)
            {
                return Task.FromResult(behavior);
            }
        }
        
        // Default to emoji reaction if something goes wrong
        return Task.FromResult(IdleBehaviorType.EmojiReaction);
    }
    
    /// <summary>
    /// Gets or creates a channel activity tracking object
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <returns>The channel activity object</returns>
    private ChannelActivity GetOrCreateChannelActivity(string channelId)
    {
        return _channelActivities.GetOrAdd(channelId, id => new ChannelActivity
        {
            ChannelId = id,
            LastMessageTimestamp = DateTimeOffset.UtcNow
        });
    }
    
    /// <summary>
    /// Determines the activity level based on messages per minute
    /// </summary>
    /// <param name="messagesPerMinute">Number of messages per minute</param>
    /// <returns>The activity level</returns>
    private ActivityLevel DetermineActivityLevel(double messagesPerMinute)
    {
        if (messagesPerMinute >= _options.HighActivityThreshold * 1.5)
        {
            return ActivityLevel.VeryHigh;
        }
        
        if (messagesPerMinute >= _options.HighActivityThreshold)
        {
            return ActivityLevel.High;
        }
        
        if (messagesPerMinute >= _options.ModerateActivityThreshold)
        {
            return ActivityLevel.Moderate;
        }
        
        if (messagesPerMinute >= _options.LowActivityThreshold)
        {
            return ActivityLevel.Low;
        }
        
        return ActivityLevel.Inactive;
    }
    
    /// <summary>
    /// Updates the relationship stage for a user based on relationship strength
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="strength">The relationship strength</param>
    private void UpdateRelationshipStage(string userId, double strength)
    {
        // Get current stage
        var currentStage = _relationshipStages.GetOrAdd(userId, _ => RelationshipStage.Stranger);
        
        // Get threshold values
        var thresholds = _options.RelationshipStageThresholds;
        
        // Determine new stage
        RelationshipStage newStage = currentStage;
        
        if (strength >= thresholds.GetValueOrDefault("CloseFriend", 0.9))
        {
            newStage = RelationshipStage.CloseFriend;
        }
        else if (strength >= thresholds.GetValueOrDefault("Friend", 0.7))
        {
            newStage = RelationshipStage.Friend;
        }
        else if (strength >= thresholds.GetValueOrDefault("Casual", 0.4))
        {
            newStage = RelationshipStage.Casual;
        }
        else if (strength >= thresholds.GetValueOrDefault("Acquaintance", 0.2))
        {
            newStage = RelationshipStage.Acquaintance;
        }
        else
        {
            newStage = RelationshipStage.Stranger;
        }
        
        // Update stage if changed
        if (newStage != currentStage)
        {
            _relationshipStages[userId] = newStage;
            
            _logger.LogInformation(
                "Relationship with user {UserId} progressed from {OldStage} to {NewStage} (strength: {Strength:P2})",
                userId, currentStage, newStage, strength);
        }
    }
}
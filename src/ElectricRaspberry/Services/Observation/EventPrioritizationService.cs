using System.Collections.Concurrent;
using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Observation;
using ElectricRaspberry.Services.Observation.Configuration;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Service for prioritizing events from different channels
/// </summary>
public class EventPrioritizationService : IEventPrioritizationService
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly IChannelBufferManager _channelBufferManager;
    private readonly ILogger<EventPrioritizationService> _logger;
    private readonly EventPrioritizationOptions _options;
    
    /// <summary>
    /// Creates a new instance of the event prioritization service
    /// </summary>
    /// <param name="knowledgeService">Knowledge service for relationship information</param>
    /// <param name="channelBufferManager">Channel buffer manager</param>
    /// <param name="options">Prioritization options</param>
    /// <param name="logger">Logger</param>
    public EventPrioritizationService(
        IKnowledgeService knowledgeService,
        IChannelBufferManager channelBufferManager,
        IOptions<EventPrioritizationOptions> options,
        ILogger<EventPrioritizationService> logger)
    {
        _knowledgeService = knowledgeService;
        _channelBufferManager = channelBufferManager;
        _logger = logger;
        _options = options.Value;
    }
    
    /// <summary>
    /// Prioritizes a single message event
    /// </summary>
    /// <param name="messageEvent">The message event to prioritize</param>
    /// <param name="channelId">The channel ID where the event originated</param>
    /// <param name="botUserId">The bot's user ID</param>
    /// <returns>A prioritized event with assigned priority level</returns>
    public async Task<PrioritizedEvent> PrioritizeEventAsync(MessageEvent messageEvent, string channelId, string botUserId)
    {
        // Start with normal priority
        var priority = EventPriority.Normal;
        
        // Check if this is a direct mention of the bot
        if (messageEvent.IsMention && messageEvent.MentionedUserIds.Contains(botUserId))
        {
            priority = EventPriority.Critical;
            _logger.LogInformation("Prioritized direct mention from {UserId} as {Priority}", 
                messageEvent.AuthorId, priority);
        }
        // Check if this is a direct message to the bot
        else if (messageEvent.IsDirectMessage)
        {
            priority = EventPriority.Critical;
            _logger.LogInformation("Prioritized direct message from {UserId} as {Priority}", 
                messageEvent.AuthorId, priority);
        }
        // Check user relationship if not already critical priority
        else
        {
            var relationship = await _knowledgeService.GetUserRelationshipAsync(messageEvent.AuthorId);
            if (relationship != null)
            {
                // Prioritize based on relationship strength
                if (relationship.Strength >= _options.HighPriorityRelationshipThreshold)
                {
                    priority = EventPriority.High;
                    _logger.LogDebug("Prioritized message from {UserId} as {Priority} based on relationship strength {Strength}", 
                        messageEvent.AuthorId, priority, relationship.Strength);
                }
            }
        }
        
        return new PrioritizedEvent(messageEvent, priority, channelId);
    }
    
    /// <summary>
    /// Prioritizes all events in a specific channel buffer
    /// </summary>
    /// <param name="channelId">The channel ID to prioritize events from</param>
    /// <param name="botUserId">The bot's user ID</param>
    /// <returns>A collection of prioritized events</returns>
    public async Task<ICollection<PrioritizedEvent>> PrioritizeChannelEventsAsync(string channelId, string botUserId)
    {
        var buffer = _channelBufferManager.GetBuffer(channelId);
        var events = await buffer.PeekEventsAsync();
        
        var prioritizedEvents = new List<PrioritizedEvent>(events.Count);
        foreach (var messageEvent in events)
        {
            var prioritizedEvent = await PrioritizeEventAsync(messageEvent, channelId, botUserId);
            prioritizedEvents.Add(prioritizedEvent);
        }
        
        // Sort events by priority
        prioritizedEvents.Sort();
        return prioritizedEvents;
    }
    
    /// <summary>
    /// Prioritizes events across all active channel buffers
    /// </summary>
    /// <param name="botUserId">The bot's user ID</param>
    /// <returns>A collection of prioritized events from all channels</returns>
    public async Task<ICollection<PrioritizedEvent>> PrioritizeAllEventsAsync(string botUserId)
    {
        var buffers = _channelBufferManager.GetAllBuffers();
        var allPrioritizedEvents = new List<PrioritizedEvent>();
        
        foreach (var buffer in buffers)
        {
            var channelEvents = await buffer.PeekEventsAsync();
            if (channelEvents.Count == 0) continue;
            
            foreach (var messageEvent in channelEvents)
            {
                var prioritizedEvent = await PrioritizeEventAsync(messageEvent, buffer.ChannelId, botUserId);
                allPrioritizedEvents.Add(prioritizedEvent);
            }
        }
        
        // Sort events by priority
        allPrioritizedEvents.Sort();
        return allPrioritizedEvents;
    }
}
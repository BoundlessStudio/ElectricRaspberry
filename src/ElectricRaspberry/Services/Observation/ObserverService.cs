using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Observation;
using ElectricRaspberry.Services.Observation.Configuration;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Service for managing concurrent observation of multiple channels
/// </summary>
public class ObserverService : IObserverService
{
    private readonly IChannelBufferManager _channelBufferManager;
    private readonly IEventPrioritizationService _prioritizationService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IConcurrencyManager _concurrencyManager;
    private readonly IConversationService _conversationService;
    private readonly IStaminaService _staminaService;
    private readonly ICatchupService _catchupService;
    private readonly ILogger<ObserverService> _logger;
    private readonly ObserverOptions _options;
    private readonly string _botUserId;
    
    /// <summary>
    /// Creates a new instance of the observer service
    /// </summary>
    /// <param name="channelBufferManager">Channel buffer manager</param>
    /// <param name="prioritizationService">Event prioritization service</param>
    /// <param name="rateLimitingService">Rate limiting service</param>
    /// <param name="concurrencyManager">Concurrency manager</param>
    /// <param name="conversationService">Conversation service</param>
    /// <param name="staminaService">Stamina service</param>
    /// <param name="catchupService">Catchup service</param>
    /// <param name="options">Observer options</param>
    /// <param name="logger">Logger</param>
    public ObserverService(
        IChannelBufferManager channelBufferManager,
        IEventPrioritizationService prioritizationService,
        IRateLimitingService rateLimitingService,
        IConcurrencyManager concurrencyManager,
        IConversationService conversationService,
        IStaminaService staminaService,
        ICatchupService catchupService,
        IOptions<ObserverOptions> options,
        ILogger<ObserverService> logger)
    {
        _channelBufferManager = channelBufferManager;
        _prioritizationService = prioritizationService;
        _rateLimitingService = rateLimitingService;
        _concurrencyManager = concurrencyManager;
        _conversationService = conversationService;
        _staminaService = staminaService;
        _catchupService = catchupService;
        _logger = logger;
        _options = options.Value;
        _botUserId = _options.BotUserId;
    }
    
    /// <summary>
    /// Processes a new message event
    /// </summary>
    /// <param name="messageEvent">The message event</param>
    /// <param name="channelId">The channel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ProcessMessageEventAsync(MessageEvent messageEvent, string channelId, CancellationToken cancellationToken = default)
    {
        // Check if the bot is sleeping
        var isSleeping = await _staminaService.IsSleepingAsync();
        
        // If the bot is sleeping, add the message to the catchup queue unless it's a direct mention or DM
        if (isSleeping && !(messageEvent.MentionsBot || messageEvent.IsMentioned(Convert.ToUInt64(_botUserId))) && !messageEvent.IsDirectMessage)
        {
            await _catchupService.AddToCatchupQueueAsync(new CatchupQueueItem(messageEvent, channelId));
            _logger.LogDebug("Added message from {UserId} in channel {ChannelId} to catchup queue while sleeping", 
                messageEvent.AuthorId, channelId);
            return;
        }
        
        // Add the event to the channel buffer
        await _channelBufferManager.AddEventAsync(channelId, messageEvent);
        
        // Prioritize the event
        var prioritizedEvent = await _prioritizationService.PrioritizeEventAsync(messageEvent, channelId, _botUserId);
        
        // Check rate limits before proceeding
        var operationKey = "ProcessMessage";
        if (!_rateLimitingService.CanPerformOperation(operationKey, channelId))
        {
            var timeToWait = _rateLimitingService.GetTimeToWait(operationKey, channelId);
            _logger.LogDebug("Rate limited for channel {ChannelId}, waiting {WaitTime}ms", channelId, timeToWait.TotalMilliseconds);
            
            // If this is a critical priority event, process it anyway despite rate limits
            if (prioritizedEvent.Priority != EventPriority.Critical)
            {
                return;
            }
            
            _logger.LogInformation("Processing critical event despite rate limit: {MessageId}", messageEvent.MessageId);
        }
        
        // Record the operation to update rate limiting
        _rateLimitingService.RecordOperation(operationKey, channelId);
        
        // Lock the channel for processing to maintain consistent context
        var resourceKey = $"channel:{channelId}";
        await _concurrencyManager.ExecuteWithResourceLockAsync(resourceKey, async () =>
        {
            // Process the event with the conversation service
            await _conversationService.ProcessMessageAsync(
                messageEvent, 
                channelId, 
                prioritizedEvent.Priority >= EventPriority.High);
            
            _logger.LogDebug("Processed message {MessageId} from {UserId} in channel {ChannelId} with priority {Priority}", 
                messageEvent.MessageId, messageEvent.AuthorId, channelId, prioritizedEvent.Priority);
        }, cancellationToken);
    }
    
    /// <summary>
    /// Processes multiple message events from different channels based on priority
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ProcessPrioritizedEventsAsync(CancellationToken cancellationToken = default)
    {
        // Prioritize events across all channels
        var prioritizedEvents = await _prioritizationService.PrioritizeAllEventsAsync(_botUserId);
        
        // Take only a batch of events to process
        var eventsToProcess = prioritizedEvents.Take(_options.MaxEventsPerBatch).ToList();
        
        if (eventsToProcess.Count == 0)
        {
            return;
        }
        
        _logger.LogInformation("Processing batch of {Count} prioritized events", eventsToProcess.Count);
        
        // Process each event in order of priority
        foreach (var prioritizedEvent in eventsToProcess)
        {
            // Skip processing if we're over the rate limit, except for critical events
            var operationKey = "ProcessBatchedMessage";
            if (!_rateLimitingService.CanPerformOperation(operationKey, prioritizedEvent.ChannelId) && 
                prioritizedEvent.Priority != EventPriority.Critical)
            {
                continue;
            }
            
            // Record the operation
            _rateLimitingService.RecordOperation(operationKey, prioritizedEvent.ChannelId);
            
            // Lock the channel for processing
            var resourceKey = $"channel:{prioritizedEvent.ChannelId}";
            await _concurrencyManager.ExecuteWithResourceLockAsync(resourceKey, async () =>
            {
                await _conversationService.ProcessMessageAsync(
                    prioritizedEvent.MessageEvent, 
                    prioritizedEvent.ChannelId, 
                    prioritizedEvent.Priority >= EventPriority.High);
                
                _logger.LogDebug("Processed batched message {MessageId} from {UserId} in channel {ChannelId} with priority {Priority}", 
                    prioritizedEvent.MessageEvent.MessageId, prioritizedEvent.MessageEvent.AuthorId, 
                    prioritizedEvent.ChannelId, prioritizedEvent.Priority);
            }, cancellationToken);
            
            // Check if we should apply a brief delay between processing events
            if (_options.DelayBetweenEventProcessingMs > 0)
            {
                await Task.Delay(_options.DelayBetweenEventProcessingMs, cancellationToken);
            }
        }
    }
    
    /// <summary>
    /// Performs maintenance on observation components
    /// </summary>
    public void PerformMaintenance()
    {
        // Clean up stale resources in the concurrency manager
        _concurrencyManager.CleanupStaleResources();
        
        // Clean up inactive buffers
        var inactiveThreshold = DateTimeOffset.UtcNow.AddMinutes(-_options.InactiveBufferTimeoutMinutes);
        var inactiveBuffers = _channelBufferManager.GetInactiveBuffers(inactiveThreshold);
        
        foreach (var buffer in inactiveBuffers)
        {
            _channelBufferManager.RemoveBuffer(buffer.ChannelId);
            _logger.LogInformation("Removed inactive buffer for channel {ChannelId}", buffer.ChannelId);
        }
    }
}
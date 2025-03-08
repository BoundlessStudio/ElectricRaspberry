using ElectricRaspberry.Models.Conversation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ElectricRaspberry.Services;

public class CatchupService : ICatchupService
{
    private readonly ILogger<CatchupService> _logger;
    private readonly IConversationService _conversationService;
    private readonly SemaphoreSlim _queueLock = new(1, 1);
    private readonly ConcurrentDictionary<ulong, CatchupQueueItem> _catchupQueue = new();
    
    // Maximum size of the catchup queue to prevent memory issues
    private readonly int _maxQueueSize = 1000;
    
    public CatchupService(
        ILogger<CatchupService> logger,
        IConversationService conversationService)
    {
        _logger = logger;
        _conversationService = conversationService;
    }
    
    public async Task AddToCatchupQueueAsync(MessageEvent messageEvent)
    {
        await _queueLock.WaitAsync();
        try
        {
            // Calculate priority based on message characteristics
            int priority = CalculateMessagePriority(messageEvent);
            
            // Add to queue with assigned priority
            var queueItem = new CatchupQueueItem(messageEvent, priority);
            _catchupQueue[messageEvent.Message.Id] = queueItem;
            
            _logger.LogDebug("Added message {MessageId} to catchup queue with priority {Priority}", 
                messageEvent.Message.Id, priority);
            
            // If queue is getting too large, remove oldest low-priority items
            if (_catchupQueue.Count > _maxQueueSize)
            {
                await TrimQueueAsync();
            }
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task<IEnumerable<MessageEvent>> GetCatchupQueueAsync(int count = 100, ulong? channelId = null)
    {
        await _queueLock.WaitAsync();
        try
        {
            var query = _catchupQueue.Values
                .Where(i => !i.IsProcessed)
                .OrderByDescending(i => i.Priority)
                .ThenBy(i => i.QueuedAt);
            
            // Apply channel filter if specified
            if (channelId.HasValue)
            {
                query = query.Where(i => i.MessageEvent.Channel.Id == channelId.Value);
            }
            
            return query.Take(count).Select(i => i.MessageEvent).ToList();
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task<int> ProcessCatchupQueueAsync(int maxBatchSize = 50)
    {
        await _queueLock.WaitAsync();
        try
        {
            // Get unprocessed items, ordered by priority
            var itemsToProcess = _catchupQueue.Values
                .Where(i => !i.IsProcessed)
                .OrderByDescending(i => i.Priority)
                .ThenBy(i => i.QueuedAt)
                .Take(maxBatchSize)
                .ToList();
            
            _logger.LogInformation("Processing {Count} items from catchup queue", itemsToProcess.Count);
            
            int processedCount = 0;
            
            // Process each item
            foreach (var item in itemsToProcess)
            {
                try
                {
                    // Add to conversation service
                    await _conversationService.ProcessMessageAsync(item.MessageEvent);
                    
                    // Mark as processed
                    item.IsProcessed = true;
                    processedCount++;
                    
                    _logger.LogDebug("Processed catchup message {MessageId}", item.MessageEvent.Message.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing catchup message {MessageId}", 
                        item.MessageEvent.Message.Id);
                }
            }
            
            return processedCount;
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task<string> GetMissedActivitySummaryAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            // Count messages by channel
            var channelCounts = _catchupQueue.Values
                .Where(i => !i.IsProcessed)
                .GroupBy(i => i.MessageEvent.Channel.Id)
                .Select(g => new
                {
                    ChannelId = g.Key,
                    ChannelName = GetChannelName(g.First().MessageEvent),
                    MessageCount = g.Count(),
                    MentionCount = g.Count(i => i.MessageEvent.MentionsBot),
                    DirectMessageCount = g.Count(i => i.MessageEvent.IsDirectMessage)
                })
                .OrderByDescending(x => x.MessageCount)
                .ToList();
            
            // Count messages by user
            var userCounts = _catchupQueue.Values
                .Where(i => !i.IsProcessed)
                .GroupBy(i => i.MessageEvent.Message.Author.Id)
                .Select(g => new
                {
                    UserId = g.Key,
                    UserName = g.First().MessageEvent.Message.Author.Username,
                    MessageCount = g.Count()
                })
                .OrderByDescending(x => x.MessageCount)
                .Take(5)
                .ToList();
            
            // Build summary
            var summary = new System.Text.StringBuilder();
            
            int totalMessages = _catchupQueue.Values.Count(i => !i.IsProcessed);
            int totalMentions = _catchupQueue.Values.Count(i => !i.IsProcessed && i.MessageEvent.MentionsBot);
            int totalDMs = _catchupQueue.Values.Count(i => !i.IsProcessed && i.MessageEvent.IsDirectMessage);
            
            summary.AppendLine($"While sleeping, I missed {totalMessages} messages");
            if (totalMentions > 0)
            {
                summary.AppendLine($"- {totalMentions} mentioned me directly");
            }
            if (totalDMs > 0)
            {
                summary.AppendLine($"- {totalDMs} were direct messages");
            }
            
            // Add channel breakdown
            if (channelCounts.Any())
            {
                summary.AppendLine("\nChannel Activity:");
                foreach (var channel in channelCounts.Take(5))
                {
                    summary.AppendLine($"- {channel.ChannelName}: {channel.MessageCount} messages");
                }
                
                if (channelCounts.Count > 5)
                {
                    summary.AppendLine($"- And {channelCounts.Count - 5} other channels");
                }
            }
            
            // Add user breakdown
            if (userCounts.Any())
            {
                summary.AppendLine("\nMost Active Users:");
                foreach (var user in userCounts)
                {
                    summary.AppendLine($"- {user.UserName}: {user.MessageCount} messages");
                }
            }
            
            return summary.ToString();
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task<bool> MarkAsProcessedAsync(ulong messageId)
    {
        await _queueLock.WaitAsync();
        try
        {
            if (_catchupQueue.TryGetValue(messageId, out var item))
            {
                item.IsProcessed = true;
                return true;
            }
            
            return false;
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task<int> ClearCatchupQueueAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            int count = _catchupQueue.Count;
            _catchupQueue.Clear();
            
            _logger.LogInformation("Cleared catchup queue with {Count} items", count);
            
            return count;
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task PrioritizeCatchupQueueAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            // Recalculate priorities for all unprocessed items
            foreach (var item in _catchupQueue.Values.Where(i => !i.IsProcessed))
            {
                item.Priority = CalculateMessagePriority(item.MessageEvent);
            }
            
            _logger.LogInformation("Reprioritized catchup queue with {Count} items", 
                _catchupQueue.Count(kv => !kv.Value.IsProcessed));
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    // Helper methods
    private int CalculateMessagePriority(MessageEvent messageEvent)
    {
        int priority = 0;
        
        // Direct messages have higher priority
        if (messageEvent.IsDirectMessage)
        {
            priority += 50;
        }
        
        // Messages that mention the bot have higher priority
        if (messageEvent.MentionsBot)
        {
            priority += 100;
        }
        
        // Messages from users with relationships have higher priority
        // TODO: When relationship service is implemented, add relationship strength factor
        
        // Newer messages have slightly higher priority
        int ageInMinutes = (int)(DateTime.UtcNow - messageEvent.Timestamp).TotalMinutes;
        priority -= Math.Min(ageInMinutes, 60); // Cap at -60 to prevent very old messages from having extremely low priority
        
        return priority;
    }
    
    private async Task TrimQueueAsync()
    {
        // Remove 25% of the oldest, already processed, or lowest-priority items
        int itemsToRemove = _catchupQueue.Count - (_maxQueueSize * 3 / 4);
        
        if (itemsToRemove <= 0)
        {
            return;
        }
        
        // First remove already processed items
        var processedItems = _catchupQueue.Where(kv => kv.Value.IsProcessed)
            .Select(kv => kv.Key)
            .Take(itemsToRemove)
            .ToList();
        
        foreach (var key in processedItems)
        {
            _catchupQueue.TryRemove(key, out _);
        }
        
        itemsToRemove -= processedItems.Count;
        
        // If we still need to remove more, take lowest priority items
        if (itemsToRemove > 0)
        {
            var lowPriorityItems = _catchupQueue
                .OrderBy(kv => kv.Value.Priority)
                .ThenBy(kv => kv.Value.QueuedAt)
                .Take(itemsToRemove)
                .Select(kv => kv.Key)
                .ToList();
            
            foreach (var key in lowPriorityItems)
            {
                _catchupQueue.TryRemove(key, out _);
            }
        }
        
        _logger.LogInformation("Trimmed catchup queue. Removed {RemovedCount} items.", 
            processedItems.Count + itemsToRemove);
    }
    
    private string GetChannelName(MessageEvent messageEvent)
    {
        if (messageEvent.IsDirectMessage)
        {
            return $"DM with {messageEvent.Message.Author.Username}";
        }
        else if (messageEvent.Channel is Discord.ITextChannel textChannel)
        {
            return $"#{textChannel.Name}";
        }
        else
        {
            return $"Channel {messageEvent.Channel.Id}";
        }
    }
}
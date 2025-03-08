using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Observation;

namespace ElectricRaspberry.Services;

/// <summary>
/// Handles the processing of messages in the catchup queue
/// </summary>
public class CatchupService : ICatchupService
{
    private readonly SemaphoreSlim _queueLock = new(1, 1);
    private readonly Dictionary<string, CatchupQueueItem> _catchupQueue = new();
    private readonly ILogger<CatchupService> _logger;
    private readonly int _maxQueueSize;
    
    /// <summary>
    /// Creates a new catchup service
    /// </summary>
    public CatchupService(ILogger<CatchupService> logger, IConfiguration config)
    {
        _logger = logger;
        _maxQueueSize = config.GetValue<int>("Catchup:MaxQueueSize", 500);
    }
    
    public async Task AddToCatchupQueueAsync(CatchupQueueItem queueItem)
    {
        if (queueItem == null)
        {
            throw new ArgumentNullException(nameof(queueItem));
        }
        
        if (queueItem.MessageEvent == null)
        {
            throw new ArgumentException("QueueItem must have a valid MessageEvent", nameof(queueItem));
        }
        
        await _queueLock.WaitAsync();
        try
        {
            // Use message ID as key to avoid duplicates
            var key = queueItem.MessageEvent.MessageId.ToString();
            _catchupQueue[key] = queueItem;
            
            _logger.LogDebug("Added message {MessageId} to catchup queue with priority {Priority}",
                queueItem.MessageEvent.MessageId, queueItem.Priority);
            
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
    
    public async Task<List<MessageEvent>> GetCatchupQueueAsync(int count = 100, ulong? channelId = null)
    {
        await _queueLock.WaitAsync();
        try
        {
            IEnumerable<CatchupQueueItem> query = _catchupQueue.Values
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
            
            // Mark all as processed (we're not actually processing here, just marking)
            foreach (var item in itemsToProcess)
            {
                item.IsProcessed = true;
                item.ProcessedAt = DateTime.UtcNow;
            }
            
            return itemsToProcess.Count;
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task ClearCatchupQueueAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            _catchupQueue.Clear();
            _logger.LogInformation("Cleared catchup queue");
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task<int> GetQueueSizeAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            return _catchupQueue.Count;
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    public async Task<int> GetUnprocessedQueueSizeAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            return _catchupQueue.Values.Count(i => !i.IsProcessed);
        }
        finally
        {
            _queueLock.Release();
        }
    }
    
    private async Task TrimQueueAsync()
    {
        // Already under lock from caller
        
        // Remove oldest processed items first
        var processedItems = _catchupQueue.Values
            .Where(i => i.IsProcessed)
            .OrderBy(i => i.ProcessedAt)
            .Take(_catchupQueue.Count - _maxQueueSize + 100)
            .ToList();
        
        foreach (var item in processedItems)
        {
            _catchupQueue.Remove(item.MessageEvent.MessageId.ToString());
        }
        
        int count = processedItems.Count;
        if (count > 0)
        {
            _logger.LogInformation("Removed {Count} processed items from catchup queue", count);
        }
        
        // If we still need to trim, remove oldest low-priority unprocessed items
        if (_catchupQueue.Count > _maxQueueSize)
        {
            var lowPriorityItems = _catchupQueue.Values
                .Where(i => !i.IsProcessed)
                .OrderBy(i => i.Priority)
                .ThenBy(i => i.QueuedAt)
                .Take(_catchupQueue.Count - _maxQueueSize)
                .ToList();
            
            foreach (var item in lowPriorityItems)
            {
                _catchupQueue.Remove(item.MessageEvent.MessageId.ToString());
            }
            
            _logger.LogInformation("Removed {Count} low-priority unprocessed items from catchup queue", lowPriorityItems.Count);
        }
    }
    
    // Additional method to support testing
    public async Task<Dictionary<string, CatchupQueueItem>> GetRawQueueAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            // Return a copy to avoid exposing the internal collection
            return new Dictionary<string, CatchupQueueItem>(_catchupQueue);
        }
        finally
        {
            _queueLock.Release();
        }
    }
}
using System.Collections.Concurrent;
using System.Threading;

namespace ElectricRaspberry.Models.Conversation;

/// <summary>
/// Represents a buffer for channel events with concurrent access support
/// </summary>
public class ChannelBuffer
{
    private readonly ConcurrentQueue<MessageEvent> _eventQueue = new();
    private readonly int _maxCapacity;
    private readonly SemaphoreSlim _bufferLock = new(1, 1);
    
    /// <summary>
    /// Gets the unique identifier for this channel buffer
    /// </summary>
    public string ChannelId { get; }
    
    /// <summary>
    /// Gets the number of events currently in the buffer
    /// </summary>
    public int Count => _eventQueue.Count;
    
    /// <summary>
    /// Gets the timestamp of the most recent event in the buffer
    /// </summary>
    public DateTimeOffset? LastEventTimestamp { get; private set; }

    /// <summary>
    /// Creates a new instance of the channel buffer
    /// </summary>
    /// <param name="channelId">The channel identifier</param>
    /// <param name="maxCapacity">Maximum number of events to store in the buffer (default: 100)</param>
    public ChannelBuffer(string channelId, int maxCapacity = 100)
    {
        ChannelId = channelId;
        _maxCapacity = maxCapacity;
    }

    /// <summary>
    /// Adds a message event to the buffer, removing oldest events if buffer is at capacity
    /// </summary>
    /// <param name="messageEvent">The message event to add</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task AddEventAsync(MessageEvent messageEvent)
    {
        await _bufferLock.WaitAsync();
        try
        {
            // Remove oldest events if we're at capacity
            while (_eventQueue.Count >= _maxCapacity)
            {
                _eventQueue.TryDequeue(out _);
            }
            
            _eventQueue.Enqueue(messageEvent);
            LastEventTimestamp = messageEvent.Timestamp;
        }
        finally
        {
            _bufferLock.Release();
        }
    }

    /// <summary>
    /// Gets all events in the buffer without removing them
    /// </summary>
    /// <returns>A read-only collection of message events</returns>
    public async Task<IReadOnlyCollection<MessageEvent>> PeekEventsAsync()
    {
        await _bufferLock.WaitAsync();
        try
        {
            return _eventQueue.ToArray();
        }
        finally
        {
            _bufferLock.Release();
        }
    }

    /// <summary>
    /// Gets all events from the buffer and clears it
    /// </summary>
    /// <returns>A collection of message events that were in the buffer</returns>
    public async Task<ICollection<MessageEvent>> DrainEventsAsync()
    {
        await _bufferLock.WaitAsync();
        try
        {
            var events = _eventQueue.ToArray();
            _eventQueue.Clear();
            return events;
        }
        finally
        {
            _bufferLock.Release();
        }
    }
}
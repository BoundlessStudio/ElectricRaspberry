using System.Collections.Concurrent;
using ElectricRaspberry.Models.Conversation;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Manages multiple channel buffers for concurrent observation across different channels
/// </summary>
public class ChannelBufferManager : IChannelBufferManager
{
    private readonly ConcurrentDictionary<string, ChannelBuffer> _channelBuffers = new();
    private readonly int _defaultBufferCapacity;
    
    /// <summary>
    /// Creates a new instance of the channel buffer manager
    /// </summary>
    /// <param name="defaultBufferCapacity">Default capacity for new buffers (default: 100)</param>
    public ChannelBufferManager(int defaultBufferCapacity = 100)
    {
        _defaultBufferCapacity = defaultBufferCapacity;
    }
    
    /// <summary>
    /// Gets a channel buffer for the specified channel, creating it if it doesn't exist
    /// </summary>
    /// <param name="channelId">The channel identifier</param>
    /// <returns>The channel buffer</returns>
    public ChannelBuffer GetBuffer(string channelId)
    {
        return _channelBuffers.GetOrAdd(channelId, id => new ChannelBuffer(id, _defaultBufferCapacity));
    }
    
    /// <summary>
    /// Gets all active channel buffers
    /// </summary>
    /// <returns>A collection of all channel buffers</returns>
    public ICollection<ChannelBuffer> GetAllBuffers()
    {
        return _channelBuffers.Values.ToList();
    }
    
    /// <summary>
    /// Gets active channel buffers matching the specified predicate
    /// </summary>
    /// <param name="predicate">A function to test each buffer</param>
    /// <returns>A collection of buffers matching the predicate</returns>
    public ICollection<ChannelBuffer> GetBuffers(Func<ChannelBuffer, bool> predicate)
    {
        return _channelBuffers.Values.Where(predicate).ToList();
    }
    
    /// <summary>
    /// Tries to add a message event to the specified channel buffer
    /// </summary>
    /// <param name="channelId">The channel identifier</param>
    /// <param name="messageEvent">The message event to add</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task AddEventAsync(string channelId, MessageEvent messageEvent)
    {
        var buffer = GetBuffer(channelId);
        await buffer.AddEventAsync(messageEvent);
    }
    
    /// <summary>
    /// Removes a channel buffer
    /// </summary>
    /// <param name="channelId">The channel identifier</param>
    /// <returns>True if the buffer was removed; otherwise, false</returns>
    public bool RemoveBuffer(string channelId)
    {
        return _channelBuffers.TryRemove(channelId, out _);
    }
    
    /// <summary>
    /// Gets channel buffers that haven't had activity since the specified time
    /// </summary>
    /// <param name="thresholdTime">The time threshold for inactivity</param>
    /// <returns>A collection of inactive channel buffers</returns>
    public ICollection<ChannelBuffer> GetInactiveBuffers(DateTimeOffset thresholdTime)
    {
        return _channelBuffers.Values
            .Where(buffer => buffer.LastEventTimestamp.HasValue && buffer.LastEventTimestamp < thresholdTime)
            .ToList();
    }
}
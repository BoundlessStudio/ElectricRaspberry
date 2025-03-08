using ElectricRaspberry.Models.Conversation;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Interface for managing multiple channel buffers for concurrent observation
/// </summary>
public interface IChannelBufferManager
{
    /// <summary>
    /// Gets a channel buffer for the specified channel, creating it if it doesn't exist
    /// </summary>
    /// <param name="channelId">The channel identifier</param>
    /// <returns>The channel buffer</returns>
    ChannelBuffer GetBuffer(string channelId);
    
    /// <summary>
    /// Gets all active channel buffers
    /// </summary>
    /// <returns>A collection of all channel buffers</returns>
    ICollection<ChannelBuffer> GetAllBuffers();
    
    /// <summary>
    /// Gets active channel buffers matching the specified predicate
    /// </summary>
    /// <param name="predicate">A function to test each buffer</param>
    /// <returns>A collection of buffers matching the predicate</returns>
    ICollection<ChannelBuffer> GetBuffers(Func<ChannelBuffer, bool> predicate);
    
    /// <summary>
    /// Tries to add a message event to the specified channel buffer
    /// </summary>
    /// <param name="channelId">The channel identifier</param>
    /// <param name="messageEvent">The message event to add</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task AddEventAsync(string channelId, MessageEvent messageEvent);
    
    /// <summary>
    /// Removes a channel buffer
    /// </summary>
    /// <param name="channelId">The channel identifier</param>
    /// <returns>True if the buffer was removed; otherwise, false</returns>
    bool RemoveBuffer(string channelId);
    
    /// <summary>
    /// Gets channel buffers that haven't had activity since the specified time
    /// </summary>
    /// <param name="thresholdTime">The time threshold for inactivity</param>
    /// <returns>A collection of inactive channel buffers</returns>
    ICollection<ChannelBuffer> GetInactiveBuffers(DateTimeOffset thresholdTime);
}
using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Observation;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Interface for prioritizing events from different channels
/// </summary>
public interface IEventPrioritizationService
{
    /// <summary>
    /// Prioritizes a single message event
    /// </summary>
    /// <param name="messageEvent">The message event to prioritize</param>
    /// <param name="channelId">The channel ID where the event originated</param>
    /// <param name="botUserId">The bot's user ID</param>
    /// <returns>A prioritized event with assigned priority level</returns>
    Task<PrioritizedEvent> PrioritizeEventAsync(MessageEvent messageEvent, string channelId, string botUserId);
    
    /// <summary>
    /// Prioritizes all events in a specific channel buffer
    /// </summary>
    /// <param name="channelId">The channel ID to prioritize events from</param>
    /// <param name="botUserId">The bot's user ID</param>
    /// <returns>A collection of prioritized events</returns>
    Task<ICollection<PrioritizedEvent>> PrioritizeChannelEventsAsync(string channelId, string botUserId);
    
    /// <summary>
    /// Prioritizes events across all active channel buffers
    /// </summary>
    /// <param name="botUserId">The bot's user ID</param>
    /// <returns>A collection of prioritized events from all channels</returns>
    Task<ICollection<PrioritizedEvent>> PrioritizeAllEventsAsync(string botUserId);
}
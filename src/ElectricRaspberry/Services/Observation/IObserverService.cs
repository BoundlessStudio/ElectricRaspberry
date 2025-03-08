using ElectricRaspberry.Models.Conversation;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Interface for managing concurrent observation of multiple channels
/// </summary>
public interface IObserverService
{
    /// <summary>
    /// Processes a new message event
    /// </summary>
    /// <param name="messageEvent">The message event</param>
    /// <param name="channelId">The channel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ProcessMessageEventAsync(MessageEvent messageEvent, string channelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes multiple message events from different channels based on priority
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ProcessPrioritizedEventsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs maintenance on observation components
    /// </summary>
    void PerformMaintenance();
}
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace ElectricRaspberry.Services;

/// <summary>
/// Service for handling Discord voice channel operations
/// </summary>
public interface IVoiceService
{
    /// <summary>
    /// Gets whether the bot is currently connected to a voice channel
    /// </summary>
    /// <returns>True if connected to any voice channel, false otherwise</returns>
    Task<bool> IsConnectedToVoiceAsync();

    /// <summary>
    /// Joins a voice channel
    /// </summary>
    /// <param name="voiceChannel">The voice channel to join</param>
    /// <returns>Audio client for the voice connection</returns>
    Task<IAudioClient> JoinVoiceChannelAsync(IVoiceChannel voiceChannel);

    /// <summary>
    /// Leaves the current voice channel
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task LeaveVoiceChannelAsync();

    /// <summary>
    /// Gets the voice channel the bot is currently connected to
    /// </summary>
    /// <returns>Voice channel if connected, null otherwise</returns>
    Task<IVoiceChannel?> GetCurrentVoiceChannelAsync();

    /// <summary>
    /// Processes voice state updates from users in voice channels
    /// </summary>
    /// <param name="user">The user whose voice state changed</param>
    /// <param name="oldState">The user's previous voice state</param>
    /// <param name="newState">The user's new voice state</param>
    /// <returns>Task representing the operation</returns>
    Task ProcessVoiceStateUpdateAsync(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState);

    /// <summary>
    /// Gets a list of users currently in the same voice channel as the bot
    /// </summary>
    /// <returns>Collection of users in the same voice channel</returns>
    Task<IReadOnlyCollection<SocketGuildUser>> GetUsersInVoiceChannelAsync();

    /// <summary>
    /// Determines whether the bot should automatically join a voice channel based on context
    /// </summary>
    /// <param name="voiceChannel">The voice channel to evaluate</param>
    /// <returns>True if the bot should join, false otherwise</returns>
    Task<bool> ShouldJoinVoiceChannelAsync(IVoiceChannel voiceChannel);

    /// <summary>
    /// Determines whether the bot should leave its current voice channel based on context
    /// </summary>
    /// <returns>True if the bot should leave, false otherwise</returns>
    Task<bool> ShouldLeaveVoiceChannelAsync();
}
namespace ElectricRaspberry.Services.Admin;

/// <summary>
/// Interface for bot administration operations
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Forces the bot to enter sleep mode
    /// </summary>
    /// <param name="durationMinutes">Optional sleep duration in minutes (null for indefinite)</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    Task<bool> ForceSleepAsync(int? durationMinutes = null);
    
    /// <summary>
    /// Forces the bot to wake up from sleep mode
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    Task<bool> ForceWakeAsync();
    
    /// <summary>
    /// Stops the bot from sending messages for a specified duration
    /// </summary>
    /// <param name="durationMinutes">Silence duration in minutes</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    Task<bool> SilenceBotAsync(int durationMinutes);
    
    /// <summary>
    /// Removes the silence restriction from the bot
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    Task<bool> UnsilenceBotAsync();
    
    /// <summary>
    /// Resets the bot's emotional and conversational state
    /// </summary>
    /// <param name="resetMemory">Whether to also reset the knowledge graph (defaults to false)</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    Task<bool> ResetBotStateAsync(bool resetMemory = false);
    
    /// <summary>
    /// Immediately stops all bot activities and disconnects from Discord
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    Task<bool> EmergencyStopAsync();
    
    /// <summary>
    /// Checks if the given user ID has admin permissions for the bot
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if the user is an admin</returns>
    Task<bool> IsAdminAsync(string userId);
}
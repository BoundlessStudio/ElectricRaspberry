namespace ElectricRaspberry.Services;

/// <summary>
/// Service for managing the bot's stamina, energy levels, and sleep/wake cycles
/// </summary>
public interface IStaminaService
{
    /// <summary>
    /// Gets the current stamina level
    /// </summary>
    /// <returns>Current stamina value between 0-100</returns>
    Task<double> GetCurrentStaminaAsync();
    
    /// <summary>
    /// Consumes stamina for an activity
    /// </summary>
    /// <param name="amount">Amount of stamina to consume</param>
    /// <returns>Remaining stamina after consumption</returns>
    Task<double> ConsumeStaminaAsync(double amount);
    
    /// <summary>
    /// Recovers stamina at the passive recovery rate
    /// </summary>
    /// <param name="minutes">Minutes to recover for</param>
    /// <returns>New stamina value after recovery</returns>
    Task<double> RecoverStaminaAsync(double minutes);
    
    /// <summary>
    /// Checks if the bot is currently in sleep mode
    /// </summary>
    /// <returns>True if sleeping, false otherwise</returns>
    Task<bool> IsSleepingAsync();
    
    /// <summary>
    /// Forces the bot to enter sleep mode
    /// </summary>
    /// <param name="duration">Optional duration for sleep, null for indefinite</param>
    /// <returns>Task representing the operation</returns>
    Task ForceSleepModeAsync(TimeSpan? duration = null);
    
    /// <summary>
    /// Forces the bot to wake from sleep mode
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task ForceWakeAsync();
    
    /// <summary>
    /// Checks if stamina is below the low threshold and should trigger sleep
    /// </summary>
    /// <returns>True if stamina is below threshold, false otherwise</returns>
    Task<bool> ShouldSleepAsync();
    
    /// <summary>
    /// Checks if stamina is high enough to wake up from sleep
    /// </summary>
    /// <returns>True if stamina is high enough to wake, false otherwise</returns>
    Task<bool> ShouldWakeAsync();
    
    /// <summary>
    /// Resets the stamina to maximum value
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ResetStaminaAsync();
    
    /// <summary>
    /// Gets the maximum stamina value
    /// </summary>
    /// <returns>Maximum stamina value</returns>
    Task<double> GetMaxStaminaAsync();
}
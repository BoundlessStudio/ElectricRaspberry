using ElectricRaspberry.Models.Emotions;

namespace ElectricRaspberry.Services;

/// <summary>
/// Service for managing the bot's emotional state and responses
/// </summary>
public interface IEmotionalService
{
    /// <summary>
    /// Processes an emotional trigger and updates the bot's emotional state
    /// </summary>
    /// <param name="trigger">The emotional trigger to process</param>
    /// <returns>Task representing the operation</returns>
    Task ProcessEmotionalTriggerAsync(EmotionalTrigger trigger);
    
    /// <summary>
    /// Gets the current emotional state
    /// </summary>
    /// <returns>The current emotional state</returns>
    Task<EmotionalState> GetCurrentEmotionalStateAsync();
    
    /// <summary>
    /// Gets the current emotional expression for communication
    /// </summary>
    /// <returns>The current emotional expression</returns>
    Task<EmotionalExpression> GetCurrentExpressionAsync();
    
    /// <summary>
    /// Performs maintenance on the emotional state (recovery to baseline)
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task PerformEmotionalMaintenanceAsync();
    
    /// <summary>
    /// Creates an emotional trigger from a message
    /// </summary>
    /// <param name="content">The message content</param>
    /// <param name="sourceId">The source ID (user or channel)</param>
    /// <param name="messageId">The message ID</param>
    /// <returns>The created emotional trigger</returns>
    Task<EmotionalTrigger> CreateTriggerFromMessageAsync(string content, string sourceId, string messageId);
    
    /// <summary>
    /// Calculates the emotional impact of a trigger
    /// </summary>
    /// <param name="trigger">The emotional trigger</param>
    /// <returns>The calculated emotional impact</returns>
    EmotionalImpact CalculateEmotionalImpact(EmotionalTrigger trigger);
    
    /// <summary>
    /// Calculates the stamina cost of an emotional impact
    /// </summary>
    /// <param name="impact">The emotional impact</param>
    /// <returns>The stamina cost</returns>
    double CalculateEmotionalStaminaCost(EmotionalImpact impact);
    
    /// <summary>
    /// Resets the emotional state to baseline
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ResetEmotionalStateAsync();
}
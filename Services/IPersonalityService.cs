using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Emotions;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Service responsible for managing and adapting the bot's personality over time based on interactions
    /// </summary>
    public interface IPersonalityService
    {
        /// <summary>
        /// Gets the current personality profile
        /// </summary>
        /// <returns>Dictionary of personality traits and their strength values (0.0-1.0)</returns>
        Task<Dictionary<string, double>> GetPersonalityProfileAsync();

        /// <summary>
        /// Updates the personality based on an interaction
        /// </summary>
        /// <param name="messageEvent">The message event that occurred</param>
        /// <param name="emotionalImpact">The emotional impact of the message</param>
        Task UpdatePersonalityFromInteractionAsync(MessageEvent messageEvent, EmotionalImpact emotionalImpact);

        /// <summary>
        /// Gets a value indicating how likely the bot is to initiate conversation based on current context
        /// </summary>
        /// <param name="conversation">The current conversation context</param>
        /// <returns>Probability value between 0.0 and 1.0</returns>
        Task<double> GetInitiationProbabilityAsync(Conversation conversation);

        /// <summary>
        /// Determines if the bot should respond to a message based on personality and context
        /// </summary>
        /// <param name="messageEvent">The message event to evaluate</param>
        /// <param name="conversation">The current conversation context</param>
        /// <returns>True if the bot should respond, false otherwise</returns>
        Task<bool> ShouldRespondToMessageAsync(MessageEvent messageEvent, Conversation conversation);

        /// <summary>
        /// Gets the preferred response style based on current personality and context
        /// </summary>
        /// <param name="emotionalState">The current emotional state</param>
        /// <param name="conversation">The current conversation</param>
        /// <returns>A dictionary of style attributes and their values</returns>
        Task<Dictionary<string, object>> GetResponseStyleAsync(EmotionalState emotionalState, Conversation conversation);
    }
}
using ElectricRaspberry.Models.Emotions;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Service responsible for managing the bot's persona, including personality, preferences, and behavior traits
    /// </summary>
    public interface IPersonaService
    {
        /// <summary>
        /// Gets the bot's name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the bot's short description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the dominant personality traits of the bot
        /// </summary>
        /// <returns>A dictionary of trait names and their intensity values (0.0-1.0)</returns>
        Task<Dictionary<string, double>> GetPersonalityTraitsAsync();

        /// <summary>
        /// Gets the current emotional state of the bot
        /// </summary>
        Task<EmotionalState> GetEmotionalStateAsync();

        /// <summary>
        /// Gets the bot's interests with confidence ratings
        /// </summary>
        /// <returns>A dictionary of interest topics and their relevance scores (0.0-1.0)</returns>
        Task<Dictionary<string, double>> GetInterestsAsync();

        /// <summary>
        /// Gets a list of response templates for different emotional states and contexts
        /// </summary>
        /// <param name="emotion">The emotional state to get responses for</param>
        /// <param name="context">Optional context tag for more specific responses</param>
        /// <returns>A list of response templates that can be used</returns>
        Task<IEnumerable<string>> GetResponseTemplatesAsync(string emotion, string context = null);

        /// <summary>
        /// Updates the bot's interests based on interaction
        /// </summary>
        /// <param name="topic">The topic of interest</param>
        /// <param name="relevanceAdjustment">How much to adjust the relevance (-1.0 to 1.0)</param>
        Task UpdateInterestAsync(string topic, double relevanceAdjustment);
    }
}
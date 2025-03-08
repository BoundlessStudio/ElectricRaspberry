using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Emotions;
using ElectricRaspberry.Models.Knowledge.Edges;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Implementation of IPersonalityService for managing bot's personality
    /// </summary>
    public class PersonalityService : IPersonalityService
    {
        private readonly ILogger<PersonalityService> _logger;
        private readonly PersonalityOptions _options;
        private readonly IPersonaService _personaService;
        private readonly IKnowledgeService _knowledgeService;
        private readonly IEmotionalService _emotionalService;
        
        private readonly ConcurrentDictionary<string, double> _personalityTraits = new();
        private readonly Random _random = new Random();
        private DateTime _lastInitiationTime = DateTime.MinValue;

        public PersonalityService(
            ILogger<PersonalityService> logger,
            IOptions<PersonalityOptions> options,
            IPersonaService personaService,
            IKnowledgeService knowledgeService,
            IEmotionalService emotionalService)
        {
            _logger = logger;
            _options = options.Value;
            _personaService = personaService;
            _knowledgeService = knowledgeService;
            _emotionalService = emotionalService;
            
            // Initialize personality traits
            InitializeAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Start with base personality traits from the persona service
                var baseTraits = await _personaService.GetPersonalityTraitsAsync();
                foreach (var trait in baseTraits)
                {
                    _personalityTraits[trait.Key] = trait.Value;
                }

                _logger.LogInformation("Initialized personality with {count} traits", _personalityTraits.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing personality service");
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, double>> GetPersonalityProfileAsync()
        {
            return _personalityTraits.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <inheritdoc/>
        public async Task UpdatePersonalityFromInteractionAsync(MessageEvent messageEvent, EmotionalImpact emotionalImpact)
        {
            try
            {
                // Only adapt personality if the emotional impact is significant
                if (emotionalImpact.Intensity < _options.EmotionalImpactThreshold)
                {
                    return;
                }

                // Get the dominant emotion from the impact
                var dominantEmotion = emotionalImpact.GetDominantEmotion().ToString();

                // Check if we have a mapping for this emotion
                if (_options.EmotionalPersonalityImpacts.TryGetValue(dominantEmotion, out var impacts))
                {
                    foreach (var impact in impacts)
                    {
                        var traitName = impact.Key;
                        var adjustmentValue = impact.Value * emotionalImpact.Intensity * _options.AdaptationRate;

                        // Get current value or default
                        double currentValue = _personalityTraits.GetValueOrDefault(traitName, 0.5);
                        
                        // Apply adjustment
                        double newValue = Math.Clamp(
                            currentValue + adjustmentValue,
                            _options.MinTraitValue,
                            _options.MaxTraitValue);
                        
                        // Update the trait
                        _personalityTraits[traitName] = newValue;
                        
                        _logger.LogDebug("Updated personality trait {trait} from {oldValue} to {newValue} due to {emotion}",
                            traitName, currentValue, newValue, dominantEmotion);
                    }

                    _logger.LogInformation("Updated {count} personality traits based on {emotion} with intensity {intensity}",
                        impacts.Count, dominantEmotion, emotionalImpact.Intensity);
                }

                // Update relationship with user if applicable
                if (messageEvent.AuthorId != null)
                {
                    await _knowledgeService.RecordInteractionAsync(messageEvent.AuthorId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating personality from interaction");
            }
        }

        /// <inheritdoc/>
        public async Task<double> GetInitiationProbabilityAsync(Conversation conversation)
        {
            try
            {
                // Base probability
                double probability = _options.BaseInitiationProbability;

                // Don't initiate if we've done so recently
                TimeSpan timeSinceLastInitiation = DateTime.UtcNow - _lastInitiationTime;
                if (timeSinceLastInitiation.TotalSeconds < _options.MinInitiationDelaySeconds)
                {
                    return 0.0;
                }

                // Don't initiate if the conversation is too active
                if (conversation != null && conversation.State == ConversationState.Active)
                {
                    TimeSpan timeSinceLastMessage = DateTime.UtcNow - conversation.LastActivityTime;
                    if (timeSinceLastMessage.TotalMinutes < 5)
                    {
                        return 0.0;
                    }
                }

                // Check if conversation is dormant
                bool isDormant = conversation == null || 
                                 (DateTime.UtcNow - conversation.LastActivityTime).TotalMinutes >= _options.DormantConversationThresholdMinutes;
                
                // Increase probability for dormant conversations
                if (isDormant)
                {
                    probability *= 1.5;
                }

                // Adjust based on personality traits
                foreach (var trait in _personalityTraits)
                {
                    if (_options.InitiationTraitMultipliers.TryGetValue(trait.Key, out double multiplier))
                    {
                        // Weighted by trait value and its multiplier
                        probability *= 1.0 + ((trait.Value - 0.5) * (multiplier - 1.0));
                    }
                }

                // Check emotional state - less likely to initiate if in negative emotional state
                var emotionalState = await _emotionalService.GetCurrentEmotionalStateAsync();
                if (emotionalState.GetDominantEmotion() == CoreEmotions.Sadness ||
                    emotionalState.GetDominantEmotion() == CoreEmotions.Anger ||
                    emotionalState.GetDominantEmotion() == CoreEmotions.Fear)
                {
                    probability *= 0.7;
                }

                // Check stamina - less likely to initiate when stamina is low
                var staminaService = await GetStaminaServiceAsync();
                if (staminaService != null)
                {
                    double currentStamina = await staminaService.GetCurrentStaminaAsync();
                    double maxStamina = await staminaService.GetMaxStaminaAsync();
                    double staminaRatio = currentStamina / maxStamina;

                    // Reduce probability when stamina is below 50%
                    if (staminaRatio < 0.5)
                    {
                        probability *= staminaRatio * 1.5; // Scale down based on stamina
                    }
                }

                // Clamp to valid range
                return Math.Clamp(probability, 0.0, 1.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating initiation probability");
                return 0.0;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ShouldRespondToMessageAsync(MessageEvent messageEvent, Conversation conversation)
        {
            try
            {
                // Base response probability
                double probability = _options.BaseResponseProbability;

                // Always respond if directly mentioned
                if (messageEvent.IsMentioned)
                {
                    return true;
                }

                // Adjust based on relationship with user
                if (messageEvent.AuthorId != null)
                {
                    try
                    {
                        var relationship = await _knowledgeService.GetEdgesByTypeAsync<RelationshipEdge>(
                            edge => edge.TargetId == messageEvent.AuthorId);
                        
                        if (relationship != null && relationship.Any())
                        {
                            // Higher relationship strength = more likely to respond
                            probability *= 1.0 + (relationship.First().Strength - 0.5);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error retrieving relationship data");
                    }
                }

                // Check if in an active conversation context
                if (conversation != null && conversation.State == ConversationState.Active)
                {
                    probability *= 1.2; // More likely to respond in active conversations
                }

                // Check personality traits
                if (_personalityTraits.TryGetValue("Responsive", out double responsiveness))
                {
                    probability *= 0.8 + (responsiveness * 0.4); // Scale from 0.8 to 1.2 based on trait
                }

                // Check emotional state - less likely to respond when in negative emotional state
                var emotionalState = await _emotionalService.GetCurrentEmotionalStateAsync();
                if (emotionalState.GetDominantEmotion() == CoreEmotions.Sadness ||
                    emotionalState.GetDominantEmotion() == CoreEmotions.Anger)
                {
                    probability *= 0.8;
                }

                // Check stamina level - less likely to respond when stamina is low
                var staminaService = await GetStaminaServiceAsync();
                if (staminaService != null)
                {
                    double currentStamina = await staminaService.GetCurrentStaminaAsync();
                    double maxStamina = await staminaService.GetMaxStaminaAsync();
                    double staminaRatio = currentStamina / maxStamina;

                    // Significantly reduce probability when stamina is below 30%
                    if (staminaRatio < 0.3)
                    {
                        probability *= staminaRatio * 2; // Scale down based on stamina
                    }
                }

                // Roll the dice
                double roll = _random.NextDouble();
                bool shouldRespond = roll < probability;

                if (shouldRespond)
                {
                    // Determine a random delay before responding to seem more natural
                    int delayMs = _random.Next(_options.MinResponseDelayMs, _options.MaxResponseDelayMs);
                    await Task.Delay(delayMs);
                }

                return shouldRespond;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining if should respond to message");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, object>> GetResponseStyleAsync(EmotionalState emotionalState, Conversation conversation)
        {
            var style = new Dictionary<string, object>();

            try
            {
                // Base verbosity level (1-5, where 1 is very concise and 5 is very verbose)
                int verbosity = 3;
                
                // Base formality level (1-5, where 1 is very casual and 5 is very formal)
                int formality = 3;
                
                // Base enthusiasm level (1-5, where 1 is very subdued and 5 is very enthusiastic)
                int enthusiasm = 3;
                
                // Emoticon frequency (0-1, where 0 is never and 1 is very frequent)
                double emoticonFrequency = 0.3;

                // Adjust based on emotional state
                if (emotionalState != null)
                {
                    var dominantEmotion = emotionalState.GetDominantEmotion();
                    switch (dominantEmotion)
                    {
                        case CoreEmotions.Joy:
                            enthusiasm += 1;
                            emoticonFrequency += 0.2;
                            break;
                        case CoreEmotions.Sadness:
                            enthusiasm -= 1;
                            verbosity -= 1;
                            break;
                        case CoreEmotions.Anger:
                            verbosity -= 1;
                            formality -= 1;
                            break;
                        case CoreEmotions.Surprise:
                            enthusiasm += 1;
                            emoticonFrequency += 0.1;
                            break;
                        case CoreEmotions.Fear:
                            verbosity -= 1;
                            formality += 1;
                            break;
                    }
                }

                // Adjust based on personality traits
                if (_personalityTraits.TryGetValue("Verbose", out double verboseTrait))
                {
                    verbosity += (int)Math.Round((verboseTrait - 0.5) * 4); // -2 to +2 adjustment
                }

                if (_personalityTraits.TryGetValue("Formal", out double formalTrait))
                {
                    formality += (int)Math.Round((formalTrait - 0.5) * 4); // -2 to +2 adjustment
                }

                if (_personalityTraits.TryGetValue("Enthusiastic", out double enthusiasticTrait))
                {
                    enthusiasm += (int)Math.Round((enthusiasticTrait - 0.5) * 4); // -2 to +2 adjustment
                }

                if (_personalityTraits.TryGetValue("Expressive", out double expressiveTrait))
                {
                    emoticonFrequency += (expressiveTrait - 0.5) * 0.4; // -0.2 to +0.2 adjustment
                }

                // Check if in an active conversation - adjust style for more continuity
                if (conversation != null && conversation.State == ConversationState.Active)
                {
                    // In active conversations, be slightly more verbose and enthusiastic
                    verbosity = Math.Min(verbosity + 1, 5);
                    enthusiasm = Math.Min(enthusiasm + 1, 5);
                }

                // Check stamina level - less enthusiastic and verbose when low on stamina
                var staminaService = await GetStaminaServiceAsync();
                if (staminaService != null)
                {
                    double currentStamina = await staminaService.GetCurrentStaminaAsync();
                    double maxStamina = await staminaService.GetMaxStaminaAsync();
                    double staminaRatio = currentStamina / maxStamina;

                    if (staminaRatio < 0.5)
                    {
                        verbosity = Math.Max(1, verbosity - 1);
                        enthusiasm = Math.Max(1, enthusiasm - 1);
                    }
                }

                // Clamp values to valid ranges
                verbosity = Math.Clamp(verbosity, 1, 5);
                formality = Math.Clamp(formality, 1, 5);
                enthusiasm = Math.Clamp(enthusiasm, 1, 5);
                emoticonFrequency = Math.Clamp(emoticonFrequency, 0.0, 1.0);

                // Build style dictionary
                style["Verbosity"] = verbosity;
                style["Formality"] = formality;
                style["Enthusiasm"] = enthusiasm;
                style["EmoticonFrequency"] = emoticonFrequency;

                // Add typical sentence count based on verbosity
                style["SentenceCount"] = verbosity switch
                {
                    1 => 1,
                    2 => _random.Next(1, 3),
                    3 => _random.Next(2, 4),
                    4 => _random.Next(3, 5),
                    5 => _random.Next(4, 7),
                    _ => 2
                };

                return style;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining response style");
                
                // Return default style on error
                return new Dictionary<string, object>
                {
                    { "Verbosity", 3 },
                    { "Formality", 3 },
                    { "Enthusiasm", 3 },
                    { "EmoticonFrequency", 0.3 },
                    { "SentenceCount", 2 }
                };
            }
        }

        /// <summary>
        /// Helper method to safely get the stamina service
        /// </summary>
        private async Task<IStaminaService> GetStaminaServiceAsync()
        {
            try
            {
                // In a real implementation this would be injected via DI
                // This is a workaround to avoid circular dependencies
                // We could use a service locator pattern in production
                var serviceProvider = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IServiceProvider>(
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IServiceProvider>(null));
                
                return serviceProvider.GetService<IStaminaService>();
            }
            catch
            {
                // If we can't get the stamina service, return null
                _logger.LogWarning("Could not get StaminaService");
                return null;
            }
        }
    }
}
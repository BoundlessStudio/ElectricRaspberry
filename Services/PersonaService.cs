using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Emotions;
using ElectricRaspberry.Models.Knowledge.Edges;
using ElectricRaspberry.Models.Knowledge.Vertices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Implementation of IPersonaService managing the bot's persona
    /// </summary>
    public class PersonaService : IPersonaService
    {
        private readonly ILogger<PersonaService> _logger;
        private readonly PersonaOptions _options;
        private readonly IEmotionalService _emotionalService;
        private readonly IKnowledgeService _knowledgeService;
        private readonly ConcurrentDictionary<string, double> _personalityTraits;
        private readonly ConcurrentDictionary<string, double> _interests;

        public string Name => _options.Name;
        public string Description => _options.Description;

        public PersonaService(
            ILogger<PersonaService> logger,
            IOptions<PersonaOptions> options,
            IEmotionalService emotionalService,
            IKnowledgeService knowledgeService)
        {
            _logger = logger;
            _options = options.Value;
            _emotionalService = emotionalService;
            _knowledgeService = knowledgeService;
            _personalityTraits = new ConcurrentDictionary<string, double>(_options.BasePersonalityTraits);
            _interests = new ConcurrentDictionary<string, double>(_options.BaseInterests);

            // Initialize personality traits and interests
            InitializeAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Load personality traits and interests from knowledge graph if available
                await LoadInterestsFromKnowledgeGraphAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing persona service");
            }
        }

        private async Task LoadInterestsFromKnowledgeGraphAsync()
        {
            try
            {
                // Try to get interests from the knowledge graph
                var topicEdges = await _knowledgeService.GetEdgesByTypeAsync<InterestEdge>();
                if (topicEdges != null && topicEdges.Any())
                {
                    foreach (var edge in topicEdges)
                    {
                        var topic = await _knowledgeService.GetVertexByIdAsync<TopicVertex>(edge.TargetId);
                        if (topic != null)
                        {
                            _interests[topic.Name] = edge.Level;
                        }
                    }
                    _logger.LogInformation("Loaded {count} interests from knowledge graph", topicEdges.Count());
                }
                else
                {
                    _logger.LogInformation("No interests found in knowledge graph, using defaults");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading interests from knowledge graph");
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, double>> GetPersonalityTraitsAsync()
        {
            return _personalityTraits.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <inheritdoc/>
        public async Task<EmotionalState> GetEmotionalStateAsync()
        {
            return await _emotionalService.GetCurrentEmotionalStateAsync();
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, double>> GetInterestsAsync()
        {
            // First try to update from the knowledge graph
            await LoadInterestsFromKnowledgeGraphAsync();
            return _interests.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetResponseTemplatesAsync(CoreEmotions emotion, string context = null)
        {
            string key = emotion.ToString();
            
            // Try to get context-specific templates first
            if (!string.IsNullOrEmpty(context))
            {
                string contextKey = $"{emotion}_{context}";
                if (_options.ResponseTemplates.TryGetValue(contextKey, out var contextTemplates))
                {
                    return contextTemplates;
                }
            }

            // Fall back to general templates for the emotion
            if (_options.ResponseTemplates.TryGetValue(key, out var templates))
            {
                return templates;
            }

            // Default templates if nothing matches
            return new[] { "I see. {0}", "Interesting. {0}", "I understand. {0}" };
        }

        /// <inheritdoc/>
        public async Task UpdateInterestAsync(string topic, double relevanceAdjustment)
        {
            if (string.IsNullOrEmpty(topic))
                return;

            // Normalize the topic to lowercase for consistency
            topic = topic.Trim();

            // Get current value or default to 0.5
            double currentValue = _interests.GetValueOrDefault(topic, 0.5);
            
            // Apply adjustment with rate scaling
            double adjustedValue = currentValue + (relevanceAdjustment * _options.InterestChangeRate);
            
            // Clamp to valid range
            adjustedValue = Math.Max(0.0, Math.Min(1.0, adjustedValue));
            
            // Update local dictionary
            _interests[topic] = adjustedValue;
            
            _logger.LogInformation("Updated interest in {topic} from {oldValue} to {newValue}", 
                topic, currentValue, adjustedValue);

            // If significant change, also update the knowledge graph
            if (Math.Abs(adjustedValue - currentValue) > 0.05)
            {
                try
                {
                    await _knowledgeService.RecordInterestAsync(topic, adjustedValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating interest in knowledge graph for {topic}", topic);
                }
            }
        }
    }
}
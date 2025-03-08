using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Emotions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Services;

public class EmotionalService : IEmotionalService
{
    private readonly ILogger<EmotionalService> _logger;
    private readonly IStaminaService _staminaService;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly EmotionalState _currentState = new();
    private readonly PersonalityProfile _personalityProfile = new(); // TODO: Replace with IPersonalityService
    private readonly ExpressionMapper _expressionMapper = new(); // TODO: Extract to interface if needed
    
    public EmotionalService(
        ILogger<EmotionalService> logger,
        IStaminaService staminaService)
    {
        _logger = logger;
        _staminaService = staminaService;
    }
    
    public async Task ProcessEmotionalTriggerAsync(EmotionalTrigger trigger)
    {
        await _stateLock.WaitAsync();
        try
        {
            // Calculate emotional impact
            var impact = CalculateEmotionalImpact(trigger);
            
            // Update emotional state
            foreach (var (emotion, change) in impact.Changes)
            {
                _currentState.AdjustEmotion(emotion, change);
            }
            
            // Calculate stamina effect
            double staminaCost = CalculateEmotionalStaminaCost(impact);
            await _staminaService.ConsumeStaminaAsync(staminaCost);
            
            // Log significant emotional changes
            if (impact.Significance > 0.5)
            {
                _logger.LogInformation(
                    "Significant emotional change: {TriggerType}, Impact: {Significance}",
                    trigger.Type,
                    impact.Significance);
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    public async Task<EmotionalState> GetCurrentEmotionalStateAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            // Return a clone of the current state to prevent external modification
            var clone = new EmotionalState();
            foreach (var (emotion, value) in _currentState.Emotions)
            {
                clone.SetEmotion(emotion, value);
            }
            
            return clone;
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    public async Task<EmotionalExpression> GetCurrentExpressionAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            // Get current stamina to influence expression
            var stamina = await _staminaService.GetCurrentStaminaAsync();
            
            // Apply personality and stamina modifiers to raw emotional state
            return _expressionMapper.MapStateToExpression(_currentState, _personalityProfile, stamina);
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    public async Task PerformEmotionalMaintenanceAsync()
    {
        // Called periodically and during sleep periods
        await _stateLock.WaitAsync();
        try
        {
            // Gradually return emotions to baseline
            foreach (var emotion in _currentState.Emotions.ToList()) // Using ToList to avoid modification during enumeration
            {
                var baselineValue = _personalityProfile.GetEmotionalBaseline(emotion.Key);
                var currentValue = emotion.Value;
                var recoveryRate = _personalityProfile.GetRecoveryRate(emotion.Key);
                
                // Apply stamina modifier to recovery rate
                var stamina = await _staminaService.GetCurrentStaminaAsync();
                var staminaModifier = 1.0 + ((100 - stamina) / 200); // 1.0 to 1.5x slower when tired
                
                // Calculate new value moving toward baseline
                var step = (baselineValue - currentValue) * (recoveryRate / staminaModifier);
                _currentState.SetEmotion(emotion.Key, currentValue + step);
            }
            
            _logger.LogDebug("Emotional maintenance performed. Dominant emotion: {Emotion}", 
                _currentState.GetDominantEmotion());
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    public async Task<EmotionalTrigger> CreateTriggerFromMessageAsync(string content, string sourceId, string messageId)
    {
        // TODO: Implement sentiment analysis to determine emotion changes
        // For now, create a simple trigger with default values
        
        var trigger = new EmotionalTrigger
        {
            Type = EmotionalTriggerType.DirectMessage,
            Source = sourceId,
            ContentId = messageId,
            Intensity = 0.5, // Medium intensity
            Context = content.Length > 100 ? content.Substring(0, 100) + "..." : content
        };
        
        // Analyze content for positive/negative sentiment
        bool isPositive = !content.Contains("sad") && !content.Contains("angry") && !content.Contains("bad");
        
        // Set basic emotion changes based on simple sentiment
        if (isPositive)
        {
            trigger.EmotionChanges[CoreEmotions.Joy] = 10;
            trigger.EmotionChanges[CoreEmotions.Sadness] = -5;
        }
        else
        {
            trigger.EmotionChanges[CoreEmotions.Sadness] = 10;
            trigger.EmotionChanges[CoreEmotions.Joy] = -5;
        }
        
        return trigger;
    }
    
    public EmotionalImpact CalculateEmotionalImpact(EmotionalTrigger trigger)
    {
        var impact = new EmotionalImpact(trigger);
        
        // Apply personality-based modifiers to the impact
        foreach (var (emotion, change) in impact.Changes.ToList())
        {
            double emotionalSensitivity = _personalityProfile.GetEmotionalSensitivity(emotion);
            impact.Changes[emotion] = change * emotionalSensitivity;
        }
        
        return impact;
    }
    
    public double CalculateEmotionalStaminaCost(EmotionalImpact impact)
    {
        // Base cost determined by significance
        double baseCost = impact.Significance * 2.0;
        
        // Additional cost based on which emotions are impacted most
        double emotionMultiplier = 1.0;
        foreach (var (emotion, change) in impact.Changes)
        {
            // High-arousal emotions (joy, anger) are more draining when intense
            if ((emotion == CoreEmotions.Joy || emotion == CoreEmotions.Anger) && Math.Abs(change) > 15)
            {
                emotionMultiplier = 1.5;
                break;
            }
        }
        
        return baseCost * emotionMultiplier;
    }
    
    // TODO: Move to PersonalityService when implemented
    private class PersonalityProfile
    {
        // Baseline values that emotions tend to return to (0-100)
        private readonly Dictionary<string, double> _baselines = new()
        {
            [CoreEmotions.Joy] = 60,      // Optimistic baseline
            [CoreEmotions.Sadness] = 30,   // Low sadness baseline
            [CoreEmotions.Anger] = 25,     // Low anger baseline
            [CoreEmotions.Fear] = 30,      // Moderate fear baseline
            [CoreEmotions.Surprise] = 50,  // Neutral surprise baseline
            [CoreEmotions.Disgust] = 25    // Low disgust baseline
        };
        
        // How quickly emotions return to baseline (0-1)
        private readonly Dictionary<string, double> _recoveryRates = new()
        {
            [CoreEmotions.Joy] = 0.15,      // Slow recovery from joy
            [CoreEmotions.Sadness] = 0.1,   // Very slow recovery from sadness
            [CoreEmotions.Anger] = 0.2,     // Medium recovery from anger
            [CoreEmotions.Fear] = 0.25,     // Faster recovery from fear
            [CoreEmotions.Surprise] = 0.5,  // Quick recovery from surprise
            [CoreEmotions.Disgust] = 0.2    // Medium recovery from disgust
        };
        
        // How strongly emotions are affected by triggers (0-2)
        private readonly Dictionary<string, double> _sensitivities = new()
        {
            [CoreEmotions.Joy] = 1.5,      // Very sensitive to joy
            [CoreEmotions.Sadness] = 1.2,   // Quite sensitive to sadness
            [CoreEmotions.Anger] = 0.7,     // Less sensitive to anger
            [CoreEmotions.Fear] = 1.0,      // Average sensitivity to fear
            [CoreEmotions.Surprise] = 1.3,  // Quite sensitive to surprise
            [CoreEmotions.Disgust] = 0.8    // Less sensitive to disgust
        };
        
        public double GetEmotionalBaseline(string emotion)
        {
            return _baselines.TryGetValue(emotion, out var value) ? value : 50;
        }
        
        public double GetRecoveryRate(string emotion)
        {
            return _recoveryRates.TryGetValue(emotion, out var value) ? value : 0.2;
        }
        
        public double GetEmotionalSensitivity(string emotion)
        {
            return _sensitivities.TryGetValue(emotion, out var value) ? value : 1.0;
        }
    }
    
    private class ExpressionMapper
    {
        public EmotionalExpression MapStateToExpression(
            EmotionalState state, 
            PersonalityProfile profile,
            double stamina)
        {
            var expression = new EmotionalExpression(state);
            
            // Adjust expression based on stamina
            if (stamina < 30)
            {
                // When tired, expression is more subdued
                expression.ExpressionIntensity *= 0.7;
                expression.CommunicationModifiers.Add("show tiredness");
                
                if (stamina < 15)
                {
                    // When very tired, add specific modifiers
                    expression.CommunicationModifiers.Add("speak briefly");
                    expression.CommunicationModifiers.Add("be low energy");
                }
            }
            
            return expression;
        }
    }
}
namespace ElectricRaspberry.Models.Emotions;

/// <summary>
/// Represents how emotions are expressed in communication
/// </summary>
public class EmotionalExpression
{
    /// <summary>
    /// The underlying emotional state
    /// </summary>
    public EmotionalState State { get; set; }
    
    /// <summary>
    /// The communication tone to use (formal, casual, excited, etc.)
    /// </summary>
    public string Tone { get; set; } = "neutral";
    
    /// <summary>
    /// Emoji patterns to use in messages based on emotional state
    /// </summary>
    public List<string> SuggestedEmojis { get; set; } = new();
    
    /// <summary>
    /// Communication modifiers (e.g., "speak briefly", "be enthusiastic")
    /// </summary>
    public List<string> CommunicationModifiers { get; set; } = new();
    
    /// <summary>
    /// Whether the current emotional state is generally positive
    /// </summary>
    public bool IsPositive { get; set; }
    
    /// <summary>
    /// Whether to express emotion strongly or subtly
    /// </summary>
    public double ExpressionIntensity { get; set; } = 0.5;
    
    /// <summary>
    /// Creates a new emotional expression from an emotional state
    /// </summary>
    public EmotionalExpression(EmotionalState state)
    {
        State = state;
        IsPositive = state.IsPositive;
        
        // Set tone based on dominant emotion
        string dominantEmotion = state.GetDominantEmotion();
        Tone = MapEmotionToTone(dominantEmotion, state.GetEmotion(dominantEmotion));
        
        // Set emojis based on emotional state
        SuggestedEmojis = GetEmojisForState(state);
        
        // Set modifiers based on emotional state
        CommunicationModifiers = GetModifiersForState(state);
        
        // Set expression intensity based on arousal
        ExpressionIntensity = state.GetArousal();
    }
    
    private string MapEmotionToTone(string emotion, double intensity)
    {
        // Normalize intensity to 0-1
        double normalizedIntensity = intensity / 100.0;
        
        return emotion switch
        {
            CoreEmotions.Joy when normalizedIntensity > 0.7 => "enthusiastic",
            CoreEmotions.Joy when normalizedIntensity > 0.4 => "cheerful",
            CoreEmotions.Joy => "pleasant",
            
            CoreEmotions.Sadness when normalizedIntensity > 0.7 => "melancholic",
            CoreEmotions.Sadness when normalizedIntensity > 0.4 => "somber",
            CoreEmotions.Sadness => "subdued",
            
            CoreEmotions.Anger when normalizedIntensity > 0.7 => "irritated",
            CoreEmotions.Anger when normalizedIntensity > 0.4 => "annoyed",
            CoreEmotions.Anger => "stern",
            
            CoreEmotions.Fear when normalizedIntensity > 0.7 => "anxious",
            CoreEmotions.Fear when normalizedIntensity > 0.4 => "cautious",
            CoreEmotions.Fear => "concerned",
            
            CoreEmotions.Surprise when normalizedIntensity > 0.7 => "shocked",
            CoreEmotions.Surprise when normalizedIntensity > 0.4 => "astonished",
            CoreEmotions.Surprise => "curious",
            
            CoreEmotions.Disgust when normalizedIntensity > 0.7 => "repulsed",
            CoreEmotions.Disgust when normalizedIntensity > 0.4 => "disapproving",
            CoreEmotions.Disgust => "skeptical",
            
            _ => "neutral"
        };
    }
    
    private List<string> GetEmojisForState(EmotionalState state)
    {
        var emojis = new List<string>();
        string dominantEmotion = state.GetDominantEmotion();
        double intensity = state.GetEmotion(dominantEmotion) / 100.0;
        
        // Only add emojis if intensity is significant
        if (intensity < 0.4) return emojis;
        
        // Add emoji based on dominant emotion
        switch (dominantEmotion)
        {
            case CoreEmotions.Joy:
                emojis.Add(intensity > 0.7 ? "😄" : "🙂");
                break;
            case CoreEmotions.Sadness:
                emojis.Add(intensity > 0.7 ? "😢" : "😞");
                break;
            case CoreEmotions.Anger:
                emojis.Add(intensity > 0.7 ? "😠" : "😒");
                break;
            case CoreEmotions.Fear:
                emojis.Add(intensity > 0.7 ? "😨" : "😟");
                break;
            case CoreEmotions.Surprise:
                emojis.Add(intensity > 0.7 ? "😲" : "😮");
                break;
            case CoreEmotions.Disgust:
                emojis.Add(intensity > 0.7 ? "🤢" : "😕");
                break;
        }
        
        return emojis;
    }
    
    private List<string> GetModifiersForState(EmotionalState state)
    {
        var modifiers = new List<string>();
        double arousal = state.GetArousal();
        
        // Add modifiers based on arousal level
        if (arousal > 0.7)
        {
            modifiers.Add("speak energetically");
            modifiers.Add("use exclamations");
        }
        else if (arousal < 0.3)
        {
            modifiers.Add("speak calmly");
            modifiers.Add("use measured language");
        }
        
        // Add modifiers based on valence
        if (state.IsPositive)
        {
            modifiers.Add("be encouraging");
        }
        else if (state.IsNegative)
        {
            modifiers.Add("be reserved");
        }
        
        return modifiers;
    }
}
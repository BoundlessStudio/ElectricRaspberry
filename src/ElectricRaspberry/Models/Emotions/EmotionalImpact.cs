namespace ElectricRaspberry.Models.Emotions;

/// <summary>
/// Represents the impact of an emotional trigger on the bot's emotional state
/// </summary>
public class EmotionalImpact
{
    /// <summary>
    /// Changes to apply to emotions
    /// </summary>
    public Dictionary<string, double> Changes { get; set; } = new();
    
    /// <summary>
    /// Overall significance of this emotional impact (0-1)
    /// </summary>
    public double Significance { get; set; }
    
    /// <summary>
    /// Duration of the impact in minutes
    /// </summary>
    public double Duration { get; set; }
    
    /// <summary>
    /// Related emotional trigger
    /// </summary>
    public EmotionalTrigger Trigger { get; set; }
    
    /// <summary>
    /// Intensity of this emotional impact (0-1)
    /// </summary>
    public double Intensity => Significance;
    
    /// <summary>
    /// Creates a new emotional impact from a trigger
    /// </summary>
    public EmotionalImpact(EmotionalTrigger trigger)
    {
        Trigger = trigger;
        
        // Copy emotion changes from trigger
        foreach (var change in trigger.EmotionChanges)
        {
            Changes[change.Key] = change.Value;
        }
        
        // Default significance based on trigger intensity
        Significance = trigger.Intensity;
        
        // Default duration based on significance
        Duration = 5 + (20 * Significance);  // 5-25 minutes
    }
    
    /// <summary>
    /// Gets the dominant emotion from this impact
    /// </summary>
    public CoreEmotions GetDominantEmotion()
    {
        if (Changes.Count == 0)
        {
            return CoreEmotions.Neutral;
        }
        
        var maxChange = Changes.OrderByDescending(c => Math.Abs(c.Value)).First();
        
        // Try to parse the emotion as a CoreEmotions enum
        if (Enum.TryParse<CoreEmotions>(maxChange.Key, out var coreEmotion))
        {
            return coreEmotion;
        }
        
        return CoreEmotions.Neutral;
    }
}
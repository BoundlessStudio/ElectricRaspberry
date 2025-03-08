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
}
namespace ElectricRaspberry.Models.Emotions;

/// <summary>
/// Represents an event that can trigger an emotional response
/// </summary>
public class EmotionalTrigger
{
    /// <summary>
    /// Type of the emotional trigger
    /// </summary>
    public EmotionalTriggerType Type { get; set; }
    
    /// <summary>
    /// Intensity of the trigger (0-1)
    /// </summary>
    public double Intensity { get; set; }
    
    /// <summary>
    /// Source of the trigger (user ID, channel ID, etc.)
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the related content (message ID, etc.)
    /// </summary>
    public string ContentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Specific emotions this trigger affects
    /// </summary>
    public Dictionary<string, double> EmotionChanges { get; set; } = new();
    
    /// <summary>
    /// Additional context about the trigger
    /// </summary>
    public string Context { get; set; } = string.Empty;
    
    /// <summary>
    /// When the trigger occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
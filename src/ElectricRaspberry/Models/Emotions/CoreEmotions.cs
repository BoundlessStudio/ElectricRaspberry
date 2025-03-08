namespace ElectricRaspberry.Models.Emotions;

/// <summary>
/// Defines the core primary emotions used in the emotional system
/// </summary>
public enum CoreEmotions
{
    Neutral = 0,
    Joy,
    Sadness,
    Anger,
    Fear,
    Surprise,
    Disgust
}

/// <summary>
/// Extension methods for handling core emotions
/// </summary>
public static class CoreEmotionsExtensions
{
    /// <summary>
    /// Gets all core emotion names
    /// </summary>
    public static IEnumerable<string> AllEmotions => Enum.GetNames(typeof(CoreEmotions))
        .Where(name => name != nameof(CoreEmotions.Neutral));
        
    /// <summary>
    /// Converts enum value to string
    /// </summary>
    public static string AsString(this CoreEmotions emotion)
    {
        return emotion.ToString();
    }
}
namespace ElectricRaspberry.Models.Emotions;

/// <summary>
/// Defines the core primary emotions used in the emotional system
/// </summary>
public static class CoreEmotions
{
    public const string Joy = "Joy";
    public const string Sadness = "Sadness";
    public const string Anger = "Anger";
    public const string Fear = "Fear";
    public const string Surprise = "Surprise";
    public const string Disgust = "Disgust";
    
    /// <summary>
    /// Gets all core emotion names
    /// </summary>
    public static IEnumerable<string> All => new[] { Joy, Sadness, Anger, Fear, Surprise, Disgust };
}
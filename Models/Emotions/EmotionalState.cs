namespace ElectricRaspberry.Models.Emotions;

/// <summary>
/// Represents the bot's current emotional state with a collection of emotions and their intensities
/// </summary>
public class EmotionalState
{
    /// <summary>
    /// Dictionary of emotions and their current intensity values (0-100)
    /// </summary>
    public Dictionary<string, double> Emotions { get; private set; } = new();
    
    public EmotionalState()
    {
        // Initialize with basic emotions at neutral values
        Emotions[CoreEmotions.Joy] = 50;
        Emotions[CoreEmotions.Sadness] = 50;
        Emotions[CoreEmotions.Anger] = 50;
        Emotions[CoreEmotions.Fear] = 50;
        Emotions[CoreEmotions.Surprise] = 50;
        Emotions[CoreEmotions.Disgust] = 50;
    }
    
    /// <summary>
    /// Adjusts the intensity of a specific emotion
    /// </summary>
    /// <param name="emotion">The emotion to adjust</param>
    /// <param name="change">The change in intensity (-100 to 100)</param>
    public void AdjustEmotion(string emotion, double change)
    {
        if (!Emotions.ContainsKey(emotion))
        {
            Emotions[emotion] = 50; // Start at neutral if not defined
        }
        
        Emotions[emotion] = Math.Clamp(Emotions[emotion] + change, 0, 100);
    }
    
    /// <summary>
    /// Sets the intensity of a specific emotion directly
    /// </summary>
    /// <param name="emotion">The emotion to set</param>
    /// <param name="value">The intensity value (0-100)</param>
    public void SetEmotion(string emotion, double value)
    {
        Emotions[emotion] = Math.Clamp(value, 0, 100);
    }
    
    /// <summary>
    /// Gets the current intensity of a specific emotion
    /// </summary>
    /// <param name="emotion">The emotion to get</param>
    /// <returns>The intensity value (0-100)</returns>
    public double GetEmotion(string emotion)
    {
        return Emotions.TryGetValue(emotion, out var value) ? value : 50;
    }
    
    /// <summary>
    /// Gets the most dominant emotion
    /// </summary>
    /// <returns>The name of the dominant emotion</returns>
    public string GetDominantEmotion()
    {
        return Emotions.OrderByDescending(e => e.Value).First().Key;
    }
    
    /// <summary>
    /// Gets the overall valence (positive/negative) of the emotional state
    /// </summary>
    /// <returns>Value from -1 (negative) to 1 (positive)</returns>
    public double GetValence()
    {
        // Positive emotions increase valence, negative emotions decrease it
        double positive = GetEmotion(CoreEmotions.Joy) / 100.0;
        double negative = (GetEmotion(CoreEmotions.Sadness) + 
                          GetEmotion(CoreEmotions.Anger) + 
                          GetEmotion(CoreEmotions.Fear) + 
                          GetEmotion(CoreEmotions.Disgust)) / 400.0;
        
        return positive - negative;
    }
    
    /// <summary>
    /// Gets the overall arousal (intensity/energy) of the emotional state
    /// </summary>
    /// <returns>Value from 0 (calm) to 1 (excited)</returns>
    public double GetArousal()
    {
        // High energy emotions increase arousal
        double arousalSum = (GetEmotion(CoreEmotions.Joy) + 
                            GetEmotion(CoreEmotions.Anger) + 
                            GetEmotion(CoreEmotions.Fear) + 
                            GetEmotion(CoreEmotions.Surprise)) / 400.0;
        
        return arousalSum;
    }
    
    /// <summary>
    /// Determines if the overall emotional state is positive
    /// </summary>
    public bool IsPositive => GetValence() > 0;
    
    /// <summary>
    /// Determines if the overall emotional state is negative
    /// </summary>
    public bool IsNegative => GetValence() < 0;
    
    /// <summary>
    /// Determines if the overall emotional state is highly aroused/energetic
    /// </summary>
    public bool IsEnergetic => GetArousal() > 0.6;
    
    /// <summary>
    /// Determines if the overall emotional state is calm/relaxed
    /// </summary>
    public bool IsCalm => GetArousal() < 0.4;
}
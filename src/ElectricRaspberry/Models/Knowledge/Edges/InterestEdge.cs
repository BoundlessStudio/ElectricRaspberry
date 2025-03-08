using Newtonsoft.Json;

namespace ElectricRaspberry.Models.Knowledge.Edges;

/// <summary>
/// Represents an interest connection between a person and a topic
/// </summary>
public class InterestEdge : GraphEdge
{
    /// <summary>
    /// Label for interest edges
    /// </summary>
    public const string EdgeLabel = "Interest";
    
    /// <summary>
    /// Level of interest (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "level")]
    public double Level { get; set; } = 0.5;
    
    /// <summary>
    /// When the interest was first observed
    /// </summary>
    [JsonProperty(PropertyName = "firstObservedAt")]
    public DateTime FirstObservedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the interest was last observed
    /// </summary>
    [JsonProperty(PropertyName = "lastObservedAt")]
    public DateTime LastObservedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// How many times the interest has been observed
    /// </summary>
    [JsonProperty(PropertyName = "observationCount")]
    public int ObservationCount { get; set; } = 1;
    
    /// <summary>
    /// Source of the interest observation (conversation, etc.)
    /// </summary>
    [JsonProperty(PropertyName = "source")]
    public string Source { get; set; }
    
    /// <summary>
    /// Creates a new interest edge
    /// </summary>
    public InterestEdge() : base(EdgeLabel, string.Empty, string.Empty)
    {
    }
    
    /// <summary>
    /// Creates a new interest edge between a person and a topic
    /// </summary>
    public InterestEdge(string personId, string topicId, string source) 
        : base(EdgeLabel, personId, topicId)
    {
        Source = source;
    }
    
    /// <summary>
    /// Records a new observation of the interest
    /// </summary>
    /// <param name="source">Source of the observation</param>
    public void RecordObservation(string source)
    {
        ObservationCount++;
        LastObservedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Source = source;
        
        // Increase interest level (with diminishing returns)
        Level = Math.Min(1.0, Level + (0.1 / Math.Sqrt(ObservationCount)));
    }
    
    /// <summary>
    /// Sets the interest level explicitly
    /// </summary>
    /// <param name="level">The new interest level (0-1)</param>
    public void SetInterestLevel(double level)
    {
        Level = Math.Clamp(level, 0, 1);
        UpdatedAt = DateTime.UtcNow;
    }
}
using Newtonsoft.Json;

namespace ElectricRaspberry.Models.Knowledge.Edges;

/// <summary>
/// Represents a relationship between two people in the knowledge graph
/// </summary>
public class RelationshipEdge : GraphEdge
{
    /// <summary>
    /// Label for relationship edges
    /// </summary>
    public const string EdgeLabel = "Relationship";
    
    /// <summary>
    /// Type of relationship (friend, acquaintance, etc.)
    /// </summary>
    [JsonProperty(PropertyName = "type")]
    public string Type { get; set; }
    
    /// <summary>
    /// How long the relationship has existed (in days)
    /// </summary>
    [JsonProperty(PropertyName = "durationDays")]
    public int DurationDays { get; set; }
    
    /// <summary>
    /// How frequently the users interact (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "interactionFrequency")]
    public double InteractionFrequency { get; set; } = 0.0;
    
    /// <summary>
    /// When the users last interacted
    /// </summary>
    [JsonProperty(PropertyName = "lastInteractionAt")]
    public DateTime LastInteractionAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// How many times the users have interacted
    /// </summary>
    [JsonProperty(PropertyName = "interactionCount")]
    public int InteractionCount { get; set; } = 0;
    
    /// <summary>
    /// Notes about the relationship
    /// </summary>
    [JsonProperty(PropertyName = "notes")]
    public string Notes { get; set; }
    
    /// <summary>
    /// Creates a new relationship edge
    /// </summary>
    public RelationshipEdge() : base(EdgeLabel, string.Empty, string.Empty)
    {
    }
    
    /// <summary>
    /// Creates a new relationship edge with basic details
    /// </summary>
    public RelationshipEdge(string sourcePersonId, string targetPersonId, string type) 
        : base(EdgeLabel, sourcePersonId, targetPersonId)
    {
        Type = type;
        DurationDays = 0;
    }
    
    /// <summary>
    /// Records an interaction between the two people
    /// </summary>
    public void RecordInteraction()
    {
        InteractionCount++;
        LastInteractionAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        // Update duration
        DurationDays = (int)(DateTime.UtcNow - CreatedAt).TotalDays;
        
        // Recalculate interaction frequency (simple decay function)
        if (DurationDays > 0)
        {
            InteractionFrequency = Math.Min(1.0, InteractionCount / (5 + DurationDays));
        }
    }
    
    /// <summary>
    /// Upgrades the relationship type
    /// </summary>
    /// <param name="newType">The new relationship type</param>
    public void UpgradeRelationship(string newType)
    {
        Type = newType;
        UpdatedAt = DateTime.UtcNow;
    }
}
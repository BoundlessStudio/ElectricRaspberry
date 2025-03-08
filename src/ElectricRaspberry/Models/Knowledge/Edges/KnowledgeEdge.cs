using Newtonsoft.Json;

namespace ElectricRaspberry.Models.Knowledge.Edges;

/// <summary>
/// Represents a knowledge connection between a memory and a topic
/// </summary>
public class KnowledgeEdge : GraphEdge
{
    /// <summary>
    /// Label for knowledge edges
    /// </summary>
    public const string EdgeLabel = "Knowledge";
    
    /// <summary>
    /// Relevance of the memory to the topic (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "relevance")]
    public double Relevance { get; set; } = 0.5;
    
    /// <summary>
    /// Confidence in the connection (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "confidence")]
    public double Confidence { get; set; } = 0.5;
    
    /// <summary>
    /// Source of the knowledge connection
    /// </summary>
    [JsonProperty(PropertyName = "source")]
    public string Source { get; set; }
    
    /// <summary>
    /// How many times this knowledge has been reinforced
    /// </summary>
    [JsonProperty(PropertyName = "reinforcementCount")]
    public int ReinforcementCount { get; set; } = 0;
    
    /// <summary>
    /// Notes about the knowledge connection
    /// </summary>
    [JsonProperty(PropertyName = "notes")]
    public string Notes { get; set; }
    
    /// <summary>
    /// Creates a new knowledge edge
    /// </summary>
    public KnowledgeEdge() : base(EdgeLabel, string.Empty, string.Empty)
    {
    }
    
    /// <summary>
    /// Creates a new knowledge edge between a memory and a topic
    /// </summary>
    public KnowledgeEdge(string memoryId, string topicId, string source, double relevance = 0.5, double confidence = 0.5) 
        : base(EdgeLabel, memoryId, topicId)
    {
        Source = source;
        Relevance = relevance;
        Confidence = confidence;
    }
    
    /// <summary>
    /// Reinforces the knowledge connection
    /// </summary>
    /// <param name="relevanceChange">Optional change to relevance (-1 to 1)</param>
    /// <param name="confidenceChange">Optional change to confidence (-1 to 1)</param>
    public void Reinforce(double relevanceChange = 0, double confidenceChange = 0.1)
    {
        ReinforcementCount++;
        UpdatedAt = DateTime.UtcNow;
        
        // Update relevance and confidence
        Relevance = Math.Clamp(Relevance + relevanceChange, 0, 1);
        Confidence = Math.Clamp(Confidence + confidenceChange, 0, 1);
    }
    
    /// <summary>
    /// Updates the notes about the knowledge connection
    /// </summary>
    /// <param name="notes">The new notes</param>
    public void UpdateNotes(string notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
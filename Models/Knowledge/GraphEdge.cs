using Newtonsoft.Json;

namespace ElectricRaspberry.Models.Knowledge;

/// <summary>
/// Base class for edges in the knowledge graph
/// </summary>
public abstract class GraphEdge
{
    /// <summary>
    /// Unique ID for the edge
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Edge label for Gremlin queries
    /// </summary>
    [JsonProperty(PropertyName = "label")]
    public string Label { get; set; }
    
    /// <summary>
    /// ID of the source vertex
    /// </summary>
    [JsonProperty(PropertyName = "outV")]
    public string SourceId { get; set; }
    
    /// <summary>
    /// ID of the target vertex
    /// </summary>
    [JsonProperty(PropertyName = "inV")]
    public string TargetId { get; set; }
    
    /// <summary>
    /// Edge strength or weight (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "strength")]
    public double Strength { get; set; } = 1.0;
    
    /// <summary>
    /// When the edge was created
    /// </summary>
    [JsonProperty(PropertyName = "createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the edge was last modified
    /// </summary>
    [JsonProperty(PropertyName = "updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ETag for optimistic concurrency control
    /// </summary>
    [JsonProperty(PropertyName = "_etag", NullValueHandling = NullValueHandling.Ignore)]
    public string ETag { get; set; }
    
    /// <summary>
    /// Creates a new graph edge
    /// </summary>
    /// <param name="label">The edge label</param>
    /// <param name="sourceId">The source vertex ID</param>
    /// <param name="targetId">The target vertex ID</param>
    protected GraphEdge(string label, string sourceId, string targetId)
    {
        Label = label;
        SourceId = sourceId;
        TargetId = targetId;
    }
}
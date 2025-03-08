using Newtonsoft.Json;

namespace ElectricRaspberry.Models.Knowledge;

/// <summary>
/// Base class for vertices in the knowledge graph
/// </summary>
public abstract class GraphVertex
{
    /// <summary>
    /// Unique ID for the vertex
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Vertex label for Gremlin queries
    /// </summary>
    [JsonProperty(PropertyName = "label")]
    public string Label { get; set; }
    
    /// <summary>
    /// When the vertex was created
    /// </summary>
    [JsonProperty(PropertyName = "createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the vertex was last modified
    /// </summary>
    [JsonProperty(PropertyName = "updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ETag for optimistic concurrency control
    /// </summary>
    [JsonProperty(PropertyName = "_etag", NullValueHandling = NullValueHandling.Ignore)]
    public string ETag { get; set; }
    
    /// <summary>
    /// Vertex relevance factor (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "relevance")]
    public double Relevance { get; set; } = 1.0;
    
    /// <summary>
    /// Creates a new graph vertex
    /// </summary>
    /// <param name="label">The vertex label</param>
    protected GraphVertex(string label)
    {
        Label = label;
    }
}
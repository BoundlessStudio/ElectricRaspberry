using Newtonsoft.Json;

namespace ElectricRaspberry.Models.Knowledge.Vertices;

/// <summary>
/// Represents a memory (conversation, event, etc.) in the knowledge graph
/// </summary>
public class MemoryVertex : GraphVertex
{
    /// <summary>
    /// Label for memory vertices
    /// </summary>
    public const string VertexLabel = "Memory";
    
    /// <summary>
    /// Title or summary of the memory
    /// </summary>
    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; }
    
    /// <summary>
    /// Content of the memory
    /// </summary>
    [JsonProperty(PropertyName = "content")]
    public string Content { get; set; }
    
    /// <summary>
    /// Type of memory (conversation, event, fact, etc.)
    /// </summary>
    [JsonProperty(PropertyName = "type")]
    public string Type { get; set; }
    
    /// <summary>
    /// Source of the memory (conversation ID, channel ID, etc.)
    /// </summary>
    [JsonProperty(PropertyName = "source")]
    public string Source { get; set; }
    
    /// <summary>
    /// When the memory occurred
    /// </summary>
    [JsonProperty(PropertyName = "occurredAt")]
    public DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// Importance of the memory (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "importance")]
    public double Importance { get; set; } = 0.5;
    
    /// <summary>
    /// How many times this memory has been recalled
    /// </summary>
    [JsonProperty(PropertyName = "recallCount")]
    public int RecallCount { get; set; } = 0;
    
    /// <summary>
    /// When this memory was last recalled
    /// </summary>
    [JsonProperty(PropertyName = "lastRecalledAt")]
    public DateTime? LastRecalledAt { get; set; }
    
    /// <summary>
    /// Keywords associated with this memory
    /// </summary>
    [JsonProperty(PropertyName = "keywords")]
    public List<string> Keywords { get; set; } = new();
    
    /// <summary>
    /// Creates a new memory vertex
    /// </summary>
    public MemoryVertex() : base(VertexLabel)
    {
    }
    
    /// <summary>
    /// Creates a new memory vertex with basic details
    /// </summary>
    public MemoryVertex(string title, string content, string type, string source, DateTime occurredAt) : base(VertexLabel)
    {
        Title = title;
        Content = content;
        Type = type;
        Source = source;
        OccurredAt = occurredAt;
    }
    
    /// <summary>
    /// Adds a keyword to the memory
    /// </summary>
    /// <param name="keyword">The keyword to add</param>
    public void AddKeyword(string keyword)
    {
        if (!Keywords.Contains(keyword))
        {
            Keywords.Add(keyword);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Records a recall of this memory
    /// </summary>
    public void RecordRecall()
    {
        RecallCount++;
        LastRecalledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Sets the importance of the memory
    /// </summary>
    /// <param name="importance">The importance value (0-1)</param>
    public void SetImportance(double importance)
    {
        Importance = Math.Clamp(importance, 0, 1);
        UpdatedAt = DateTime.UtcNow;
    }
}
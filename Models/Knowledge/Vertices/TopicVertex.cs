using Newtonsoft.Json;

namespace ElectricRaspberry.Models.Knowledge.Vertices;

/// <summary>
/// Represents a topic or subject in the knowledge graph
/// </summary>
public class TopicVertex : GraphVertex
{
    /// <summary>
    /// Label for topic vertices
    /// </summary>
    public const string VertexLabel = "Topic";
    
    /// <summary>
    /// Name of the topic
    /// </summary>
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }
    
    /// <summary>
    /// Description of the topic
    /// </summary>
    [JsonProperty(PropertyName = "description")]
    public string Description { get; set; }
    
    /// <summary>
    /// Keywords associated with this topic
    /// </summary>
    [JsonProperty(PropertyName = "keywords")]
    public List<string> Keywords { get; set; } = new();
    
    /// <summary>
    /// How frequently this topic appears in conversations (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "frequency")]
    public double Frequency { get; set; } = 0.0;
    
    /// <summary>
    /// When this topic was last mentioned
    /// </summary>
    [JsonProperty(PropertyName = "lastMentionedAt")]
    public DateTime LastMentionedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// How many times this topic has been mentioned
    /// </summary>
    [JsonProperty(PropertyName = "mentionCount")]
    public int MentionCount { get; set; } = 0;
    
    /// <summary>
    /// Creates a new topic vertex
    /// </summary>
    public TopicVertex() : base(VertexLabel)
    {
    }
    
    /// <summary>
    /// Creates a new topic vertex with basic details
    /// </summary>
    public TopicVertex(string name, string description = "") : base(VertexLabel)
    {
        Name = name;
        Description = description;
    }
    
    /// <summary>
    /// Adds a keyword to the topic
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
    /// Records a mention of this topic
    /// </summary>
    public void RecordMention()
    {
        MentionCount++;
        LastMentionedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        // Recalculate frequency (simple decay function)
        double daysSinceCreation = (DateTime.UtcNow - CreatedAt).TotalDays;
        if (daysSinceCreation > 0)
        {
            Frequency = Math.Min(1.0, MentionCount / (10 + daysSinceCreation));
        }
    }
    
    /// <summary>
    /// Updates the topic description
    /// </summary>
    /// <param name="description">The new description</param>
    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
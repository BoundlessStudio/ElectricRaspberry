using Newtonsoft.Json;

namespace ElectricRaspberry.Models.Knowledge.Vertices;

/// <summary>
/// Represents a person (user) in the knowledge graph
/// </summary>
public class PersonVertex : GraphVertex
{
    /// <summary>
    /// Label for person vertices
    /// </summary>
    public const string VertexLabel = "Person";
    
    /// <summary>
    /// Discord user ID
    /// </summary>
    [JsonProperty(PropertyName = "discordUserId")]
    public ulong DiscordUserId { get; set; }
    
    /// <summary>
    /// Discord username
    /// </summary>
    [JsonProperty(PropertyName = "username")]
    public string Username { get; set; }
    
    /// <summary>
    /// Discord display name
    /// </summary>
    [JsonProperty(PropertyName = "displayName")]
    public string DisplayName { get; set; }
    
    /// <summary>
    /// Known information about the person
    /// </summary>
    [JsonProperty(PropertyName = "knownInfo")]
    public Dictionary<string, string> KnownInfo { get; set; } = new();
    
    /// <summary>
    /// Relationship strength with the bot (0-1)
    /// </summary>
    [JsonProperty(PropertyName = "relationshipStrength")]
    public double RelationshipStrength { get; set; } = 0.0;
    
    /// <summary>
    /// Topics the person has shown interest in
    /// </summary>
    [JsonProperty(PropertyName = "interests")]
    public List<string> Interests { get; set; } = new();
    
    /// <summary>
    /// When the person was last interacted with
    /// </summary>
    [JsonProperty(PropertyName = "lastInteractionAt")]
    public DateTime LastInteractionAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Creates a new person vertex
    /// </summary>
    public PersonVertex() : base(VertexLabel)
    {
    }
    
    /// <summary>
    /// Creates a new person vertex with basic details
    /// </summary>
    public PersonVertex(ulong discordUserId, string username, string displayName) : base(VertexLabel)
    {
        DiscordUserId = discordUserId;
        Username = username;
        DisplayName = displayName;
    }
    
    /// <summary>
    /// Updates the known information about a person
    /// </summary>
    /// <param name="key">The information key</param>
    /// <param name="value">The information value</param>
    public void UpdateKnownInfo(string key, string value)
    {
        KnownInfo[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Adds an interest topic for the person
    /// </summary>
    /// <param name="interest">The interest to add</param>
    public void AddInterest(string interest)
    {
        if (!Interests.Contains(interest))
        {
            Interests.Add(interest);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Updates the relationship strength
    /// </summary>
    /// <param name="change">The change in relationship strength (-1 to 1)</param>
    public void UpdateRelationship(double change)
    {
        RelationshipStrength = Math.Clamp(RelationshipStrength + change, 0, 1);
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Records a new interaction with this person
    /// </summary>
    public void RecordInteraction()
    {
        LastInteractionAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
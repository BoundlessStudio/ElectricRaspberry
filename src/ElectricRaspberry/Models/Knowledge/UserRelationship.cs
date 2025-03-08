namespace ElectricRaspberry.Models.Knowledge;

/// <summary>
/// Represents a relationship with a user
/// </summary>
public class UserRelationship
{
    /// <summary>
    /// The Discord user ID
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// The username
    /// </summary>
    public string Username { get; set; }
    
    /// <summary>
    /// The display name
    /// </summary>
    public string DisplayName { get; set; }
    
    /// <summary>
    /// The relationship type (friend, acquaintance, etc.)
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    /// The relationship strength (0-1)
    /// </summary>
    public double Strength { get; set; }
    
    /// <summary>
    /// How long the relationship has existed (in days)
    /// </summary>
    public int DurationDays { get; set; }
    
    /// <summary>
    /// How many times the users have interacted
    /// </summary>
    public int InteractionCount { get; set; }
    
    /// <summary>
    /// When the users last interacted
    /// </summary>
    public DateTime LastInteractionAt { get; set; }
    
    /// <summary>
    /// List of known interests
    /// </summary>
    public List<string> Interests { get; set; } = new();
    
    /// <summary>
    /// Known information about the user
    /// </summary>
    public Dictionary<string, string> KnownInfo { get; set; } = new();
    
    /// <summary>
    /// Creates a new user relationship
    /// </summary>
    public UserRelationship(ulong userId, string username, string displayName, string type, double strength)
    {
        UserId = userId;
        Username = username;
        DisplayName = displayName;
        Type = type;
        Strength = strength;
        LastInteractionAt = DateTime.UtcNow;
    }
}
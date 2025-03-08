namespace ElectricRaspberry.Models.Regulation;

/// <summary>
/// Represents the progression stages in a relationship
/// </summary>
public enum RelationshipStage
{
    /// <summary>
    /// New relationship with minimal interaction
    /// </summary>
    Stranger = 0,
    
    /// <summary>
    /// Beginning to know each other with some interaction
    /// </summary>
    Acquaintance = 1,
    
    /// <summary>
    /// Regular interaction and some shared interests
    /// </summary>
    Casual = 2,
    
    /// <summary>
    /// Frequent interaction and established rapport
    /// </summary>
    Friend = 3,
    
    /// <summary>
    /// Close relationship with high engagement
    /// </summary>
    CloseFriend = 4
}
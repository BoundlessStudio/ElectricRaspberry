namespace ElectricRaspberry.Models.Emotions;

/// <summary>
/// Types of events that can trigger emotional responses
/// </summary>
public enum EmotionalTriggerType
{
    /// <summary>
    /// A user message directed at the bot
    /// </summary>
    DirectMessage,
    
    /// <summary>
    /// A positive interaction with a user
    /// </summary>
    PositiveInteraction,
    
    /// <summary>
    /// A negative interaction with a user
    /// </summary>
    NegativeInteraction,
    
    /// <summary>
    /// Recognition or praise from a user
    /// </summary>
    Praise,
    
    /// <summary>
    /// Criticism or rebuke from a user
    /// </summary>
    Criticism,
    
    /// <summary>
    /// An unexpected event or surprise
    /// </summary>
    UnexpectedEvent,
    
    /// <summary>
    /// A user leaving the server
    /// </summary>
    UserDeparture,
    
    /// <summary>
    /// A new user joining the server
    /// </summary>
    UserJoin,
    
    /// <summary>
    /// Success in completing a task
    /// </summary>
    Success,
    
    /// <summary>
    /// Failure to complete a task
    /// </summary>
    Failure,
    
    /// <summary>
    /// Conflict or argument in a channel
    /// </summary>
    Conflict,
    
    /// <summary>
    /// Content that violates server rules
    /// </summary>
    RuleViolation,
    
    /// <summary>
    /// Significant server event
    /// </summary>
    ServerEvent,
    
    /// <summary>
    /// Low stamina condition
    /// </summary>
    Fatigue,
    
    /// <summary>
    /// Triggered by internal processes
    /// </summary>
    Internal
}
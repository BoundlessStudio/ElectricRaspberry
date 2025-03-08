using ElectricRaspberry.Models.Knowledge;
using ElectricRaspberry.Models.Knowledge.Edges;
using ElectricRaspberry.Models.Knowledge.Vertices;

namespace ElectricRaspberry.Services;

/// <summary>
/// Service for managing the knowledge graph and memory
/// </summary>
public interface IKnowledgeService
{
    // Person operations
    
    /// <summary>
    /// Gets a person by Discord user ID
    /// </summary>
    /// <param name="discordUserId">The Discord user ID</param>
    /// <returns>The person if found, null otherwise</returns>
    Task<PersonVertex> GetPersonAsync(ulong discordUserId);
    
    /// <summary>
    /// Adds or updates a person in the knowledge graph
    /// </summary>
    /// <param name="person">The person to add or update</param>
    /// <returns>The added or updated person</returns>
    Task<PersonVertex> UpsertPersonAsync(PersonVertex person);
    
    /// <summary>
    /// Gets a user relationship
    /// </summary>
    /// <param name="userId">The Discord user ID</param>
    /// <returns>The relationship strength and other details</returns>
    Task<UserRelationship> GetUserRelationshipAsync(ulong userId);
    
    /// <summary>
    /// Updates a user relationship
    /// </summary>
    /// <param name="userId">The Discord user ID</param>
    /// <param name="relationshipType">The relationship type</param>
    /// <param name="strength">The new relationship strength</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateUserRelationshipAsync(ulong userId, string relationshipType, double strength);
    
    // Topic operations
    
    /// <summary>
    /// Gets a topic by name
    /// </summary>
    /// <param name="name">The topic name</param>
    /// <returns>The topic if found, null otherwise</returns>
    Task<TopicVertex> GetTopicAsync(string name);
    
    /// <summary>
    /// Gets topics by keywords
    /// </summary>
    /// <param name="keywords">The keywords to search for</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>List of matching topics</returns>
    Task<IEnumerable<TopicVertex>> SearchTopicsAsync(string[] keywords, int maxResults = 10);
    
    /// <summary>
    /// Adds or updates a topic in the knowledge graph
    /// </summary>
    /// <param name="topic">The topic to add or update</param>
    /// <returns>The added or updated topic</returns>
    Task<TopicVertex> UpsertTopicAsync(TopicVertex topic);
    
    // Memory operations
    
    /// <summary>
    /// Creates a new memory
    /// </summary>
    /// <param name="title">The memory title</param>
    /// <param name="content">The memory content</param>
    /// <param name="type">The memory type</param>
    /// <param name="source">The memory source</param>
    /// <param name="occurredAt">When the memory occurred</param>
    /// <returns>The created memory</returns>
    Task<MemoryVertex> CreateMemoryAsync(string title, string content, string type, string source, DateTime occurredAt);
    
    /// <summary>
    /// Searches for memories
    /// </summary>
    /// <param name="keywords">Keywords to search for</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>List of matching memories</returns>
    Task<IEnumerable<MemoryVertex>> SearchMemoriesAsync(string[] keywords, int maxResults = 10);
    
    /// <summary>
    /// Gets recent memories
    /// </summary>
    /// <param name="count">Maximum number of memories to return</param>
    /// <returns>List of recent memories</returns>
    Task<IEnumerable<MemoryVertex>> GetRecentMemoriesAsync(int count = 10);
    
    // Relationships between entities
    
    /// <summary>
    /// Records an interaction between two people
    /// </summary>
    /// <param name="sourceUserId">The source user ID</param>
    /// <param name="targetUserId">The target user ID</param>
    /// <param name="type">The relationship type</param>
    /// <returns>The updated relationship</returns>
    Task<RelationshipEdge> RecordInteractionAsync(ulong sourceUserId, ulong targetUserId, string type);
    
    /// <summary>
    /// Records an interest for a person
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="topicName">The topic name</param>
    /// <param name="source">The interest source</param>
    /// <returns>The updated interest</returns>
    Task<InterestEdge> RecordInterestAsync(ulong userId, string topicName, string source);
    
    /// <summary>
    /// Connects a memory to a topic
    /// </summary>
    /// <param name="memoryId">The memory ID</param>
    /// <param name="topicName">The topic name</param>
    /// <param name="source">The knowledge source</param>
    /// <param name="relevance">The relevance (0-1)</param>
    /// <param name="confidence">The confidence (0-1)</param>
    /// <returns>The created knowledge edge</returns>
    Task<KnowledgeEdge> ConnectMemoryToTopicAsync(string memoryId, string topicName, string source, double relevance = 0.5, double confidence = 0.5);
    
    // Maintenance operations
    
    /// <summary>
    /// Performs maintenance on the knowledge graph
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task PerformGraphMaintenanceAsync();
}
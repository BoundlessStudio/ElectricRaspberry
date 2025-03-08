using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Knowledge;
using ElectricRaspberry.Models.Knowledge.Edges;
using ElectricRaspberry.Models.Knowledge.Vertices;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace ElectricRaspberry.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly ILogger<KnowledgeService> _logger;
    private readonly CosmosDbOptions _cosmosOptions;
    private readonly GremlinClient _gremlinClient;
    private readonly SemaphoreSlim _graphLock = new(1, 1);
    
    // Memory cache for frequently accessed entities
    private readonly ConcurrentDictionary<string, PersonVertex> _personCache = new();
    private readonly ConcurrentDictionary<string, TopicVertex> _topicCache = new();
    
    public KnowledgeService(
        ILogger<KnowledgeService> logger,
        IOptions<CosmosDbOptions> cosmosOptions)
    {
        _logger = logger;
        _cosmosOptions = cosmosOptions.Value;
        
        // Initialize Gremlin client
        var gremlinServer = new GremlinServer(
            _cosmosOptions.Endpoint,
            443,
            enableSsl: true,
            username: "/dbs/" + _cosmosOptions.Database + "/colls/" + _cosmosOptions.Container,
            password: _cosmosOptions.Key);
        
        _gremlinClient = new GremlinClient(gremlinServer);
        
        // Initialize the graph
        InitializeGraphAsync().Wait();
    }
    
    // Person operations
    
    public async Task<PersonVertex> GetPersonAsync(ulong discordUserId)
    {
        // Check cache first
        string cacheKey = discordUserId.ToString();
        if (_personCache.TryGetValue(cacheKey, out var cachedPerson))
        {
            return cachedPerson;
        }
        
        await _graphLock.WaitAsync();
        try
        {
            // Query for person by Discord ID
            var query = $"g.V().hasLabel('{PersonVertex.VertexLabel}').has('discordUserId', {discordUserId})";
            var results = await SubmitGremlinQueryWithRetryAsync<PersonVertex>(query);
            
            var person = results.FirstOrDefault();
            
            // Cache the result if found
            if (person != null)
            {
                _personCache[cacheKey] = person;
            }
            
            return person;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting person with Discord ID {DiscordUserId}", discordUserId);
            return null;
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task<PersonVertex> UpsertPersonAsync(PersonVertex person)
    {
        await _graphLock.WaitAsync();
        try
        {
            // Check if person exists
            PersonVertex existingPerson = null;
            if (person.DiscordUserId != 0)
            {
                existingPerson = await GetPersonAsync(person.DiscordUserId);
            }
            
            if (existingPerson != null)
            {
                // Update existing person
                person.Id = existingPerson.Id;
                person.CreatedAt = existingPerson.CreatedAt;
                person.UpdatedAt = DateTime.UtcNow;
                
                // Use ETag for optimistic concurrency
                person.ETag = existingPerson.ETag;
                
                // Update the vertex
                var updateQuery = $"g.V('{person.Id}').has('_etag', '{person.ETag}')";
                updateQuery += $".property('username', '{EscapeStringForGremlin(person.Username)}')";
                updateQuery += $".property('displayName', '{EscapeStringForGremlin(person.DisplayName)}')";
                updateQuery += $".property('relationshipStrength', {person.RelationshipStrength})";
                updateQuery += $".property('updatedAt', '{person.UpdatedAt:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(updateQuery);
                
                // Update known info
                foreach (var (key, value) in person.KnownInfo)
                {
                    var infoQuery = $"g.V('{person.Id}').property('knownInfo.{EscapeStringForGremlin(key)}', '{EscapeStringForGremlin(value)}')";
                    await SubmitGremlinQueryWithRetryAsync<dynamic>(infoQuery);
                }
                
                // Update interests
                var interestsQuery = $"g.V('{person.Id}').property('interests', {JsonConvert.SerializeObject(person.Interests)})";
                await SubmitGremlinQueryWithRetryAsync<dynamic>(interestsQuery);
            }
            else
            {
                // Create new person
                person.Id = Guid.NewGuid().ToString();
                person.CreatedAt = DateTime.UtcNow;
                person.UpdatedAt = DateTime.UtcNow;
                
                // Create the vertex
                var addQuery = $"g.addV('{PersonVertex.VertexLabel}').property('id', '{person.Id}')";
                addQuery += $".property('discordUserId', {person.DiscordUserId})";
                addQuery += $".property('username', '{EscapeStringForGremlin(person.Username)}')";
                addQuery += $".property('displayName', '{EscapeStringForGremlin(person.DisplayName)}')";
                addQuery += $".property('relationshipStrength', {person.RelationshipStrength})";
                addQuery += $".property('createdAt', '{person.CreatedAt:o}')";
                addQuery += $".property('updatedAt', '{person.UpdatedAt:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(addQuery);
                
                // Add known info
                foreach (var (key, value) in person.KnownInfo)
                {
                    var infoQuery = $"g.V('{person.Id}').property('knownInfo.{EscapeStringForGremlin(key)}', '{EscapeStringForGremlin(value)}')";
                    await SubmitGremlinQueryWithRetryAsync<dynamic>(infoQuery);
                }
                
                // Add interests
                var interestsQuery = $"g.V('{person.Id}').property('interests', {JsonConvert.SerializeObject(person.Interests)})";
                await SubmitGremlinQueryWithRetryAsync<dynamic>(interestsQuery);
            }
            
            // Refresh the data
            var refreshQuery = $"g.V('{person.Id}')";
            var refreshResults = await SubmitGremlinQueryWithRetryAsync<PersonVertex>(refreshQuery);
            var refreshedPerson = refreshResults.FirstOrDefault();
            
            // Update cache
            if (refreshedPerson != null)
            {
                _personCache[person.DiscordUserId.ToString()] = refreshedPerson;
            }
            
            return refreshedPerson ?? person;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting person with Discord ID {DiscordUserId}", person.DiscordUserId);
            return person;
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task<UserRelationship> GetUserRelationshipAsync(ulong userId)
    {
        // Get the person
        var person = await GetPersonAsync(userId);
        if (person == null)
        {
            return new UserRelationship(userId, "Unknown", "Unknown", "unknown", 0);
        }
        
        // Get relationship edges
        var relationshipQuery = $"g.V().hasLabel('{PersonVertex.VertexLabel}').has('discordUserId', {userId}).bothE('{RelationshipEdge.EdgeLabel}')";
        var relationships = await SubmitGremlinQueryWithRetryAsync<RelationshipEdge>(relationshipQuery);
        
        // Get interest edges
        var interestQuery = $"g.V().hasLabel('{PersonVertex.VertexLabel}').has('discordUserId', {userId}).outE('{InterestEdge.EdgeLabel}')";
        var interests = await SubmitGremlinQueryWithRetryAsync<InterestEdge>(interestQuery);
        
        // Get topics from interests
        var topicIds = interests.Select(i => i.TargetId).ToList();
        var topics = new List<TopicVertex>();
        
        foreach (var topicId in topicIds)
        {
            var topicQuery = $"g.V('{topicId}')";
            var topicResults = await SubmitGremlinQueryWithRetryAsync<TopicVertex>(topicQuery);
            var topic = topicResults.FirstOrDefault();
            
            if (topic != null)
            {
                topics.Add(topic);
            }
        }
        
        // Find strongest relationship
        var relationship = relationships.OrderByDescending(r => r.Strength).FirstOrDefault();
        
        // Build user relationship
        var userRelationship = new UserRelationship(
            userId,
            person.Username,
            person.DisplayName,
            relationship?.Type ?? "acquaintance",
            person.RelationshipStrength);
        
        // Add additional details
        userRelationship.InteractionCount = relationship?.InteractionCount ?? 0;
        userRelationship.LastInteractionAt = relationship?.LastInteractionAt ?? person.LastInteractionAt;
        userRelationship.DurationDays = relationship?.DurationDays ?? 0;
        userRelationship.Interests = topics.Select(t => t.Name).ToList();
        userRelationship.KnownInfo = person.KnownInfo;
        
        return userRelationship;
    }
    
    public async Task<bool> UpdateUserRelationshipAsync(ulong userId, string relationshipType, double strength)
    {
        try
        {
            // Get current person
            var person = await GetPersonAsync(userId);
            if (person == null)
            {
                _logger.LogWarning("Cannot update relationship for unknown user {UserId}", userId);
                return false;
            }
            
            // Update relationship strength
            person.RelationshipStrength = Math.Clamp(strength, 0, 1);
            
            // Upsert the person
            await UpsertPersonAsync(person);
            
            // Get bot vertex ID
            var botVertex = await GetBotVertexAsync();
            if (botVertex == null)
            {
                _logger.LogWarning("Bot vertex not found, cannot update relationship");
                return false;
            }
            
            // Check for existing relationship edge
            var edgeQuery = $"g.V('{botVertex.Id}').outE('{RelationshipEdge.EdgeLabel}').where(inV().has('id', '{person.Id}'))";
            var edges = await SubmitGremlinQueryWithRetryAsync<RelationshipEdge>(edgeQuery);
            var edge = edges.FirstOrDefault();
            
            if (edge != null)
            {
                // Update existing edge
                var updateQuery = $"g.E('{edge.Id}')";
                updateQuery += $".property('type', '{EscapeStringForGremlin(relationshipType)}')";
                updateQuery += $".property('strength', {strength})";
                updateQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(updateQuery);
            }
            else
            {
                // Create new edge
                var addQuery = $"g.V('{botVertex.Id}').addE('{RelationshipEdge.EdgeLabel}').to(g.V('{person.Id}'))";
                addQuery += $".property('id', '{Guid.NewGuid()}')";
                addQuery += $".property('type', '{EscapeStringForGremlin(relationshipType)}')";
                addQuery += $".property('strength', {strength})";
                addQuery += $".property('createdAt', '{DateTime.UtcNow:o}')";
                addQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                addQuery += $".property('interactionCount', 1)";
                addQuery += $".property('lastInteractionAt', '{DateTime.UtcNow:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(addQuery);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating relationship for user {UserId}", userId);
            return false;
        }
    }
    
    // Topic operations
    
    public async Task<TopicVertex> GetTopicAsync(string name)
    {
        // Check cache first
        string cacheKey = name.ToLowerInvariant();
        if (_topicCache.TryGetValue(cacheKey, out var cachedTopic))
        {
            return cachedTopic;
        }
        
        await _graphLock.WaitAsync();
        try
        {
            // Query for topic by name
            var query = $"g.V().hasLabel('{TopicVertex.VertexLabel}').has('name', '{EscapeStringForGremlin(name)}')";
            var results = await SubmitGremlinQueryWithRetryAsync<TopicVertex>(query);
            
            var topic = results.FirstOrDefault();
            
            // Cache the result if found
            if (topic != null)
            {
                _topicCache[cacheKey] = topic;
            }
            
            return topic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting topic {Name}", name);
            return null;
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task<IEnumerable<TopicVertex>> SearchTopicsAsync(string[] keywords, int maxResults = 10)
    {
        await _graphLock.WaitAsync();
        try
        {
            var results = new List<TopicVertex>();
            
            // Build query for topics matching any keyword
            var query = $"g.V().hasLabel('{TopicVertex.VertexLabel}')";
            
            if (keywords.Length > 0)
            {
                query += ".or(";
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (i > 0) query += ", ";
                    query += $"has('name', containing('{EscapeStringForGremlin(keywords[i])}'))";
                }
                query += ")";
            }
            
            query += $".order().by('frequency', decr).limit({maxResults})";
            
            results = (await SubmitGremlinQueryWithRetryAsync<TopicVertex>(query)).ToList();
            
            // If not enough results, try searching by keywords
            if (results.Count < maxResults && keywords.Length > 0)
            {
                var keywordQuery = $"g.V().hasLabel('{TopicVertex.VertexLabel}')";
                keywordQuery += ".or(";
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (i > 0) keywordQuery += ", ";
                    keywordQuery += $"has('keywords', containing('{EscapeStringForGremlin(keywords[i])}'))";
                }
                keywordQuery += ")";
                
                keywordQuery += $".order().by('frequency', decr).limit({maxResults - results.Count})";
                
                var keywordResults = await SubmitGremlinQueryWithRetryAsync<TopicVertex>(keywordQuery);
                
                // Add results that aren't already in the list
                foreach (var topic in keywordResults)
                {
                    if (!results.Any(t => t.Id == topic.Id))
                    {
                        results.Add(topic);
                    }
                }
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching topics");
            return Enumerable.Empty<TopicVertex>();
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task<TopicVertex> UpsertTopicAsync(TopicVertex topic)
    {
        await _graphLock.WaitAsync();
        try
        {
            // Check if topic exists
            var existingTopic = await GetTopicAsync(topic.Name);
            
            if (existingTopic != null)
            {
                // Update existing topic
                topic.Id = existingTopic.Id;
                topic.CreatedAt = existingTopic.CreatedAt;
                topic.UpdatedAt = DateTime.UtcNow;
                
                // Use ETag for optimistic concurrency
                topic.ETag = existingTopic.ETag;
                
                // Update the vertex
                var updateQuery = $"g.V('{topic.Id}').has('_etag', '{topic.ETag}')";
                updateQuery += $".property('description', '{EscapeStringForGremlin(topic.Description)}')";
                updateQuery += $".property('frequency', {topic.Frequency})";
                updateQuery += $".property('mentionCount', {topic.MentionCount})";
                updateQuery += $".property('lastMentionedAt', '{topic.LastMentionedAt:o}')";
                updateQuery += $".property('updatedAt', '{topic.UpdatedAt:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(updateQuery);
                
                // Update keywords
                var keywordsQuery = $"g.V('{topic.Id}').property('keywords', {JsonConvert.SerializeObject(topic.Keywords)})";
                await SubmitGremlinQueryWithRetryAsync<dynamic>(keywordsQuery);
            }
            else
            {
                // Create new topic
                topic.Id = Guid.NewGuid().ToString();
                topic.CreatedAt = DateTime.UtcNow;
                topic.UpdatedAt = DateTime.UtcNow;
                
                // Create the vertex
                var addQuery = $"g.addV('{TopicVertex.VertexLabel}').property('id', '{topic.Id}')";
                addQuery += $".property('name', '{EscapeStringForGremlin(topic.Name)}')";
                addQuery += $".property('description', '{EscapeStringForGremlin(topic.Description)}')";
                addQuery += $".property('frequency', {topic.Frequency})";
                addQuery += $".property('mentionCount', {topic.MentionCount})";
                addQuery += $".property('lastMentionedAt', '{topic.LastMentionedAt:o}')";
                addQuery += $".property('createdAt', '{topic.CreatedAt:o}')";
                addQuery += $".property('updatedAt', '{topic.UpdatedAt:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(addQuery);
                
                // Add keywords
                var keywordsQuery = $"g.V('{topic.Id}').property('keywords', {JsonConvert.SerializeObject(topic.Keywords)})";
                await SubmitGremlinQueryWithRetryAsync<dynamic>(keywordsQuery);
            }
            
            // Refresh the data
            var refreshQuery = $"g.V('{topic.Id}')";
            var refreshResults = await SubmitGremlinQueryWithRetryAsync<TopicVertex>(refreshQuery);
            var refreshedTopic = refreshResults.FirstOrDefault();
            
            // Update cache
            if (refreshedTopic != null)
            {
                _topicCache[topic.Name.ToLowerInvariant()] = refreshedTopic;
            }
            
            return refreshedTopic ?? topic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting topic {Name}", topic.Name);
            return topic;
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    // Memory operations
    
    public async Task<MemoryVertex> CreateMemoryAsync(string title, string content, string type, string source, DateTime occurredAt)
    {
        await _graphLock.WaitAsync();
        try
        {
            // Create new memory
            var memory = new MemoryVertex(title, content, type, source, occurredAt);
            memory.Id = Guid.NewGuid().ToString();
            memory.CreatedAt = DateTime.UtcNow;
            memory.UpdatedAt = DateTime.UtcNow;
            
            // Create the vertex
            var addQuery = $"g.addV('{MemoryVertex.VertexLabel}').property('id', '{memory.Id}')";
            addQuery += $".property('title', '{EscapeStringForGremlin(memory.Title)}')";
            addQuery += $".property('content', '{EscapeStringForGremlin(memory.Content)}')";
            addQuery += $".property('type', '{EscapeStringForGremlin(memory.Type)}')";
            addQuery += $".property('source', '{EscapeStringForGremlin(memory.Source)}')";
            addQuery += $".property('occurredAt', '{memory.OccurredAt:o}')";
            addQuery += $".property('importance', {memory.Importance})";
            addQuery += $".property('createdAt', '{memory.CreatedAt:o}')";
            addQuery += $".property('updatedAt', '{memory.UpdatedAt:o}')";
            
            await SubmitGremlinQueryWithRetryAsync<dynamic>(addQuery);
            
            // Add keywords
            var keywordsQuery = $"g.V('{memory.Id}').property('keywords', {JsonConvert.SerializeObject(memory.Keywords)})";
            await SubmitGremlinQueryWithRetryAsync<dynamic>(keywordsQuery);
            
            // Refresh the data
            var refreshQuery = $"g.V('{memory.Id}')";
            var refreshResults = await SubmitGremlinQueryWithRetryAsync<MemoryVertex>(refreshQuery);
            var refreshedMemory = refreshResults.FirstOrDefault();
            
            return refreshedMemory ?? memory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating memory {Title}", title);
            return null;
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task<IEnumerable<MemoryVertex>> SearchMemoriesAsync(string[] keywords, int maxResults = 10)
    {
        await _graphLock.WaitAsync();
        try
        {
            var results = new List<MemoryVertex>();
            
            // Build query for memories matching any keyword in content
            var query = $"g.V().hasLabel('{MemoryVertex.VertexLabel}')";
            
            if (keywords.Length > 0)
            {
                query += ".or(";
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (i > 0) query += ", ";
                    query += $"has('content', containing('{EscapeStringForGremlin(keywords[i])}'))";
                }
                
                for (int i = 0; i < keywords.Length; i++)
                {
                    query += $", has('title', containing('{EscapeStringForGremlin(keywords[i])}'))";
                }
                query += ")";
            }
            
            query += $".order().by('importance', decr).limit({maxResults})";
            
            results = (await SubmitGremlinQueryWithRetryAsync<MemoryVertex>(query)).ToList();
            
            // If not enough results, try searching by keywords
            if (results.Count < maxResults && keywords.Length > 0)
            {
                var keywordQuery = $"g.V().hasLabel('{MemoryVertex.VertexLabel}')";
                keywordQuery += ".or(";
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (i > 0) keywordQuery += ", ";
                    keywordQuery += $"has('keywords', containing('{EscapeStringForGremlin(keywords[i])}'))";
                }
                keywordQuery += ")";
                
                keywordQuery += $".order().by('importance', decr).limit({maxResults - results.Count})";
                
                var keywordResults = await SubmitGremlinQueryWithRetryAsync<MemoryVertex>(keywordQuery);
                
                // Add results that aren't already in the list
                foreach (var memory in keywordResults)
                {
                    if (!results.Any(m => m.Id == memory.Id))
                    {
                        results.Add(memory);
                    }
                }
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching memories");
            return Enumerable.Empty<MemoryVertex>();
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task<IEnumerable<MemoryVertex>> GetRecentMemoriesAsync(int count = 10)
    {
        await _graphLock.WaitAsync();
        try
        {
            // Query for recent memories
            var query = $"g.V().hasLabel('{MemoryVertex.VertexLabel}').order().by('occurredAt', decr).limit({count})";
            var results = await SubmitGremlinQueryWithRetryAsync<MemoryVertex>(query);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent memories");
            return Enumerable.Empty<MemoryVertex>();
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    // Relationships between entities
    
    public async Task<RelationshipEdge> RecordInteractionAsync(ulong sourceUserId, ulong targetUserId, string type)
    {
        await _graphLock.WaitAsync();
        try
        {
            // Get source and target person vertices
            var sourcePerson = await GetPersonAsync(sourceUserId);
            var targetPerson = await GetPersonAsync(targetUserId);
            
            if (sourcePerson == null || targetPerson == null)
            {
                _logger.LogWarning("Cannot record interaction between unknown users {SourceUserId} and {TargetUserId}", 
                    sourceUserId, targetUserId);
                return null;
            }
            
            // Check for existing relationship edge
            var edgeQuery = $"g.V('{sourcePerson.Id}').outE('{RelationshipEdge.EdgeLabel}').where(inV().has('id', '{targetPerson.Id}'))";
            var edges = await SubmitGremlinQueryWithRetryAsync<RelationshipEdge>(edgeQuery);
            var edge = edges.FirstOrDefault();
            
            if (edge != null)
            {
                // Update existing edge
                edge.RecordInteraction();
                
                // Update the edge
                var updateQuery = $"g.E('{edge.Id}')";
                updateQuery += $".property('interactionCount', {edge.InteractionCount})";
                updateQuery += $".property('lastInteractionAt', '{edge.LastInteractionAt:o}')";
                updateQuery += $".property('durationDays', {edge.DurationDays})";
                updateQuery += $".property('interactionFrequency', {edge.InteractionFrequency})";
                updateQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                // Update type if one is provided
                if (!string.IsNullOrEmpty(type))
                {
                    updateQuery += $".property('type', '{EscapeStringForGremlin(type)}')";
                }
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(updateQuery);
                
                // Refresh the edge
                var refreshQuery = $"g.E('{edge.Id}')";
                var refreshResults = await SubmitGremlinQueryWithRetryAsync<RelationshipEdge>(refreshQuery);
                var refreshedEdge = refreshResults.FirstOrDefault();
                
                return refreshedEdge ?? edge;
            }
            else
            {
                // Create new edge
                var newEdge = new RelationshipEdge(sourcePerson.Id, targetPerson.Id, type);
                
                var addQuery = $"g.V('{sourcePerson.Id}').addE('{RelationshipEdge.EdgeLabel}').to(g.V('{targetPerson.Id}'))";
                addQuery += $".property('id', '{newEdge.Id}')";
                addQuery += $".property('type', '{EscapeStringForGremlin(type)}')";
                addQuery += $".property('interactionCount', 1)";
                addQuery += $".property('lastInteractionAt', '{DateTime.UtcNow:o}')";
                addQuery += $".property('durationDays', 0)";
                addQuery += $".property('interactionFrequency', 0.2)";
                addQuery += $".property('strength', 0.2)";
                addQuery += $".property('createdAt', '{DateTime.UtcNow:o}')";
                addQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(addQuery);
                
                // Refresh the edge
                var refreshQuery = $"g.E('{newEdge.Id}')";
                var refreshResults = await SubmitGremlinQueryWithRetryAsync<RelationshipEdge>(refreshQuery);
                var refreshedEdge = refreshResults.FirstOrDefault();
                
                return refreshedEdge ?? newEdge;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording interaction between users {SourceUserId} and {TargetUserId}", 
                sourceUserId, targetUserId);
            return null;
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task<InterestEdge> RecordInterestAsync(ulong userId, string topicName, string source)
    {
        await _graphLock.WaitAsync();
        try
        {
            // Get person vertex
            var person = await GetPersonAsync(userId);
            if (person == null)
            {
                _logger.LogWarning("Cannot record interest for unknown user {UserId}", userId);
                return null;
            }
            
            // Get or create topic vertex
            var topic = await GetTopicAsync(topicName);
            if (topic == null)
            {
                // Create new topic
                topic = new TopicVertex(topicName);
                topic = await UpsertTopicAsync(topic);
                
                if (topic == null)
                {
                    _logger.LogWarning("Failed to create topic {TopicName}", topicName);
                    return null;
                }
            }
            
            // Record topic mention
            topic.RecordMention();
            await UpsertTopicAsync(topic);
            
            // Check for existing interest edge
            var edgeQuery = $"g.V('{person.Id}').outE('{InterestEdge.EdgeLabel}').where(inV().has('id', '{topic.Id}'))";
            var edges = await SubmitGremlinQueryWithRetryAsync<InterestEdge>(edgeQuery);
            var edge = edges.FirstOrDefault();
            
            if (edge != null)
            {
                // Update existing edge
                edge.RecordObservation(source);
                
                // Update the edge
                var updateQuery = $"g.E('{edge.Id}')";
                updateQuery += $".property('observationCount', {edge.ObservationCount})";
                updateQuery += $".property('lastObservedAt', '{edge.LastObservedAt:o}')";
                updateQuery += $".property('level', {edge.Level})";
                updateQuery += $".property('source', '{EscapeStringForGremlin(source)}')";
                updateQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(updateQuery);
                
                // Add interest to person
                if (!person.Interests.Contains(topicName))
                {
                    person.AddInterest(topicName);
                    await UpsertPersonAsync(person);
                }
                
                // Refresh the edge
                var refreshQuery = $"g.E('{edge.Id}')";
                var refreshResults = await SubmitGremlinQueryWithRetryAsync<InterestEdge>(refreshQuery);
                var refreshedEdge = refreshResults.FirstOrDefault();
                
                return refreshedEdge ?? edge;
            }
            else
            {
                // Create new edge
                var newEdge = new InterestEdge(person.Id, topic.Id, source);
                
                var addQuery = $"g.V('{person.Id}').addE('{InterestEdge.EdgeLabel}').to(g.V('{topic.Id}'))";
                addQuery += $".property('id', '{newEdge.Id}')";
                addQuery += $".property('level', 0.5)";
                addQuery += $".property('firstObservedAt', '{DateTime.UtcNow:o}')";
                addQuery += $".property('lastObservedAt', '{DateTime.UtcNow:o}')";
                addQuery += $".property('observationCount', 1)";
                addQuery += $".property('source', '{EscapeStringForGremlin(source)}')";
                addQuery += $".property('strength', 0.5)";
                addQuery += $".property('createdAt', '{DateTime.UtcNow:o}')";
                addQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(addQuery);
                
                // Add interest to person
                if (!person.Interests.Contains(topicName))
                {
                    person.AddInterest(topicName);
                    await UpsertPersonAsync(person);
                }
                
                // Refresh the edge
                var refreshQuery = $"g.E('{newEdge.Id}')";
                var refreshResults = await SubmitGremlinQueryWithRetryAsync<InterestEdge>(refreshQuery);
                var refreshedEdge = refreshResults.FirstOrDefault();
                
                return refreshedEdge ?? newEdge;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording interest for user {UserId} in topic {TopicName}", userId, topicName);
            return null;
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task<KnowledgeEdge> ConnectMemoryToTopicAsync(string memoryId, string topicName, string source, double relevance = 0.5, double confidence = 0.5)
    {
        await _graphLock.WaitAsync();
        try
        {
            // Get memory vertex
            var memoryQuery = $"g.V('{memoryId}')";
            var memoryResults = await SubmitGremlinQueryWithRetryAsync<MemoryVertex>(memoryQuery);
            var memory = memoryResults.FirstOrDefault();
            
            if (memory == null)
            {
                _logger.LogWarning("Cannot connect unknown memory {MemoryId} to topic", memoryId);
                return null;
            }
            
            // Get or create topic vertex
            var topic = await GetTopicAsync(topicName);
            if (topic == null)
            {
                // Create new topic
                topic = new TopicVertex(topicName);
                topic = await UpsertTopicAsync(topic);
                
                if (topic == null)
                {
                    _logger.LogWarning("Failed to create topic {TopicName}", topicName);
                    return null;
                }
            }
            
            // Record topic mention
            topic.RecordMention();
            await UpsertTopicAsync(topic);
            
            // Check for existing knowledge edge
            var edgeQuery = $"g.V('{memory.Id}').outE('{KnowledgeEdge.EdgeLabel}').where(inV().has('id', '{topic.Id}'))";
            var edges = await SubmitGremlinQueryWithRetryAsync<KnowledgeEdge>(edgeQuery);
            var edge = edges.FirstOrDefault();
            
            if (edge != null)
            {
                // Update existing edge
                edge.Reinforce(relevance - edge.Relevance, 0.1);
                
                // Update the edge
                var updateQuery = $"g.E('{edge.Id}')";
                updateQuery += $".property('reinforcementCount', {edge.ReinforcementCount})";
                updateQuery += $".property('relevance', {edge.Relevance})";
                updateQuery += $".property('confidence', {edge.Confidence})";
                updateQuery += $".property('source', '{EscapeStringForGremlin(source)}')";
                updateQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(updateQuery);
                
                // Add keyword to memory if it's not already there
                if (!memory.Keywords.Contains(topicName))
                {
                    memory.AddKeyword(topicName);
                    
                    var keywordsQuery = $"g.V('{memory.Id}').property('keywords', {JsonConvert.SerializeObject(memory.Keywords)})";
                    await SubmitGremlinQueryWithRetryAsync<dynamic>(keywordsQuery);
                }
                
                // Refresh the edge
                var refreshQuery = $"g.E('{edge.Id}')";
                var refreshResults = await SubmitGremlinQueryWithRetryAsync<KnowledgeEdge>(refreshQuery);
                var refreshedEdge = refreshResults.FirstOrDefault();
                
                return refreshedEdge ?? edge;
            }
            else
            {
                // Create new edge
                var newEdge = new KnowledgeEdge(memory.Id, topic.Id, source, relevance, confidence);
                
                var addQuery = $"g.V('{memory.Id}').addE('{KnowledgeEdge.EdgeLabel}').to(g.V('{topic.Id}'))";
                addQuery += $".property('id', '{newEdge.Id}')";
                addQuery += $".property('relevance', {relevance})";
                addQuery += $".property('confidence', {confidence})";
                addQuery += $".property('source', '{EscapeStringForGremlin(source)}')";
                addQuery += $".property('reinforcementCount', 0)";
                addQuery += $".property('strength', {relevance})";
                addQuery += $".property('createdAt', '{DateTime.UtcNow:o}')";
                addQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                await SubmitGremlinQueryWithRetryAsync<dynamic>(addQuery);
                
                // Add keyword to memory
                if (!memory.Keywords.Contains(topicName))
                {
                    memory.AddKeyword(topicName);
                    
                    var keywordsQuery = $"g.V('{memory.Id}').property('keywords', {JsonConvert.SerializeObject(memory.Keywords)})";
                    await SubmitGremlinQueryWithRetryAsync<dynamic>(keywordsQuery);
                }
                
                // Refresh the edge
                var refreshQuery = $"g.E('{newEdge.Id}')";
                var refreshResults = await SubmitGremlinQueryWithRetryAsync<KnowledgeEdge>(refreshQuery);
                var refreshedEdge = refreshResults.FirstOrDefault();
                
                return refreshedEdge ?? newEdge;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting memory {MemoryId} to topic {TopicName}", memoryId, topicName);
            return null;
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    public async Task PerformGraphMaintenanceAsync()
    {
        await _graphLock.WaitAsync();
        try
        {
            _logger.LogInformation("Starting knowledge graph maintenance");
            
            // 1. Update person relationship strengths based on interaction frequency
            var personQuery = $"g.V().hasLabel('{PersonVertex.VertexLabel}')";
            var people = await SubmitGremlinQueryWithRetryAsync<PersonVertex>(personQuery);
            
            foreach (var person in people)
            {
                // Get all relationships
                var relationshipQuery = $"g.V('{person.Id}').bothE('{RelationshipEdge.EdgeLabel}')";
                var relationships = await SubmitGremlinQueryWithRetryAsync<RelationshipEdge>(relationshipQuery);
                
                if (relationships.Any())
                {
                    // Calculate average interaction frequency
                    double avgFrequency = relationships.Average(r => r.InteractionFrequency);
                    
                    // Update relationship strength if different
                    if (Math.Abs(person.RelationshipStrength - avgFrequency) > 0.1)
                    {
                        person.RelationshipStrength = avgFrequency;
                        
                        var updateQuery = $"g.V('{person.Id}')";
                        updateQuery += $".property('relationshipStrength', {avgFrequency})";
                        updateQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
                        
                        await SubmitGremlinQueryWithRetryAsync<dynamic>(updateQuery);
                    }
                }
            }
            
            // 2. Clear caches to ensure fresh data
            _personCache.Clear();
            _topicCache.Clear();
            
            _logger.LogInformation("Completed knowledge graph maintenance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing graph maintenance");
        }
        finally
        {
            _graphLock.Release();
        }
    }
    
    // Helper methods
    
    private async Task<PersonVertex> GetBotVertexAsync()
    {
        // This represents the bot itself in the graph
        var query = $"g.V().hasLabel('{PersonVertex.VertexLabel}').has('isBot', true)";
        var results = await SubmitGremlinQueryWithRetryAsync<PersonVertex>(query);
        
        var botVertex = results.FirstOrDefault();
        
        if (botVertex == null)
        {
            // Create the bot vertex
            botVertex = new PersonVertex
            {
                Username = "ElectricRaspberry",
                DisplayName = "ElectricRaspberry",
                RelationshipStrength = 1.0
            };
            
            // Add a property to mark as bot
            var addQuery = $"g.addV('{PersonVertex.VertexLabel}').property('id', '{Guid.NewGuid()}')";
            addQuery += $".property('username', '{botVertex.Username}')";
            addQuery += $".property('displayName', '{botVertex.DisplayName}')";
            addQuery += $".property('relationshipStrength', 1.0)";
            addQuery += $".property('isBot', true)";
            addQuery += $".property('createdAt', '{DateTime.UtcNow:o}')";
            addQuery += $".property('updatedAt', '{DateTime.UtcNow:o}')";
            
            await SubmitGremlinQueryWithRetryAsync<dynamic>(addQuery);
            
            // Get the newly created vertex
            var refreshQuery = $"g.V().hasLabel('{PersonVertex.VertexLabel}').has('isBot', true)";
            var refreshResults = await SubmitGremlinQueryWithRetryAsync<PersonVertex>(refreshQuery);
            botVertex = refreshResults.FirstOrDefault();
        }
        
        return botVertex;
    }
    
    private async Task InitializeGraphAsync()
    {
        try
        {
            // Check if we can connect to the graph
            var testQuery = "g.V().limit(1)";
            await SubmitGremlinQueryWithRetryAsync<dynamic>(testQuery);
            
            // Create bot vertex if it doesn't exist
            await GetBotVertexAsync();
            
            _logger.LogInformation("Knowledge graph initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing knowledge graph");
        }
    }
    
    private async Task<IEnumerable<T>> SubmitGremlinQueryWithRetryAsync<T>(string query, int maxRetries = 3)
    {
        int retryCount = 0;
        Exception lastException = null;
        
        while (retryCount < maxRetries)
        {
            try
            {
                var results = await _gremlinClient.SubmitAsync<T>(query);
                return results;
            }
            catch (ResponseException ex) when (ex.StatusCode == 409 || ex.StatusCode == 429)
            {
                // Conflict or rate limiting error - retry with backoff
                lastException = ex;
                retryCount++;
                
                // Exponential backoff
                await Task.Delay(100 * (int)Math.Pow(2, retryCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Gremlin query: {Query}", query);
                throw;
            }
        }
        
        _logger.LogError(lastException, "Failed to execute Gremlin query after {RetryCount} attempts: {Query}", 
            retryCount, query);
        
        return Enumerable.Empty<T>();
    }
    
    private string EscapeStringForGremlin(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "";
        }
        
        return s.Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");
    }
}
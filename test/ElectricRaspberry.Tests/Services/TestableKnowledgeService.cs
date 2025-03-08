using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Knowledge;
using ElectricRaspberry.Models.Knowledge.Edges;
using ElectricRaspberry.Models.Knowledge.Vertices;
using ElectricRaspberry.Services;
using Gremlin.Net.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElectricRaspberry.Tests.Services
{
    /// <summary>
    /// Interface for mocking Gremlin client operations
    /// </summary>
    public interface IGremlinClientWrapper
    {
        Task<IEnumerable<T>> SubmitAsync<T>(string query, Dictionary<string, object>? parameters = null);
    }

    /// <summary>
    /// Testable version of KnowledgeService that allows injecting a mock IGremlinClientWrapper
    /// </summary>
    public class TestableKnowledgeService : IKnowledgeService
    {
        private readonly ILogger<KnowledgeService> _logger;
        private readonly CosmosDbOptions _options;
        private readonly IGremlinClientWrapper _gremlinClient;

        public TestableKnowledgeService(
            ILogger<KnowledgeService> logger,
            IOptions<CosmosDbOptions> options,
            IGremlinClientWrapper gremlinClient)
        {
            _logger = logger;
            _options = options.Value;
            _gremlinClient = gremlinClient;
        }

        /// <summary>
        /// Gets a person vertex by user ID
        /// </summary>
        public async Task<PersonVertex> GetPersonAsync(string userId)
        {
            var query = $"g.V().hasLabel('person').has('userId', '{userId}')";
            var results = await _gremlinClient.SubmitAsync<PersonVertex>(query);
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Creates or updates a person vertex
        /// </summary>
        public async Task UpsertPersonAsync(PersonVertex person)
        {
            // First check if the person exists
            var existingPerson = await GetPersonAsync(person.UserId);
            
            string query;
            if (existingPerson == null)
            {
                // Create new person
                query = $"g.addV('person')" +
                    $".property('userId', '{person.UserId}')" +
                    $".property('username', '{person.Username}')" +
                    $".property('updatedAt', '{DateTime.UtcNow:o}')";
            }
            else
            {
                // Update existing person
                query = $"g.V().hasLabel('person').has('userId', '{person.UserId}')" +
                    $".property('username', '{person.Username}')" +
                    $".property('updatedAt', '{DateTime.UtcNow:o}')";
            }
            
            await _gremlinClient.SubmitAsync<object>(query);
        }

        /// <summary>
        /// Gets a topic vertex by name
        /// </summary>
        public async Task<TopicVertex> GetTopicAsync(string topicName)
        {
            var query = $"g.V().hasLabel('topic').has('name', '{topicName}')";
            var results = await _gremlinClient.SubmitAsync<TopicVertex>(query);
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Searches for topics by name prefix
        /// </summary>
        public async Task<IEnumerable<TopicVertex>> SearchTopicsAsync(string namePrefix)
        {
            var query = $"g.V().hasLabel('topic').has('name', TextP.startingWith('{namePrefix}'))";
            return await _gremlinClient.SubmitAsync<TopicVertex>(query);
        }

        /// <summary>
        /// Creates or updates a topic vertex
        /// </summary>
        public async Task UpsertTopicAsync(TopicVertex topic)
        {
            // First check if the topic exists
            var existingTopic = await GetTopicAsync(topic.Name);
            
            string query;
            if (existingTopic == null)
            {
                // Create new topic
                query = $"g.addV('topic')" +
                    $".property('name', '{topic.Name}')" +
                    $".property('updatedAt', '{DateTime.UtcNow:o}')";
            }
            else
            {
                // Update existing topic
                query = $"g.V().hasLabel('topic').has('name', '{topic.Name}')" +
                    $".property('updatedAt', '{DateTime.UtcNow:o}')";
            }
            
            await _gremlinClient.SubmitAsync<object>(query);
        }

        /// <summary>
        /// Creates a memory vertex and connects it to a person
        /// </summary>
        public async Task CreateMemoryAsync(string userId, MemoryVertex memory)
        {
            // Create memory vertex
            var createMemoryQuery = $"g.addV('memory')" +
                $".property('content', '{memory.Content}')" +
                $".property('createdAt', '{memory.CreatedAt:o}')" +
                $".property('importance', {memory.Importance})";
            
            await _gremlinClient.SubmitAsync<object>(createMemoryQuery);
            
            // Connect memory to person
            var connectQuery = $"g.V().hasLabel('person').has('userId', '{userId}')" +
                $".as('p')" +
                $".V().hasLabel('memory').has('content', '{memory.Content}').has('createdAt', '{memory.CreatedAt:o}')" +
                $".as('m')" +
                $".addE('remembers').from('p').to('m')";
            
            await _gremlinClient.SubmitAsync<object>(connectQuery);
        }

        /// <summary>
        /// Gets recent memories for a person
        /// </summary>
        public async Task<IEnumerable<MemoryVertex>> GetRecentMemoriesAsync(string userId, int limit = 10)
        {
            var query = $"g.V().hasLabel('person').has('userId', '{userId}')" +
                $".outE('remembers').inV()" +
                $".order().by('createdAt', decr)" +
                $".limit({limit})";
            
            return await _gremlinClient.SubmitAsync<MemoryVertex>(query);
        }

        /// <summary>
        /// Gets the relationship between two users
        /// </summary>
        public async Task<UserRelationship> GetUserRelationshipAsync(string userId1, string userId2)
        {
            var query = $"g.V().hasLabel('person').has('userId', '{userId1}')" +
                $".outE('relationship').where(inV().has('userId', '{userId2}'))" +
                $".project('strength', 'lastInteraction')" +
                $".by('strength')" +
                $".by('lastInteraction')";
            
            var results = await _gremlinClient.SubmitAsync<Dictionary<string, object>>(query);
            var result = results.FirstOrDefault();
            
            if (result == null)
                return null;
            
            return new UserRelationship
            {
                SourceUserId = userId1,
                TargetUserId = userId2,
                Strength = Convert.ToSingle(result["strength"]),
                LastInteraction = DateTime.Parse(result["lastInteraction"].ToString())
            };
        }

        /// <summary>
        /// Updates or creates a relationship between two users
        /// </summary>
        public async Task UpdateUserRelationshipAsync(UserRelationship relationship)
        {
            var existingRelationship = await GetUserRelationshipAsync(
                relationship.SourceUserId, 
                relationship.TargetUserId);
            
            string query;
            if (existingRelationship == null)
            {
                // Create new relationship
                query = $"g.V().hasLabel('person').has('userId', '{relationship.SourceUserId}')" +
                    $".as('p1')" +
                    $".V().hasLabel('person').has('userId', '{relationship.TargetUserId}')" +
                    $".as('p2')" +
                    $".addE('relationship').from('p1').to('p2')" +
                    $".property('strength', {relationship.Strength})" +
                    $".property('lastInteraction', '{relationship.LastInteraction:o}')";
            }
            else
            {
                // Update existing relationship
                query = $"g.V().hasLabel('person').has('userId', '{relationship.SourceUserId}')" +
                    $".outE('relationship').where(inV().has('userId', '{relationship.TargetUserId}'))" +
                    $".property('strength', {relationship.Strength})" +
                    $".property('lastInteraction', '{relationship.LastInteraction:o}')";
            }
            
            await _gremlinClient.SubmitAsync<object>(query);
        }

        /// <summary>
        /// Records an interaction between two users
        /// </summary>
        public async Task RecordInteractionAsync(string userId1, string userId2, float strengthDelta)
        {
            var relationship = await GetUserRelationshipAsync(userId1, userId2);
            
            if (relationship == null)
            {
                relationship = new UserRelationship
                {
                    SourceUserId = userId1,
                    TargetUserId = userId2,
                    Strength = strengthDelta,
                    LastInteraction = DateTime.UtcNow
                };
            }
            else
            {
                // Update existing relationship
                relationship.Strength = Math.Min(1.0f, relationship.Strength + strengthDelta);
                relationship.LastInteraction = DateTime.UtcNow;
            }
            
            await UpdateUserRelationshipAsync(relationship);
        }

        /// <summary>
        /// Records or updates a user's interest in a topic
        /// </summary>
        public async Task RecordInterestAsync(string userId, string topicName, float interestLevel)
        {
            // Get or create the topic
            var topic = await GetTopicAsync(topicName);
            if (topic == null)
            {
                topic = new TopicVertex
                {
                    Name = topicName,
                    UpdatedAt = DateTime.UtcNow
                };
                await UpsertTopicAsync(topic);
            }
            
            // Check if interest edge already exists
            var query = $"g.V().hasLabel('person').has('userId', '{userId}')" +
                $".outE('interested_in').where(inV().has('name', '{topicName}'))";
            
            var existingEdges = await _gremlinClient.SubmitAsync<object>(query);
            
            if (!existingEdges.Any())
            {
                // Create new interest edge
                var createQuery = $"g.V().hasLabel('person').has('userId', '{userId}')" +
                    $".as('p')" +
                    $".V().hasLabel('topic').has('name', '{topicName}')" +
                    $".as('t')" +
                    $".addE('interested_in').from('p').to('t')" +
                    $".property('level', {interestLevel})" +
                    $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                await _gremlinClient.SubmitAsync<object>(createQuery);
            }
            else
            {
                // Update existing interest edge
                var updateQuery = $"g.V().hasLabel('person').has('userId', '{userId}')" +
                    $".outE('interested_in').where(inV().has('name', '{topicName}'))" +
                    $".property('level', {interestLevel})" +
                    $".property('updatedAt', '{DateTime.UtcNow:o}')";
                
                await _gremlinClient.SubmitAsync<object>(updateQuery);
            }
        }

        /// <summary>
        /// Connects a memory to a topic
        /// </summary>
        public async Task ConnectMemoryToTopicAsync(MemoryVertex memory, string topicName)
        {
            // Get or create the topic
            var topic = await GetTopicAsync(topicName);
            if (topic == null)
            {
                topic = new TopicVertex
                {
                    Name = topicName,
                    UpdatedAt = DateTime.UtcNow
                };
                await UpsertTopicAsync(topic);
            }
            
            // Connect memory to topic
            var query = $"g.V().hasLabel('memory')" +
                $".has('content', '{memory.Content}')" +
                $".has('createdAt', '{memory.CreatedAt:o}')" +
                $".as('m')" +
                $".V().hasLabel('topic').has('name', '{topicName}')" +
                $".as('t')" +
                $".addE('about').from('m').to('t')";
            
            await _gremlinClient.SubmitAsync<object>(query);
        }

        /// <summary>
        /// Performs maintenance tasks on the knowledge graph
        /// </summary>
        public async Task PerformGraphMaintenanceAsync()
        {
            // Example: Decay old relationships
            var decayQuery = $"g.E().hasLabel('relationship')" +
                $".has('lastInteraction', lt('{DateTime.UtcNow.AddMonths(-1):o}'))" +
                $".property('strength', values('strength').math('_ * 0.9'))";
            
            await _gremlinClient.SubmitAsync<object>(decayQuery);
            
            // Example: Remove very weak relationships
            var pruneQuery = $"g.E().hasLabel('relationship')" +
                $".has('strength', lt(0.1))" +
                $".drop()";
            
            await _gremlinClient.SubmitAsync<object>(pruneQuery);
        }

        /// <summary>
        /// Resets the entire knowledge graph (for testing)
        /// </summary>
        public async Task ResetKnowledgeGraphAsync()
        {
            var query = "g.V().drop()";
            await _gremlinClient.SubmitAsync<object>(query);
        }
    }
}
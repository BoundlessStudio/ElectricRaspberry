using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ElectricRaspberry.Services;
using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Knowledge;
using ElectricRaspberry.Models.Knowledge.Vertices;
using ElectricRaspberry.Models.Knowledge.Edges;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ElectricRaspberry.Tests.Services
{
    public class KnowledgeServiceTests
    {
        private readonly Mock<ILogger<KnowledgeService>> _loggerMock;
        private readonly Mock<IOptions<CosmosDbOptions>> _cosmosOptionsMock;
        private readonly Mock<IGremlinClientWrapper> _gremlinClientMock;
        private readonly TestableKnowledgeService _knowledgeService;
        
        public KnowledgeServiceTests()
        {
            _loggerMock = new Mock<ILogger<KnowledgeService>>();
            
            // Setup CosmosDbOptions with test values
            var cosmosOptions = new CosmosDbOptions
            {
                Endpoint = "wss://testaccount.gremlin.cosmos.azure.com:443/",
                DatabaseName = "testdb",
                GraphName = "testgraph",
                AuthKey = "dGVzdGtleQ==", // Base64 encoded "testkey"
                RetryCount = 3,
                RetryDelayMs = 100
            };
            
            _cosmosOptionsMock = new Mock<IOptions<CosmosDbOptions>>();
            _cosmosOptionsMock.Setup(m => m.Value).Returns(cosmosOptions);
            
            _gremlinClientMock = new Mock<IGremlinClientWrapper>();
            
            _knowledgeService = new TestableKnowledgeService(
                _loggerMock.Object, 
                _cosmosOptionsMock.Object,
                _gremlinClientMock.Object);
        }
        
        #region Person Operations
        
        [Fact]
        public async Task GetPersonAsync_WhenPersonExists_ReturnsPersonVertex()
        {
            // Arrange
            string userId = "123456789";
            var personVertex = new PersonVertex 
            { 
                UserId = userId, 
                Username = "TestUser" 
            };
            
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<PersonVertex>(
                    It.Is<string>(q => q.Contains($"has('userId', '{userId}')")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<PersonVertex> { personVertex });
            
            // Act
            var result = await _knowledgeService.GetPersonAsync(userId);
            
            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.Username.Should().Be("TestUser");
        }
        
        [Fact]
        public async Task GetPersonAsync_WhenPersonDoesNotExist_ReturnsNull()
        {
            // Arrange
            string userId = "nonexistent";
            
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<PersonVertex>(
                    It.Is<string>(q => q.Contains($"has('userId', '{userId}')")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<PersonVertex>());
            
            // Act
            var result = await _knowledgeService.GetPersonAsync(userId);
            
            // Assert
            result.Should().BeNull();
        }
        
        [Fact]
        public async Task UpsertPersonAsync_WhenPersonDoesNotExist_CreatesNewPersonVertex()
        {
            // Arrange
            var person = new PersonVertex 
            { 
                UserId = "123456789", 
                Username = "TestUser" 
            };
            
            // Setup GetPersonAsync to return null (person doesn't exist)
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<PersonVertex>(
                    It.Is<string>(q => q.Contains($"has('userId', '{person.UserId}')")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<PersonVertex>());
            
            // Act
            await _knowledgeService.UpsertPersonAsync(person);
            
            // Assert
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains("addV('person')") && 
                        q.Contains($"property('userId', '{person.UserId}')") &&
                        q.Contains($"property('username', '{person.Username}')")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task UpsertPersonAsync_WhenPersonExists_UpdatesExistingPersonVertex()
        {
            // Arrange
            var person = new PersonVertex 
            { 
                UserId = "123456789", 
                Username = "TestUser" 
            };
            
            // Setup GetPersonAsync to return the person (person exists)
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<PersonVertex>(
                    It.Is<string>(q => q.Contains($"has('userId', '{person.UserId}')")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<PersonVertex> { person });
            
            // Act
            await _knowledgeService.UpsertPersonAsync(person);
            
            // Assert
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains($"g.V().hasLabel('person').has('userId', '{person.UserId}')") && 
                        q.Contains($"property('username', '{person.Username}')")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        #endregion
        
        #region Topic Operations
        
        [Fact]
        public async Task GetTopicAsync_WhenTopicExists_ReturnsTopicVertex()
        {
            // Arrange
            string topicName = "programming";
            var topicVertex = new TopicVertex
            {
                Name = topicName,
                UpdatedAt = DateTime.UtcNow
            };
            
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<TopicVertex>(
                    It.Is<string>(q => q.Contains($"has('name', '{topicName}')")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<TopicVertex> { topicVertex });
            
            // Act
            var result = await _knowledgeService.GetTopicAsync(topicName);
            
            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(topicName);
        }
        
        [Fact]
        public async Task SearchTopicsAsync_WhenTopicsExist_ReturnsMatchingTopics()
        {
            // Arrange
            string namePrefix = "prog";
            var topics = new List<TopicVertex>
            {
                new TopicVertex { Name = "programming", UpdatedAt = DateTime.UtcNow },
                new TopicVertex { Name = "progress", UpdatedAt = DateTime.UtcNow }
            };
            
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<TopicVertex>(
                    It.Is<string>(q => q.Contains($"TextP.startingWith('{namePrefix}')")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(topics);
            
            // Act
            var results = await _knowledgeService.SearchTopicsAsync(namePrefix);
            
            // Assert
            results.Should().NotBeEmpty();
            results.Should().HaveCount(2);
            results.Should().Contain(t => t.Name == "programming");
            results.Should().Contain(t => t.Name == "progress");
        }
        
        [Fact]
        public async Task UpsertTopicAsync_WhenTopicDoesNotExist_CreatesNewTopicVertex()
        {
            // Arrange
            var topic = new TopicVertex 
            { 
                Name = "programming", 
                UpdatedAt = DateTime.UtcNow 
            };
            
            // Setup GetTopicAsync to return null (topic doesn't exist)
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<TopicVertex>(
                    It.Is<string>(q => q.Contains($"has('name', '{topic.Name}')")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<TopicVertex>());
            
            // Act
            await _knowledgeService.UpsertTopicAsync(topic);
            
            // Assert
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains("addV('topic')") && 
                        q.Contains($"property('name', '{topic.Name}')")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        #endregion
        
        #region Memory Operations
        
        [Fact]
        public async Task CreateMemoryAsync_WhenCalledWithValidData_CreatesMemoryVertexAndConnectsToPerson()
        {
            // Arrange
            string userId = "123456789";
            var memory = new MemoryVertex 
            { 
                Content = "User mentioned they like hiking", 
                CreatedAt = DateTime.UtcNow,
                Importance = 5
            };
            
            // Act
            await _knowledgeService.CreateMemoryAsync(userId, memory);
            
            // Assert
            // Verify memory creation
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains("addV('memory')") && 
                        q.Contains($"property('content', '{memory.Content}')") &&
                        q.Contains($"property('importance', {memory.Importance})")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            
            // Verify memory connection to person
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains($"hasLabel('person').has('userId', '{userId}')") &&
                        q.Contains("addE('remembers')")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task GetRecentMemoriesAsync_WhenMemoriesExist_ReturnsRecentMemories()
        {
            // Arrange
            string userId = "123456789";
            int limit = 3;
            var memories = new List<MemoryVertex>
            {
                new MemoryVertex { Content = "Memory 1", CreatedAt = DateTime.UtcNow.AddDays(-1), Importance = 5 },
                new MemoryVertex { Content = "Memory 2", CreatedAt = DateTime.UtcNow.AddDays(-2), Importance = 4 },
                new MemoryVertex { Content = "Memory 3", CreatedAt = DateTime.UtcNow.AddDays(-3), Importance = 3 }
            };
            
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<MemoryVertex>(
                    It.Is<string>(q => 
                        q.Contains($"hasLabel('person').has('userId', '{userId}')") &&
                        q.Contains("outE('remembers').inV()") &&
                        q.Contains($"limit({limit})")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(memories);
            
            // Act
            var results = await _knowledgeService.GetRecentMemoriesAsync(userId, limit);
            
            // Assert
            results.Should().NotBeEmpty();
            results.Should().HaveCount(3);
        }
        
        #endregion
        
        #region Relationship Operations
        
        [Fact]
        public async Task RecordInteractionAsync_WhenRelationshipDoesNotExist_CreatesNewRelationship()
        {
            // Arrange
            string userId1 = "123456789";
            string userId2 = "987654321";
            float strengthDelta = 0.5f;
            
            // Setup GetUserRelationshipAsync to return null (relationship doesn't exist)
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<Dictionary<string, object>>(
                    It.Is<string>(q => 
                        q.Contains($"hasLabel('person').has('userId', '{userId1}')") &&
                        q.Contains($"where(inV().has('userId', '{userId2}'))")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<Dictionary<string, object>>());
            
            // Act
            await _knowledgeService.RecordInteractionAsync(userId1, userId2, strengthDelta);
            
            // Assert
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains($"hasLabel('person').has('userId', '{userId1}')") &&
                        q.Contains($"has('userId', '{userId2}')") &&
                        q.Contains("addE('relationship')") &&
                        q.Contains($"property('strength', {strengthDelta})")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task RecordInteractionAsync_WhenRelationshipExists_UpdatesExistingRelationship()
        {
            // Arrange
            string userId1 = "123456789";
            string userId2 = "987654321";
            float strengthDelta = 0.3f;
            float existingStrength = 0.5f;
            
            // Setup GetUserRelationshipAsync to return an existing relationship
            var relationshipData = new Dictionary<string, object>
            {
                { "strength", existingStrength },
                { "lastInteraction", DateTime.UtcNow.AddDays(-1).ToString("o") }
            };
            
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<Dictionary<string, object>>(
                    It.Is<string>(q => 
                        q.Contains($"hasLabel('person').has('userId', '{userId1}')") &&
                        q.Contains($"where(inV().has('userId', '{userId2}'))")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<Dictionary<string, object>> { relationshipData });
            
            // Act
            await _knowledgeService.RecordInteractionAsync(userId1, userId2, strengthDelta);
            
            // Assert
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains($"hasLabel('person').has('userId', '{userId1}')") &&
                        q.Contains($"where(inV().has('userId', '{userId2}'))") &&
                        q.Contains($"property('strength', {Math.Min(1.0f, existingStrength + strengthDelta)})")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task RecordInterestAsync_WhenTopicDoesNotExist_CreatesTopicAndInterestEdge()
        {
            // Arrange
            string userId = "123456789";
            string topicName = "programming";
            float interestLevel = 0.8f;
            
            // Setup GetTopicAsync to return null (topic doesn't exist)
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<TopicVertex>(
                    It.Is<string>(q => q.Contains($"has('name', '{topicName}')")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<TopicVertex>());
            
            // Setup existing interest edge check to return empty list
            _gremlinClientMock
                .Setup(g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains($"hasLabel('person').has('userId', '{userId}')") &&
                        q.Contains($"outE('interested_in').where(inV().has('name', '{topicName}'))")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new List<object>());
            
            // Act
            await _knowledgeService.RecordInterestAsync(userId, topicName, interestLevel);
            
            // Assert
            // Verify topic creation
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains("addV('topic')") && 
                        q.Contains($"property('name', '{topicName}')")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            
            // Verify interest edge creation
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains($"hasLabel('person').has('userId', '{userId}')") &&
                        q.Contains($"hasLabel('topic').has('name', '{topicName}')") &&
                        q.Contains("addE('interested_in')") &&
                        q.Contains($"property('level', {interestLevel})")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        #endregion
        
        #region Maintenance Operations
        
        [Fact]
        public async Task PerformGraphMaintenanceAsync_WhenCalled_PerformsMaintenanceTasks()
        {
            // Arrange
            // No specific setup needed
            
            // Act
            await _knowledgeService.PerformGraphMaintenanceAsync();
            
            // Assert
            // Verify decay of old relationships
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains("g.E().hasLabel('relationship')") &&
                        q.Contains("has('lastInteraction', lt(") &&
                        q.Contains("property('strength', values('strength').math('_ * 0.9'))")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            
            // Verify pruning of weak relationships
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => 
                        q.Contains("g.E().hasLabel('relationship')") &&
                        q.Contains("has('strength', lt(0.1))") &&
                        q.Contains("drop()")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task ResetKnowledgeGraphAsync_WhenCalled_DropsAllVertices()
        {
            // Arrange
            // No specific setup needed
            
            // Act
            await _knowledgeService.ResetKnowledgeGraphAsync();
            
            // Assert
            _gremlinClientMock.Verify(
                g => g.SubmitAsync<object>(
                    It.Is<string>(q => q.Contains("g.V().drop()")),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }
        
        #endregion
    }
}
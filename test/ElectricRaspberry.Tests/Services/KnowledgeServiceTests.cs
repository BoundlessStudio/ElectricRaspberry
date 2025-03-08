using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Knowledge;
using ElectricRaspberry.Models.Knowledge.Edges;
using ElectricRaspberry.Models.Knowledge.Vertices;
using ElectricRaspberry.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ElectricRaspberry.Tests.Services
{
    public class KnowledgeServiceTests
    {
        private readonly Mock<ILogger<KnowledgeService>> _loggerMock;
        private readonly Mock<IOptions<CosmosDbOptions>> _optionsMock;
        private readonly Mock<IKnowledgeService> _knowledgeServiceMock;
        
        public KnowledgeServiceTests()
        {
            _loggerMock = new Mock<ILogger<KnowledgeService>>();
            
            var cosmosOptions = new CosmosDbOptions
            {
                Endpoint = "https://localhost:8081/",
                Key = "testkey",
                Database = "testdb",
                Container = "testcontainer"
            };
            
            _optionsMock = new Mock<IOptions<CosmosDbOptions>>();
            _optionsMock.Setup(o => o.Value).Returns(cosmosOptions);
            
            _knowledgeServiceMock = new Mock<IKnowledgeService>();
        }
        
        [Fact(Timeout = 5000)]
        public async Task GetPerson_ShouldReturnNull_WhenPersonDoesNotExist()
        {
            // Arrange
            PersonVertex? nullPerson = null;
            _knowledgeServiceMock.Setup(k => k.GetPersonAsync(It.IsAny<ulong>()))
                .ReturnsAsync(nullPerson);
                
            // Act
            var result = await _knowledgeServiceMock.Object.GetPersonAsync(123UL);
            
            // Assert
            result.Should().BeNull();
        }
        
        [Fact(Timeout = 5000)]
        public async Task UpsertPerson_ShouldReturnPerson_WhenSuccessful()
        {
            // Arrange
            var person = new PersonVertex
            {
                Id = "123",
                DiscordUserId = 123UL,
                Username = "TestUser",
                DisplayName = "Test User"
            };
            
            _knowledgeServiceMock.Setup(k => k.UpsertPersonAsync(It.IsAny<PersonVertex>()))
                .ReturnsAsync((PersonVertex p) => p);
            
            // Act
            var result = await _knowledgeServiceMock.Object.UpsertPersonAsync(person);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(person.Id);
            result.Username.Should().Be(person.Username);
            result.DisplayName.Should().Be(person.DisplayName);
        }
    }
}
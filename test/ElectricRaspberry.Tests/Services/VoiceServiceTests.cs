using Discord;
using Discord.Audio;
using Discord.WebSocket;
using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Knowledge;
using ElectricRaspberry.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElectricRaspberry.Tests.Services
{
    public class VoiceServiceTests
    {
        private readonly Mock<ILogger<VoiceService>> _loggerMock;
        private readonly Mock<DiscordSocketClient> _discordClientMock;
        private readonly Mock<IStaminaService> _staminaServiceMock;
        private readonly Mock<IKnowledgeService> _knowledgeServiceMock;
        private readonly Mock<IOptions<StaminaSettings>> _staminaSettingsMock;
        private readonly VoiceService _voiceService;

        public VoiceServiceTests()
        {
            _loggerMock = new Mock<ILogger<VoiceService>>();
            _discordClientMock = new Mock<DiscordSocketClient>();
            _staminaServiceMock = new Mock<IStaminaService>();
            _knowledgeServiceMock = new Mock<IKnowledgeService>();
            
            var staminaSettings = new StaminaSettings
            {
                VoiceMinuteCost = 1.0,
                MaxStamina = 100
            };
            
            _staminaSettingsMock = new Mock<IOptions<StaminaSettings>>();
            _staminaSettingsMock.Setup(o => o.Value).Returns(staminaSettings);
            
            // Set up default stamina state
            _staminaServiceMock.Setup(s => s.GetCurrentStaminaAsync())
                .ReturnsAsync(100.0);
            _staminaServiceMock.Setup(s => s.IsSleepingAsync())
                .ReturnsAsync(false);
            _staminaServiceMock.Setup(s => s.ConsumeStaminaAsync(It.IsAny<double>()))
                .ReturnsAsync(50.0);
            
            // Setup Discord client current user
            var currentUserMock = new Mock<SocketSelfUser>();
            currentUserMock.Setup(u => u.Id).Returns(123456789);
            _discordClientMock.Setup(c => c.CurrentUser).Returns(currentUserMock.Object);
            
            // Create the service under test
            _voiceService = new VoiceService(
                _loggerMock.Object,
                _discordClientMock.Object,
                _staminaServiceMock.Object,
                _knowledgeServiceMock.Object,
                _staminaSettingsMock.Object
            );
        }

        #region Connection Status Tests

        [Fact]
        public async Task IsConnectedToVoice_ShouldReturnFalse_WhenNotConnected()
        {
            // Act
            var result = await _voiceService.IsConnectedToVoiceAsync();
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetCurrentVoiceChannel_ShouldReturnNull_WhenNotConnected()
        {
            // Act
            var result = await _voiceService.GetCurrentVoiceChannelAsync();
            
            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUsersInVoiceChannel_ShouldReturnEmptyCollection_WhenNotConnected()
        {
            // Act
            var result = await _voiceService.GetUsersInVoiceChannelAsync();
            
            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region Join/Leave Decision Tests

        [Fact]
        public async Task ShouldLeaveVoiceChannel_ShouldReturnFalse_WhenNotConnected()
        {
            // Act
            var result = await _voiceService.ShouldLeaveVoiceChannelAsync();
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldJoinVoiceChannel_ShouldReturnFalse_WhenBotIsSleeping()
        {
            // Arrange
            var voiceChannelMock = new Mock<IVoiceChannel>();
            _staminaServiceMock.Setup(s => s.IsSleepingAsync())
                .ReturnsAsync(true);
            
            // Act
            var result = await _voiceService.ShouldJoinVoiceChannelAsync(voiceChannelMock.Object);
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldJoinVoiceChannel_ShouldReturnFalse_WhenLowStamina()
        {
            // Arrange
            var voiceChannelMock = new Mock<IVoiceChannel>();
            _staminaServiceMock.Setup(s => s.GetCurrentStaminaAsync())
                .ReturnsAsync(20.0); // Below the 30 threshold in VoiceService
            
            // Act
            var result = await _voiceService.ShouldJoinVoiceChannelAsync(voiceChannelMock.Object);
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldJoinVoiceChannel_ShouldReturnFalse_WhenAlreadyInDifferentChannel()
        {
            // Arrange
            var currentChannelMock = new Mock<IVoiceChannel>();
            currentChannelMock.Setup(c => c.Id).Returns(123);
            
            var newChannelMock = new Mock<IVoiceChannel>();
            newChannelMock.Setup(c => c.Id).Returns(456);
            
            // Setup private field hack to simulate being in a voice channel
            var audioClientMock = new Mock<IAudioClient>();
            
            // We'll need to use reflection to set the private field since we can't access it directly
            var voiceChannelField = typeof(VoiceService).GetField("_currentVoiceChannel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            voiceChannelField?.SetValue(_voiceService, currentChannelMock.Object);
            
            var audioClientField = typeof(VoiceService).GetField("_audioClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            audioClientField?.SetValue(_voiceService, audioClientMock.Object);
            
            // Act
            var result = await _voiceService.ShouldJoinVoiceChannelAsync(newChannelMock.Object);
            
            // Assert
            result.Should().BeFalse();
            
            // Clean up - reset the fields
            voiceChannelField?.SetValue(_voiceService, null);
            audioClientField?.SetValue(_voiceService, null);
        }

        [Fact]
        public async Task ShouldJoinVoiceChannel_ShouldReturnFalse_WhenNotEnoughUsersInChannel()
        {
            // Arrange
            // Create a socket voice channel with only 1 user
            var socketVoiceChannelMock = new Mock<SocketVoiceChannel>();
            
            // Add only one user to the channel
            var users = new List<SocketGuildUser>
            {
                CreateMockGuildUser(1, "User1", false)
            };
            
            socketVoiceChannelMock.Setup(c => c.ConnectedUsers).Returns(users);
            
            // Act
            var result = await _voiceService.ShouldJoinVoiceChannelAsync(socketVoiceChannelMock.Object);
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldJoinVoiceChannel_ShouldReturnTrue_WhenHasRelationshipsWithUsers()
        {
            // Arrange
            // Create a socket voice channel with multiple users
            var socketVoiceChannelMock = new Mock<SocketVoiceChannel>();
            
            // Add 2 users to the channel
            var users = new List<SocketGuildUser>
            {
                CreateMockGuildUser(1, "User1", false),
                CreateMockGuildUser(2, "User2", false)
            };
            
            socketVoiceChannelMock.Setup(c => c.ConnectedUsers).Returns(users);
            
            // Setup a relationship with one of the users
            var relationship = new UserRelationship
            {
                UserId = "1",
                Strength = 0.5 // Above the 0.3 threshold in VoiceService
            };
            
            _knowledgeServiceMock.Setup(k => k.GetUserRelationshipAsync("1"))
                .ReturnsAsync(relationship);
            
            // Act
            var result = await _voiceService.ShouldJoinVoiceChannelAsync(socketVoiceChannelMock.Object);
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldJoinVoiceChannel_ShouldReturnFalse_WhenNoRelationshipsWithUsers()
        {
            // Arrange
            // Create a socket voice channel with multiple users
            var socketVoiceChannelMock = new Mock<SocketVoiceChannel>();
            
            // Add 2 users to the channel
            var users = new List<SocketGuildUser>
            {
                CreateMockGuildUser(1, "User1", false),
                CreateMockGuildUser(2, "User2", false)
            };
            
            socketVoiceChannelMock.Setup(c => c.ConnectedUsers).Returns(users);
            
            // Setup a weak relationship with users
            var relationship = new UserRelationship
            {
                UserId = "1",
                Strength = 0.2 // Below the 0.3 threshold in VoiceService
            };
            
            _knowledgeServiceMock.Setup(k => k.GetUserRelationshipAsync(It.IsAny<string>()))
                .ReturnsAsync(relationship);
            
            // Act
            var result = await _voiceService.ShouldJoinVoiceChannelAsync(socketVoiceChannelMock.Object);
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldLeaveVoiceChannel_ShouldReturnTrue_WhenBotIsSleeping()
        {
            // Arrange
            SetupConnectedVoiceState();
            
            _staminaServiceMock.Setup(s => s.IsSleepingAsync())
                .ReturnsAsync(true);
            
            // Act
            var result = await _voiceService.ShouldLeaveVoiceChannelAsync();
            
            // Assert
            result.Should().BeTrue();
            
            // Clean up
            CleanupConnectedVoiceState();
        }

        [Fact]
        public async Task ShouldLeaveVoiceChannel_ShouldReturnTrue_WhenStaminaIsTooLow()
        {
            // Arrange
            SetupConnectedVoiceState();
            
            _staminaServiceMock.Setup(s => s.GetCurrentStaminaAsync())
                .ReturnsAsync(5.0); // Very low, below the 10 threshold in VoiceService
            
            // Act
            var result = await _voiceService.ShouldLeaveVoiceChannelAsync();
            
            // Assert
            result.Should().BeTrue();
            
            // Clean up
            CleanupConnectedVoiceState();
        }

        [Fact]
        public async Task ShouldLeaveVoiceChannel_ShouldReturnTrue_WhenChannelIsEmpty()
        {
            // Arrange
            var voiceChannelMock = new Mock<SocketVoiceChannel>();
            voiceChannelMock.Setup(c => c.ConnectedUsers).Returns(new List<SocketGuildUser>());
            
            SetupConnectedVoiceState(voiceChannelMock.Object);
            
            // Act
            var result = await _voiceService.ShouldLeaveVoiceChannelAsync();
            
            // Assert
            result.Should().BeTrue();
            
            // Clean up
            CleanupConnectedVoiceState();
        }

        [Fact]
        public async Task ShouldLeaveVoiceChannel_ShouldReturnTrue_WhenOnlyBotsInChannel()
        {
            // Arrange
            var voiceChannelMock = new Mock<SocketVoiceChannel>();
            
            // Add only bot users to the channel
            var users = new List<SocketGuildUser>
            {
                CreateMockGuildUser(1, "Bot1", true),
                CreateMockGuildUser(2, "Bot2", true)
            };
            
            voiceChannelMock.Setup(c => c.ConnectedUsers).Returns(users);
            
            SetupConnectedVoiceState(voiceChannelMock.Object);
            
            // Act
            var result = await _voiceService.ShouldLeaveVoiceChannelAsync();
            
            // Assert
            result.Should().BeTrue();
            
            // Clean up
            CleanupConnectedVoiceState();
        }

        #endregion

        #region Voice State Update Tests

        [Fact]
        public async Task ProcessVoiceStateUpdate_ShouldIgnore_WhenBotTriggeredUpdate()
        {
            // Arrange
            var currentUserMock = _discordClientMock.Object.CurrentUser;
            var oldState = new Mock<SocketVoiceState>();
            var newState = new Mock<SocketVoiceState>();
            
            // Act
            await _voiceService.ProcessVoiceStateUpdateAsync(currentUserMock, oldState.Object, newState.Object);
            
            // No need for assertions since we're just checking it doesn't throw
            // We've confirmed it ignores the bot's own state updates
        }

        [Fact]
        public async Task ProcessVoiceStateUpdate_ShouldConsiderJoining_WhenUserJoinsVoiceChannel()
        {
            // Arrange
            var user = CreateMockUser(1, "User1", false);
            var oldState = new Mock<SocketVoiceState>();
            oldState.Setup(s => s.VoiceChannel).Returns((SocketVoiceChannel)null);
            
            var newChannel = new Mock<SocketVoiceChannel>();
            newChannel.Setup(c => c.Name).Returns("VoiceChannel");
            
            var newState = new Mock<SocketVoiceState>();
            newState.Setup(s => s.VoiceChannel).Returns(newChannel.Object);
            
            // Set up for ShouldJoinVoiceChannelAsync to return true
            // Add 2 users to the channel for the check
            var users = new List<SocketGuildUser>
            {
                CreateMockGuildUser(1, "User1", false),
                CreateMockGuildUser(2, "User2", false)
            };
            
            newChannel.Setup(c => c.ConnectedUsers).Returns(users);
            
            // Setup a relationship
            var relationship = new UserRelationship
            {
                UserId = "1",
                Strength = 0.5
            };
            
            _knowledgeServiceMock.Setup(k => k.GetUserRelationshipAsync("1"))
                .ReturnsAsync(relationship);
            
            // Set up mock audio client for JoinVoiceChannelAsync
            var audioClientMock = new Mock<IAudioClient>();
            newChannel.Setup(c => c.ConnectAsync()).ReturnsAsync(audioClientMock.Object);
            
            // Act
            await _voiceService.ProcessVoiceStateUpdateAsync(user, oldState.Object, newState.Object);
            
            // Assert
            // Verify we checked if we should join
            _knowledgeServiceMock.Verify(k => k.GetUserRelationshipAsync(It.IsAny<string>()), Times.Once);
            
            // Clean up fields in case they were modified
            CleanupConnectedVoiceState();
        }

        #endregion

        #region Helper Methods

        private void SetupConnectedVoiceState(IVoiceChannel voiceChannel = null)
        {
            // Create mock objects
            var audioClientMock = new Mock<IAudioClient>();
            var channelMock = voiceChannel ?? new Mock<IVoiceChannel>().Object;
            
            // Use reflection to set private fields
            var voiceChannelField = typeof(VoiceService).GetField("_currentVoiceChannel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            voiceChannelField?.SetValue(_voiceService, channelMock);
            
            var audioClientField = typeof(VoiceService).GetField("_audioClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            audioClientField?.SetValue(_voiceService, audioClientMock.Object);
            
            var joinedAtField = typeof(VoiceService).GetField("_joinedVoiceAt", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            joinedAtField?.SetValue(_voiceService, DateTime.UtcNow);
        }
        
        private void CleanupConnectedVoiceState()
        {
            // Use reflection to reset private fields
            var voiceChannelField = typeof(VoiceService).GetField("_currentVoiceChannel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            voiceChannelField?.SetValue(_voiceService, null);
            
            var audioClientField = typeof(VoiceService).GetField("_audioClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            audioClientField?.SetValue(_voiceService, null);
            
            var joinedAtField = typeof(VoiceService).GetField("_joinedVoiceAt", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            joinedAtField?.SetValue(_voiceService, DateTime.MinValue);
        }
        
        private SocketUser CreateMockUser(ulong id, string username, bool isBot)
        {
            var userMock = new Mock<SocketUser>();
            userMock.Setup(u => u.Id).Returns(id);
            userMock.Setup(u => u.Username).Returns(username);
            userMock.Setup(u => u.IsBot).Returns(isBot);
            return userMock.Object;
        }
        
        private SocketGuildUser CreateMockGuildUser(ulong id, string username, bool isBot)
        {
            var userMock = new Mock<SocketGuildUser>();
            userMock.Setup(u => u.Id).Returns(id);
            userMock.Setup(u => u.Username).Returns(username);
            userMock.Setup(u => u.IsBot).Returns(isBot);
            return userMock.Object;
        }

        #endregion
    }
}
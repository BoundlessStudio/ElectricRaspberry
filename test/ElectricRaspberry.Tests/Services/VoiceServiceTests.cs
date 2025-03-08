using Discord;
using Discord.Audio;
using Discord.WebSocket;
using ElectricRaspberry.Configuration;
using ElectricRaspberry.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
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
                VoiceMinuteCost = 1.0
            };
            
            _staminaSettingsMock = new Mock<IOptions<StaminaSettings>>();
            _staminaSettingsMock.Setup(o => o.Value).Returns(staminaSettings);
            
            // Set up default stamina state
            _staminaServiceMock.Setup(s => s.GetCurrentStaminaAsync())
                .ReturnsAsync(100.0);
            _staminaServiceMock.Setup(s => s.IsSleepingAsync())
                .ReturnsAsync(false);
            
            _voiceService = new VoiceService(
                _loggerMock.Object,
                _discordClientMock.Object,
                _staminaServiceMock.Object,
                _knowledgeServiceMock.Object,
                _staminaSettingsMock.Object
            );
        }

        [Fact]
        public async Task IsConnectedToVoice_ShouldReturnFalse_WhenNotConnected()
        {
            // Act
            var result = await _voiceService.IsConnectedToVoiceAsync();
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldLeaveVoiceChannel_ShouldReturnFalse_WhenNotConnected()
        {
            // Act
            var result = await _voiceService.ShouldLeaveVoiceChannelAsync();
            
            // Assert
            Assert.False(result);
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
            Assert.False(result);
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
            Assert.False(result);
        }
    }
}
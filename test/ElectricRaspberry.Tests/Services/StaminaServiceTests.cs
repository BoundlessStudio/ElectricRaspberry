using Discord;
using Discord.WebSocket;
using ElectricRaspberry.Configuration;
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
    public class StaminaServiceTests
    {
        private readonly Mock<ILogger<StaminaService>> _loggerMock;
        private readonly Mock<IOptions<StaminaSettings>> _optionsMock;
        private readonly Mock<IDiscordClient> _discordClientMock;
        private readonly Mock<DiscordSocketClient> _discordSocketClientMock;
        private readonly StaminaSettings _staminaSettings;
        private readonly StaminaService _staminaService;

        public StaminaServiceTests()
        {
            _loggerMock = new Mock<ILogger<StaminaService>>();
            
            _staminaSettings = new StaminaSettings
            {
                MaxStamina = 100,
                MessageCost = 0.5,
                VoiceMinuteCost = 1.0,
                EmotionalSpikeCost = 2.0,
                RecoveryRatePerMinute = 0.2,
                SleepRecoveryMultiplier = 3.0,
                LowStaminaThreshold = 20
            };
            
            _optionsMock = new Mock<IOptions<StaminaSettings>>();
            _optionsMock.Setup(o => o.Value).Returns(_staminaSettings);
            
            // Setup both IDiscordClient and DiscordSocketClient
            _discordSocketClientMock = new Mock<DiscordSocketClient>();
            _discordSocketClientMock.Setup(c => c.SetStatusAsync(It.IsAny<UserStatus>()))
                .Returns(Task.CompletedTask);
            _discordSocketClientMock.Setup(c => c.SetActivityAsync(It.IsAny<IActivity>()))
                .Returns(Task.CompletedTask);
                
            // Use the socket client for the IDiscordClient
            _discordClientMock = new Mock<IDiscordClient>();
            
            _staminaService = new StaminaService(
                _loggerMock.Object,
                _optionsMock.Object,
                _discordClientMock.Object
            );
        }

        [Fact]
        public async Task GetCurrentStamina_ShouldReturnMaxStamina_WhenInitialized()
        {
            // Act
            var result = await _staminaService.GetCurrentStaminaAsync();
            
            // Assert
            result.Should().Be(_staminaSettings.MaxStamina);
        }

        [Fact]
        public async Task ConsumeStamina_ShouldReduceStamina_BySpecifiedAmount()
        {
            // Arrange
            double amountToConsume = 10;
            
            // Act
            var result = await _staminaService.ConsumeStaminaAsync(amountToConsume);
            
            // Assert
            result.Should().Be(_staminaSettings.MaxStamina - amountToConsume);
        }

        [Fact]
        public async Task ConsumeStamina_ShouldNotGoBelowZero_WhenConsumingMoreThanAvailable()
        {
            // Arrange
            double amountToConsume = _staminaSettings.MaxStamina + 10;
            
            // Act
            var result = await _staminaService.ConsumeStaminaAsync(amountToConsume);
            
            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task IsSleeping_ShouldReturnFalse_WhenInitialized()
        {
            // Act
            var result = await _staminaService.IsSleepingAsync();
            
            // Assert
            result.Should().BeFalse();
        }
    }
}
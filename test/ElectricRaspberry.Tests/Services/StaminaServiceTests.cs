using Discord;
using ElectricRaspberry.Configuration;
using ElectricRaspberry.Services;
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
            Assert.Equal(_staminaSettings.MaxStamina, result);
        }

        [Fact]
        public async Task ConsumeStamina_ShouldReduceStamina_BySpecifiedAmount()
        {
            // Arrange
            double amountToConsume = 10;
            
            // Act
            var result = await _staminaService.ConsumeStaminaAsync(amountToConsume);
            
            // Assert
            Assert.Equal(_staminaSettings.MaxStamina - amountToConsume, result);
        }

        [Fact]
        public async Task IsSleeping_ShouldReturnFalse_WhenInitialized()
        {
            // Act
            var result = await _staminaService.IsSleepingAsync();
            
            // Assert
            Assert.False(result);
        }
    }
}
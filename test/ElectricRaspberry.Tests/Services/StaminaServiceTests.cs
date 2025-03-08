using Discord;
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
            // Setup Discord client methods that are called during sleep/wake transitions
            _discordClientMock.Setup(c => c.SetStatusAsync(It.IsAny<UserStatus>()))
                .Returns(Task.CompletedTask);
            _discordClientMock.Setup(c => c.SetActivityAsync(It.IsAny<IActivity>()))
                .Returns(Task.CompletedTask);
            
            _staminaService = new StaminaService(
                _loggerMock.Object,
                _optionsMock.Object,
                _discordClientMock.Object
            );
        }

        #region Initial State Tests

        [Fact]
        public async Task GetCurrentStamina_ShouldReturnMaxStamina_WhenInitialized()
        {
            // Act
            var result = await _staminaService.GetCurrentStaminaAsync();
            
            // Assert
            result.Should().Be(_staminaSettings.MaxStamina);
        }

        [Fact]
        public async Task IsSleeping_ShouldReturnFalse_WhenInitialized()
        {
            // Act
            var result = await _staminaService.IsSleepingAsync();
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldSleep_ShouldReturnFalse_WhenStaminaIsMax()
        {
            // Act
            var result = await _staminaService.ShouldSleepAsync();
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldWake_ShouldReturnTrue_WhenStaminaIsMax()
        {
            // Act
            var result = await _staminaService.ShouldWakeAsync();
            
            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Stamina Consumption Tests

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
        public async Task ConsumeStamina_ShouldEnterSleepMode_WhenStaminaDropsBelowThreshold()
        {
            // Arrange
            double amountToConsume = _staminaSettings.MaxStamina - _staminaSettings.LowStaminaThreshold + 1;
            
            // Act
            await _staminaService.ConsumeStaminaAsync(amountToConsume);
            var sleepingStatus = await _staminaService.IsSleepingAsync();
            
            // Assert
            sleepingStatus.Should().BeTrue();
            
            // Verify Discord client was updated
            _discordClientMock.Verify(c => c.SetStatusAsync(UserStatus.Idle), Times.Once);
            _discordClientMock.Verify(c => c.SetActivityAsync(It.IsAny<IActivity>()), Times.Once);
        }

        [Fact]
        public async Task ConsumeStamina_ShouldNotEnterSleepMode_WhenStaminaRemainsAboveThreshold()
        {
            // Arrange
            double amountToConsume = _staminaSettings.MaxStamina - _staminaSettings.LowStaminaThreshold - 1;
            
            // Act
            await _staminaService.ConsumeStaminaAsync(amountToConsume);
            var sleepingStatus = await _staminaService.IsSleepingAsync();
            
            // Assert
            sleepingStatus.Should().BeFalse();
            
            // Verify Discord client was not updated
            _discordClientMock.Verify(c => c.SetStatusAsync(UserStatus.Idle), Times.Never);
        }

        #endregion

        #region Stamina Recovery Tests

        [Fact]
        public async Task RecoverStamina_ShouldIncreaseStamina_ByTimeAndRecoveryRate()
        {
            // Arrange
            double minutes = 10;
            double expectedRecovery = minutes * _staminaSettings.RecoveryRatePerMinute;
            
            // First consume some stamina
            await _staminaService.ConsumeStaminaAsync(20);
            var initialStamina = await _staminaService.GetCurrentStaminaAsync();
            
            // Act
            var result = await _staminaService.RecoverStaminaAsync(minutes);
            
            // Assert
            result.Should().Be(initialStamina + expectedRecovery);
        }

        [Fact]
        public async Task RecoverStamina_ShouldNotExceedMaxStamina_WhenRecoveringLargeAmount()
        {
            // Arrange
            double minutes = 1000; // Very large amount
            
            // First consume some stamina
            await _staminaService.ConsumeStaminaAsync(20);
            
            // Act
            var result = await _staminaService.RecoverStaminaAsync(minutes);
            
            // Assert
            result.Should().Be(_staminaSettings.MaxStamina);
        }

        [Fact]
        public async Task RecoverStamina_ShouldApplySleepMultiplier_WhenSleeping()
        {
            // Arrange
            double minutes = 10;
            double expectedRegularRecovery = minutes * _staminaSettings.RecoveryRatePerMinute;
            double expectedSleepRecovery = minutes * _staminaSettings.RecoveryRatePerMinute * _staminaSettings.SleepRecoveryMultiplier;
            
            // Consume enough stamina to enter sleep mode
            await _staminaService.ConsumeStaminaAsync(_staminaSettings.MaxStamina - _staminaSettings.LowStaminaThreshold + 1);
            var initialStamina = await _staminaService.GetCurrentStaminaAsync();
            
            // Verify we're sleeping
            var sleepingStatus = await _staminaService.IsSleepingAsync();
            sleepingStatus.Should().BeTrue();
            
            // Act
            var result = await _staminaService.RecoverStaminaAsync(minutes);
            
            // Assert
            // The recovery should be greater than regular recovery due to sleep multiplier
            (result - initialStamina).Should().BeApproximately(expectedSleepRecovery, 0.001);
            (result - initialStamina).Should().BeGreaterThan(expectedRegularRecovery);
        }

        [Fact]
        public async Task RecoverStamina_ShouldExitSleepMode_WhenStaminaReachesWakeThreshold()
        {
            // Arrange
            // First enter sleep mode by consuming stamina
            await _staminaService.ConsumeStaminaAsync(_staminaSettings.MaxStamina - _staminaSettings.LowStaminaThreshold + 1);
            var sleepingStatus = await _staminaService.IsSleepingAsync();
            sleepingStatus.Should().BeTrue();
            
            // Calculate how many minutes needed to wake up
            // Wake threshold is 80% of max stamina
            var currentStamina = await _staminaService.GetCurrentStaminaAsync();
            var staminaNeededToWake = (_staminaSettings.MaxStamina * 0.8) - currentStamina;
            var minutesNeeded = staminaNeededToWake / (_staminaSettings.RecoveryRatePerMinute * _staminaSettings.SleepRecoveryMultiplier);
            
            // Act
            await _staminaService.RecoverStaminaAsync(minutesNeeded + 1); // Add 1 to ensure we cross the threshold
            var finalSleepingStatus = await _staminaService.IsSleepingAsync();
            
            // Assert
            finalSleepingStatus.Should().BeFalse();
            
            // Verify Discord client was updated
            _discordClientMock.Verify(c => c.SetStatusAsync(UserStatus.Online), Times.Once);
            _discordClientMock.Verify(c => c.SetActivityAsync(It.IsAny<IActivity>()), Times.Exactly(2)); // Once for sleep, once for wake
        }

        #endregion

        #region Forced Sleep/Wake Tests

        [Fact]
        public async Task ForceSleepMode_ShouldEnterSleepMode_RegardlessOfStamina()
        {
            // Act
            await _staminaService.ForceSleepModeAsync();
            var sleepingStatus = await _staminaService.IsSleepingAsync();
            
            // Assert
            sleepingStatus.Should().BeTrue();
            
            // Verify Discord client was updated
            _discordClientMock.Verify(c => c.SetStatusAsync(UserStatus.Idle), Times.Once);
            _discordClientMock.Verify(c => c.SetActivityAsync(It.IsAny<IActivity>()), Times.Once);
        }

        [Fact]
        public async Task ForceSleepMode_ShouldSetSleepUntil_WhenDurationProvided()
        {
            // Arrange
            var duration = TimeSpan.FromMinutes(10);
            
            // Act
            await _staminaService.ForceSleepModeAsync(duration);
            var sleepingStatus = await _staminaService.IsSleepingAsync();
            
            // Assert
            sleepingStatus.Should().BeTrue();
        }

        [Fact]
        public async Task ForceWake_ShouldExitSleepMode_RegardlessOfStamina()
        {
            // Arrange
            // First enter sleep mode
            await _staminaService.ForceSleepModeAsync();
            
            // Act
            await _staminaService.ForceWakeAsync();
            var sleepingStatus = await _staminaService.IsSleepingAsync();
            
            // Assert
            sleepingStatus.Should().BeFalse();
            
            // Verify Discord client was updated
            _discordClientMock.Verify(c => c.SetStatusAsync(UserStatus.Online), Times.Once);
            _discordClientMock.Verify(c => c.SetActivityAsync(It.IsAny<IActivity>()), Times.Exactly(2)); // Once for sleep, once for wake
        }

        #endregion

        #region Reset Tests

        [Fact]
        public async Task ResetStamina_ShouldSetStaminaToMax()
        {
            // Arrange
            // First consume some stamina
            await _staminaService.ConsumeStaminaAsync(50);
            var reducedStamina = await _staminaService.GetCurrentStaminaAsync();
            reducedStamina.Should().Be(50);
            
            // Act
            await _staminaService.ResetStaminaAsync();
            var result = await _staminaService.GetCurrentStaminaAsync();
            
            // Assert
            result.Should().Be(_staminaSettings.MaxStamina);
        }

        [Fact]
        public async Task ResetStamina_ShouldWakeFromSleep_WhenSleeping()
        {
            // Arrange
            // First enter sleep mode
            await _staminaService.ForceSleepModeAsync();
            var sleepingStatus = await _staminaService.IsSleepingAsync();
            sleepingStatus.Should().BeTrue();
            
            // Act
            await _staminaService.ResetStaminaAsync();
            
            // Assert
            var finalSleepingStatus = await _staminaService.IsSleepingAsync();
            finalSleepingStatus.Should().BeFalse();
            
            // Verify Discord client was updated
            _discordClientMock.Verify(c => c.SetStatusAsync(UserStatus.Online), Times.Once);
        }

        #endregion
    }
}
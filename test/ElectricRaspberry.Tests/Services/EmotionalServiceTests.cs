using ElectricRaspberry.Models.Emotions;
using ElectricRaspberry.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ElectricRaspberry.Tests.Services
{
    public class EmotionalServiceTests
    {
        private readonly Mock<ILogger<EmotionalService>> _loggerMock;
        private readonly Mock<IStaminaService> _staminaServiceMock;
        private readonly EmotionalService _emotionalService;

        public EmotionalServiceTests()
        {
            _loggerMock = new Mock<ILogger<EmotionalService>>();
            _staminaServiceMock = new Mock<IStaminaService>();
            
            // Set up stamina service to return a default value
            _staminaServiceMock.Setup(s => s.GetCurrentStaminaAsync())
                .ReturnsAsync(100.0);
            _staminaServiceMock.Setup(s => s.ConsumeStaminaAsync(It.IsAny<double>()))
                .ReturnsAsync(90.0);
            
            _emotionalService = new EmotionalService(
                _loggerMock.Object,
                _staminaServiceMock.Object
            );
        }

        #region Initial State Tests

        [Fact]
        public async Task GetCurrentEmotionalState_ShouldReturnInitialState_WhenNoChanges()
        {
            // Act
            var state = await _emotionalService.GetCurrentEmotionalStateAsync();
            
            // Assert
            state.Should().NotBeNull();
            
            // Verify default core emotions are initialized to neutral (50)
            state.GetEmotion(CoreEmotions.Joy).Should().Be(50);
            state.GetEmotion(CoreEmotions.Sadness).Should().Be(50);
            state.GetEmotion(CoreEmotions.Anger).Should().Be(50);
            state.GetEmotion(CoreEmotions.Fear).Should().Be(50);
            state.GetEmotion(CoreEmotions.Surprise).Should().Be(50);
            state.GetEmotion(CoreEmotions.Disgust).Should().Be(50);
            
            // Verify valence and arousal are neutral
            state.GetValence().Should().BeApproximately(0, 0.001);
            state.GetArousal().Should().BeApproximately(0.5, 0.001);
            
            // Verify state indicators
            state.IsPositive.Should().BeFalse();
            state.IsNegative.Should().BeFalse();
            state.IsEnergetic.Should().BeFalse();
            state.IsCalm.Should().BeTrue();
        }

        [Fact]
        public async Task GetCurrentExpression_ShouldReturnNeutralExpression_WhenNoChanges()
        {
            // Act
            var expression = await _emotionalService.GetCurrentExpressionAsync();
            
            // Assert
            expression.Should().NotBeNull();
            expression.State.Should().NotBeNull();
            expression.Tone.Should().NotBeNull();
            expression.SuggestedEmojis.Should().NotBeNull();
            expression.CommunicationModifiers.Should().NotBeNull();
            
            // With default emotional state (all at 50), 
            // we expect the tone to be neutral and no emojis
            expression.SuggestedEmojis.Should().BeEmpty();
        }

        #endregion

        #region Trigger Processing Tests

        [Fact]
        public async Task ProcessEmotionalTrigger_ShouldUpdateEmotionalState()
        {
            // Arrange
            var trigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.Praise,
                Intensity = 0.8,
                EmotionChanges = new()
                {
                    { CoreEmotions.Joy, 20 },
                    { CoreEmotions.Sadness, -10 }
                }
            };
            
            // Act
            await _emotionalService.ProcessEmotionalTriggerAsync(trigger);
            var state = await _emotionalService.GetCurrentEmotionalStateAsync();
            
            // Assert
            state.GetEmotion(CoreEmotions.Joy).Should().BeGreaterThan(50);
            state.GetEmotion(CoreEmotions.Sadness).Should().BeLessThan(50);
            state.IsPositive.Should().BeTrue();
            
            // Verify that stamina was consumed
            _staminaServiceMock.Verify(s => s.ConsumeStaminaAsync(It.IsAny<double>()), Times.Once);
        }

        [Fact]
        public async Task ProcessEmotionalTrigger_ShouldApplyPersonalityModifiers()
        {
            // Arrange
            // Create a trigger affecting Joy, which has a sensitivity of 1.5 in the personality profile
            var trigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.Praise,
                Intensity = 0.7,
                EmotionChanges = new()
                {
                    { CoreEmotions.Joy, 10 }
                }
            };
            
            // Calculate the expected value based on the personality sensitivity
            var impact = _emotionalService.CalculateEmotionalImpact(trigger);
            
            // Act
            await _emotionalService.ProcessEmotionalTriggerAsync(trigger);
            var state = await _emotionalService.GetCurrentEmotionalStateAsync();
            
            // Assert
            // The impact on Joy should be modified by the personality (1.5x)
            // However, since we can't access the private personality field directly,
            // we'll verify indirectly through the impact calculation
            impact.Changes[CoreEmotions.Joy].Should().BeGreaterThan(10);
            state.GetEmotion(CoreEmotions.Joy).Should().BeGreaterThan(50);
        }

        [Fact]
        public async Task ProcessEmotionalTrigger_ShouldClampEmotionsWithinValidRange()
        {
            // Arrange
            var trigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.Criticism,
                Intensity = 1.0,
                EmotionChanges = new()
                {
                    { CoreEmotions.Sadness, 100 },
                    { CoreEmotions.Joy, -100 }
                }
            };
            
            // Act
            await _emotionalService.ProcessEmotionalTriggerAsync(trigger);
            var state = await _emotionalService.GetCurrentEmotionalStateAsync();
            
            // Assert
            // Emotions should be clamped to the range 0-100
            state.GetEmotion(CoreEmotions.Sadness).Should().BeLessThanOrEqualTo(100);
            state.GetEmotion(CoreEmotions.Joy).Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion

        #region Emotion Calculation Tests

        [Fact]
        public void CalculateEmotionalImpact_ShouldApplyPersonalityModifiers()
        {
            // Arrange
            var trigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.Praise,
                Intensity = 0.7,
                EmotionChanges = new()
                {
                    { CoreEmotions.Joy, 10 }
                }
            };
            
            // Act
            var impact = _emotionalService.CalculateEmotionalImpact(trigger);
            
            // Assert
            impact.Should().NotBeNull();
            impact.Changes.Should().ContainKey(CoreEmotions.Joy);
            impact.Changes[CoreEmotions.Joy].Should().NotBe(10); // Should be modified by personality
            impact.Significance.Should().Be(trigger.Intensity);
        }

        [Fact]
        public void CalculateEmotionalStaminaCost_ShouldIncreaseForHighArousalEmotions()
        {
            // Arrange
            // Create two impacts, one with high-arousal emotions and one with low-arousal emotions
            var highArousalTrigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.Praise,
                Intensity = 0.8,
                EmotionChanges = new()
                {
                    { CoreEmotions.Joy, 20 } // Joy is a high-arousal emotion
                }
            };
            
            var lowArousalTrigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.Criticism,
                Intensity = 0.8,
                EmotionChanges = new()
                {
                    { CoreEmotions.Sadness, 20 } // Sadness is a low-arousal emotion
                }
            };
            
            var highArousalImpact = _emotionalService.CalculateEmotionalImpact(highArousalTrigger);
            var lowArousalImpact = _emotionalService.CalculateEmotionalImpact(lowArousalTrigger);
            
            // Manually set the changes to be > 15 to trigger the condition
            using (var reflection = new ReflectionHelper<EmotionalImpact>(highArousalImpact))
            {
                reflection.GetField<Dictionary<string, double>>("Changes")[CoreEmotions.Joy] = 20;
            }
            
            using (var reflection = new ReflectionHelper<EmotionalImpact>(lowArousalImpact))
            {
                reflection.GetField<Dictionary<string, double>>("Changes")[CoreEmotions.Sadness] = 20;
            }
            
            // Act
            var highArousalCost = _emotionalService.CalculateEmotionalStaminaCost(highArousalImpact);
            var lowArousalCost = _emotionalService.CalculateEmotionalStaminaCost(lowArousalImpact);
            
            // Assert
            // High-arousal emotions should cost more stamina
            highArousalCost.Should().BeGreaterThan(lowArousalCost);
        }

        [Fact]
        public void CalculateEmotionalStaminaCost_ShouldScaleWithImpactSignificance()
        {
            // Arrange
            var lowSignificanceTrigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.DirectMessage,
                Intensity = 0.3,
                EmotionChanges = new()
                {
                    { CoreEmotions.Surprise, 5 }
                }
            };
            
            var highSignificanceTrigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.DirectMessage,
                Intensity = 0.9,
                EmotionChanges = new()
                {
                    { CoreEmotions.Surprise, 5 }
                }
            };
            
            var lowSignificanceImpact = _emotionalService.CalculateEmotionalImpact(lowSignificanceTrigger);
            var highSignificanceImpact = _emotionalService.CalculateEmotionalImpact(highSignificanceTrigger);
            
            // Act
            var lowSignificanceCost = _emotionalService.CalculateEmotionalStaminaCost(lowSignificanceImpact);
            var highSignificanceCost = _emotionalService.CalculateEmotionalStaminaCost(highSignificanceImpact);
            
            // Assert
            highSignificanceCost.Should().BeGreaterThan(lowSignificanceCost);
        }

        #endregion

        #region Message Trigger Tests

        [Theory]
        [InlineData("Hello, how are you today?", true)]
        [InlineData("I'm feeling sad and angry about this situation.", false)]
        public async Task CreateTriggerFromMessage_ShouldGenerateAppropriateEmotionChanges(string message, bool expectPositive)
        {
            // Act
            var trigger = await _emotionalService.CreateTriggerFromMessageAsync(message, "user123", "msg456");
            
            // Assert
            trigger.Should().NotBeNull();
            trigger.Type.Should().Be(EmotionalTriggerType.DirectMessage);
            trigger.Source.Should().Be("user123");
            trigger.ContentId.Should().Be("msg456");
            trigger.EmotionChanges.Should().ContainKey(CoreEmotions.Joy);
            trigger.EmotionChanges.Should().ContainKey(CoreEmotions.Sadness);
            
            if (expectPositive)
            {
                trigger.EmotionChanges[CoreEmotions.Joy].Should().BePositive();
                trigger.EmotionChanges[CoreEmotions.Sadness].Should().BeNegative();
            }
            else
            {
                trigger.EmotionChanges[CoreEmotions.Sadness].Should().BePositive();
                trigger.EmotionChanges[CoreEmotions.Joy].Should().BeNegative();
            }
        }

        [Fact]
        public async Task CreateTriggerFromMessage_ShouldTruncateLongMessages()
        {
            // Arrange
            var longMessage = new string('a', 200);
            
            // Act
            var trigger = await _emotionalService.CreateTriggerFromMessageAsync(longMessage, "user123", "msg456");
            
            // Assert
            trigger.Context.Length.Should().BeLessThan(longMessage.Length);
            trigger.Context.Should().EndWith("...");
        }

        #endregion

        #region Emotional Maintenance Tests

        [Fact]
        public async Task PerformEmotionalMaintenance_ShouldMoveEmotionsTowardBaseline()
        {
            // Arrange
            // First process a trigger to move away from baseline
            var trigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.Praise,
                Intensity = 0.8,
                EmotionChanges = new()
                {
                    { CoreEmotions.Joy, 30 }
                }
            };
            await _emotionalService.ProcessEmotionalTriggerAsync(trigger);
            
            // Verify emotion is changed from baseline
            var beforeMaintenance = await _emotionalService.GetCurrentEmotionalStateAsync();
            beforeMaintenance.GetEmotion(CoreEmotions.Joy).Should().BeGreaterThan(50);
            
            // Act
            await _emotionalService.PerformEmotionalMaintenanceAsync();
            
            // Assert
            var afterMaintenance = await _emotionalService.GetCurrentEmotionalStateAsync();
            
            // The emotion should move toward baseline after maintenance
            // Since the baseline for joy is 60 in the personality profile,
            // and we pushed it above that, it should decrease
            if (beforeMaintenance.GetEmotion(CoreEmotions.Joy) > 60)
            {
                afterMaintenance.GetEmotion(CoreEmotions.Joy).Should().BeLessThan(beforeMaintenance.GetEmotion(CoreEmotions.Joy));
            }
            // If we didn't push it above the personality baseline, the test premise is invalid
            else
            {
                beforeMaintenance.GetEmotion(CoreEmotions.Joy).Should().BeGreaterThan(60);
            }
        }

        [Fact]
        public async Task PerformEmotionalMaintenance_ShouldConsiderStaminaForRecoveryRate()
        {
            // Arrange
            // First process a trigger to move away from baseline
            var trigger = new EmotionalTrigger
            {
                Type = EmotionalTriggerType.Criticism,
                Intensity = 0.8,
                EmotionChanges = new()
                {
                    { CoreEmotions.Sadness, 30 }
                }
            };
            await _emotionalService.ProcessEmotionalTriggerAsync(trigger);
            
            // Set up two different stamina levels
            _staminaServiceMock.Setup(s => s.GetCurrentStaminaAsync())
                .ReturnsAsync(20.0); // Low stamina slows recovery
            
            // Act 
            // Perform maintenance with low stamina
            await _emotionalService.PerformEmotionalMaintenanceAsync();
            var afterLowStaminaMaintenance = await _emotionalService.GetCurrentEmotionalStateAsync();
            
            // Change stamina to high and perform maintenance again
            _staminaServiceMock.Setup(s => s.GetCurrentStaminaAsync())
                .ReturnsAsync(100.0); // High stamina allows normal recovery
            await _emotionalService.PerformEmotionalMaintenanceAsync();
            var afterHighStaminaMaintenance = await _emotionalService.GetCurrentEmotionalStateAsync();
            
            // Assert
            // The second maintenance (with high stamina) should have a bigger effect
            // Assuming the sadness was above baseline to begin with
            var movementWithLowStamina = afterLowStaminaMaintenance.GetEmotion(CoreEmotions.Sadness);
            var movementWithHighStamina = afterHighStaminaMaintenance.GetEmotion(CoreEmotions.Sadness);
            
            // The high stamina maintenance should result in a faster return toward baseline
            // which means lower sadness (since we increased it)
            movementWithHighStamina.Should().BeLessThan(movementWithLowStamina);
        }

        #endregion
        
        #region Helper Classes
        
        /// <summary>
        /// Helper class for accessing private fields through reflection
        /// </summary>
        private class ReflectionHelper<T> : IDisposable
        {
            private readonly T _instance;
            
            public ReflectionHelper(T instance)
            {
                _instance = instance;
            }
            
            public TField GetField<TField>(string fieldName)
            {
                var field = typeof(T).GetField(fieldName, 
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                
                return field == null 
                    ? default 
                    : (TField)field.GetValue(_instance);
            }
            
            public void SetField<TField>(string fieldName, TField value)
            {
                var field = typeof(T).GetField(fieldName, 
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                
                field?.SetValue(_instance, value);
            }
            
            public void Dispose()
            {
                // Nothing to dispose, but required for using statement
            }
        }
        
        #endregion
    }
}
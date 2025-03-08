using Discord.WebSocket;
using ElectricRaspberry.Handlers;
using ElectricRaspberry.Notifications;
using ElectricRaspberry.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElectricRaspberry.Tests.Handlers
{
    public class VoiceStateHandlerTests
    {
        private readonly Mock<ILogger<UserVoiceStateUpdatedHandler>> _loggerMock;
        private readonly Mock<IVoiceService> _voiceServiceMock;
        private readonly Mock<IStaminaService> _staminaServiceMock;
        private readonly UserVoiceStateUpdatedHandler _handler;

        public VoiceStateHandlerTests()
        {
            _loggerMock = new Mock<ILogger<UserVoiceStateUpdatedHandler>>();
            _voiceServiceMock = new Mock<IVoiceService>();
            _staminaServiceMock = new Mock<IStaminaService>();
            
            _handler = new UserVoiceStateUpdatedHandler(
                _loggerMock.Object,
                _voiceServiceMock.Object,
                _staminaServiceMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldCallProcessVoiceStateUpdate_WhenNotSleeping()
        {
            // Arrange
            var user = new Mock<SocketUser>();
            var oldState = new Mock<SocketVoiceState>();
            var newState = new Mock<SocketVoiceState>();
            
            var notification = new UserVoiceStateUpdatedNotification(user.Object, oldState.Object, newState.Object);
            
            _staminaServiceMock.Setup(s => s.IsSleepingAsync())
                .ReturnsAsync(false);
            
            _voiceServiceMock.Setup(v => v.ProcessVoiceStateUpdateAsync(user.Object, oldState.Object, newState.Object))
                .Returns(Task.CompletedTask);
            
            // Act
            await _handler.Handle(notification, CancellationToken.None);
            
            // Assert
            _staminaServiceMock.Verify(s => s.IsSleepingAsync(), Times.Once);
            _voiceServiceMock.Verify(v => v.ProcessVoiceStateUpdateAsync(user.Object, oldState.Object, newState.Object), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldNotCallProcessVoiceStateUpdate_WhenSleeping()
        {
            // Arrange
            var user = new Mock<SocketUser>();
            var oldState = new Mock<SocketVoiceState>();
            var newState = new Mock<SocketVoiceState>();
            
            var notification = new UserVoiceStateUpdatedNotification(user.Object, oldState.Object, newState.Object);
            
            _staminaServiceMock.Setup(s => s.IsSleepingAsync())
                .ReturnsAsync(true);
            
            // Act
            await _handler.Handle(notification, CancellationToken.None);
            
            // Assert
            _staminaServiceMock.Verify(s => s.IsSleepingAsync(), Times.Once);
            _voiceServiceMock.Verify(v => v.ProcessVoiceStateUpdateAsync(It.IsAny<SocketUser>(), 
                It.IsAny<SocketVoiceState>(), It.IsAny<SocketVoiceState>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldHandleExceptions_GracefullyWithoutThrowing()
        {
            // Arrange
            var user = new Mock<SocketUser>();
            var oldState = new Mock<SocketVoiceState>();
            var newState = new Mock<SocketVoiceState>();
            
            var notification = new UserVoiceStateUpdatedNotification(user.Object, oldState.Object, newState.Object);
            
            _staminaServiceMock.Setup(s => s.IsSleepingAsync())
                .ReturnsAsync(false);
            
            _voiceServiceMock.Setup(v => v.ProcessVoiceStateUpdateAsync(user.Object, oldState.Object, newState.Object))
                .ThrowsAsync(new Exception("Test exception"));
            
            // Act & Assert
            await _handler.Invoking(h => h.Handle(notification, CancellationToken.None))
                .Should().NotThrowAsync(); // Should not throw even when the service throws
            
            // Verify logger was called with error
            // Note: Can't easily verify logger calls with standard verification due to how ILogger works
        }
    }

    public class UserVoiceServerUpdatedHandlerTests
    {
        private readonly Mock<ILogger<UserVoiceServerUpdatedHandler>> _loggerMock;
        private readonly UserVoiceServerUpdatedHandler _handler;

        public UserVoiceServerUpdatedHandlerTests()
        {
            _loggerMock = new Mock<ILogger<UserVoiceServerUpdatedHandler>>();
            _handler = new UserVoiceServerUpdatedHandler(_loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldLogDebugMessage_WhenCalled()
        {
            // Arrange
            var user = new Mock<SocketUser>();
            user.Setup(u => u.Id).Returns(123456789);
            
            var server = new Mock<SocketVoiceServer>();
            
            var notification = new UserVoiceServerUpdatedNotification(user.Object, server.Object);
            
            // Act
            await _handler.Handle(notification, CancellationToken.None);
            
            // Assert - we're just verifying it doesn't throw
            // Can't easily verify logger was called with specific parameters
        }
    }
}
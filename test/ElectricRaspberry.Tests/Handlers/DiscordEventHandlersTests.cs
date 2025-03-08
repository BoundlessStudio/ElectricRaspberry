using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ElectricRaspberry.Handlers;
using ElectricRaspberry.Notifications;
using ElectricRaspberry.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace ElectricRaspberry.Tests.Handlers
{
    public class DiscordEventHandlersTests
    {
        #region ReadyNotificationHandler Tests

        public class ReadyNotificationHandlerTests
        {
            private readonly Mock<ILogger<ReadyNotificationHandler>> _loggerMock;
            private readonly ReadyNotificationHandler _handler;

            public ReadyNotificationHandlerTests()
            {
                _loggerMock = new Mock<ILogger<ReadyNotificationHandler>>();
                _handler = new ReadyNotificationHandler(_loggerMock.Object);
            }

            [Fact]
            public async Task Handle_WhenCalled_LogsReadyMessage()
            {
                // Arrange
                var notification = new ReadyNotification();
                
                // Act
                await _handler.Handle(notification, CancellationToken.None);
                
                // Assert
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Discord client is ready")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
            }
        }

        #endregion

        #region LogNotificationHandler Tests

        public class LogNotificationHandlerTests
        {
            private readonly Mock<ILogger<LogNotificationHandler>> _loggerMock;
            private readonly LogNotificationHandler _handler;

            public LogNotificationHandlerTests()
            {
                _loggerMock = new Mock<ILogger<LogNotificationHandler>>();
                _handler = new LogNotificationHandler(_loggerMock.Object);
            }

            [Theory]
            [InlineData(LogSeverity.Critical, LogLevel.Critical)]
            [InlineData(LogSeverity.Error, LogLevel.Error)]
            [InlineData(LogSeverity.Warning, LogLevel.Warning)]
            [InlineData(LogSeverity.Info, LogLevel.Information)]
            [InlineData(LogSeverity.Verbose, LogLevel.Debug)]
            [InlineData(LogSeverity.Debug, LogLevel.Trace)]
            public async Task Handle_WithDifferentSeverityLevels_MapsToCorrectLogLevel(LogSeverity discordSeverity, LogLevel expectedLogLevel)
            {
                // Arrange
                var logMessage = new LogMessage(discordSeverity, "TestSource", "Test message");
                var notification = new LogNotification(logMessage);
                
                // Act
                await _handler.Handle(notification, CancellationToken.None);
                
                // Assert
                _loggerMock.Verify(
                    x => x.Log(
                        expectedLogLevel,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(logMessage.Message)),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
            }

            [Fact]
            public async Task Handle_WithExceptionInLogMessage_LogsExceptionDetails()
            {
                // Arrange
                var testException = new InvalidOperationException("Test exception");
                var logMessage = new LogMessage(LogSeverity.Error, "TestSource", "Error occurred", testException);
                var notification = new LogNotification(logMessage);
                
                // Act
                await _handler.Handle(notification, CancellationToken.None);
                
                // Assert
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(logMessage.Message)),
                        It.Is<Exception>(e => e == testException),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
            }
        }

        #endregion

        #region MessageReceivedNotificationHandler Tests

        public class MessageReceivedNotificationHandlerTests
        {
            private readonly Mock<ILogger<MessageReceivedNotificationHandler>> _loggerMock;
            private readonly Mock<IConversationService> _conversationServiceMock;
            private readonly MessageReceivedNotificationHandler _handler;

            public MessageReceivedNotificationHandlerTests()
            {
                _loggerMock = new Mock<ILogger<MessageReceivedNotificationHandler>>();
                _conversationServiceMock = new Mock<IConversationService>();
                
                _handler = new MessageReceivedNotificationHandler(
                    _loggerMock.Object, 
                    _conversationServiceMock.Object);
            }

            [Fact]
            public async Task Handle_WhenMessageFromBot_IgnoresMessage()
            {
                // Arrange
                var socketUserMock = new Mock<IUser>();
                socketUserMock.Setup(u => u.IsBot).Returns(true);
                socketUserMock.Setup(u => u.Id).Returns(123456789UL);
                socketUserMock.Setup(u => u.Username).Returns("TestBot");
                
                var socketMessageMock = new Mock<IMessage>();
                socketMessageMock.Setup(m => m.Author).Returns(socketUserMock.Object);
                socketMessageMock.Setup(m => m.Content).Returns("Bot message");
                socketMessageMock.Setup(m => m.Id).Returns(987654321UL);
                socketMessageMock.Setup(m => m.Channel).Returns(Mock.Of<IMessageChannel>(c => c.Id == 555555UL));
                
                var notification = new MessageReceivedNotification(socketMessageMock.Object);
                
                // Act
                await _handler.Handle(notification, CancellationToken.None);
                
                // Assert
                _conversationServiceMock.Verify(
                    cs => cs.ProcessMessageAsync(It.IsAny<Models.Conversation.MessageEvent>()),
                    Times.Never);
            }

            [Fact]
            public async Task Handle_WhenMessageFromUser_ProcessesMessage()
            {
                // Arrange
                var socketUserMock = new Mock<IUser>();
                socketUserMock.Setup(u => u.IsBot).Returns(false);
                socketUserMock.Setup(u => u.Id).Returns(123456789UL);
                socketUserMock.Setup(u => u.Username).Returns("TestUser");
                
                var socketMessageMock = new Mock<IMessage>();
                socketMessageMock.Setup(m => m.Author).Returns(socketUserMock.Object);
                socketMessageMock.Setup(m => m.Content).Returns("User message");
                socketMessageMock.Setup(m => m.Id).Returns(987654321UL);
                
                var channelMock = new Mock<IMessageChannel>();
                channelMock.Setup(c => c.Id).Returns(555555UL);
                socketMessageMock.Setup(m => m.Channel).Returns(channelMock.Object);
                
                var referencedMessage = new Mock<IMessage>();
                socketMessageMock.Setup(m => m.ReferencedMessage).Returns(referencedMessage.Object);
                
                var notification = new MessageReceivedNotification(socketMessageMock.Object);
                
                // Act
                await _handler.Handle(notification, CancellationToken.None);
                
                // Assert
                _conversationServiceMock.Verify(
                    cs => cs.ProcessMessageAsync(It.Is<Models.Conversation.MessageEvent>(
                        me => me.AuthorId == "123456789" && 
                              me.ChannelId == "555555" && 
                              me.Content == "User message" && 
                              me.MessageId == "987654321")), 
                    Times.Once);
            }

            [Fact]
            public async Task Handle_WhenMessageIsDM_SetsDMFlag()
            {
                // Arrange
                var socketUserMock = new Mock<IUser>();
                socketUserMock.Setup(u => u.IsBot).Returns(false);
                socketUserMock.Setup(u => u.Id).Returns(123456789UL);
                
                var dmChannelMock = new Mock<IDMChannel>();
                dmChannelMock.Setup(c => c.Id).Returns(555555UL);
                
                var socketMessageMock = new Mock<IMessage>();
                socketMessageMock.Setup(m => m.Author).Returns(socketUserMock.Object);
                socketMessageMock.Setup(m => m.Content).Returns("DM message");
                socketMessageMock.Setup(m => m.Id).Returns(987654321UL);
                socketMessageMock.Setup(m => m.Channel).Returns(dmChannelMock.Object);
                
                var notification = new MessageReceivedNotification(socketMessageMock.Object);
                
                // Act
                await _handler.Handle(notification, CancellationToken.None);
                
                // Assert
                _conversationServiceMock.Verify(
                    cs => cs.ProcessMessageAsync(It.Is<Models.Conversation.MessageEvent>(
                        me => me.AuthorId == "123456789" && 
                              me.IsDM == true)), 
                    Times.Once);
            }

            [Fact]
            public async Task Handle_WhenMessageContainsMention_SetsMentionFlag()
            {
                // Arrange
                var socketUserMock = new Mock<IUser>();
                socketUserMock.Setup(u => u.IsBot).Returns(false);
                socketUserMock.Setup(u => u.Id).Returns(123456789UL);
                
                var socketMessageMock = new Mock<IMessage>();
                socketMessageMock.Setup(m => m.Author).Returns(socketUserMock.Object);
                socketMessageMock.Setup(m => m.Content).Returns("Hey <@999000> how are you?");
                socketMessageMock.Setup(m => m.Id).Returns(987654321UL);
                socketMessageMock.Setup(m => m.Channel).Returns(Mock.Of<IMessageChannel>(c => c.Id == 555555UL));
                socketMessageMock.Setup(m => m.MentionedUsers).Returns(new[] { Mock.Of<IUser>(u => u.Id == 999000UL) });
                
                var clientMock = new Mock<IDiscordClient>();
                clientMock.Setup(c => c.CurrentUser).Returns(Mock.Of<ISelfUser>(u => u.Id == 999000UL));
                
                var notification = new MessageReceivedNotification(socketMessageMock.Object, clientMock.Object);
                
                // Act
                await _handler.Handle(notification, CancellationToken.None);
                
                // Assert
                _conversationServiceMock.Verify(
                    cs => cs.ProcessMessageAsync(It.Is<Models.Conversation.MessageEvent>(
                        me => me.AuthorId == "123456789" && 
                              me.IsMention == true)), 
                    Times.Once);
            }
        }

        #endregion

        #region GuildAvailableNotificationHandler Tests

        public class GuildAvailableNotificationHandlerTests
        {
            private readonly Mock<ILogger<GuildAvailableNotificationHandler>> _loggerMock;
            private readonly GuildAvailableNotificationHandler _handler;

            public GuildAvailableNotificationHandlerTests()
            {
                _loggerMock = new Mock<ILogger<GuildAvailableNotificationHandler>>();
                _handler = new GuildAvailableNotificationHandler(_loggerMock.Object);
            }

            [Fact]
            public async Task Handle_WhenCalled_LogsGuildInfo()
            {
                // Arrange
                var guildMock = new Mock<IGuild>();
                guildMock.Setup(g => g.Id).Returns(123456789UL);
                guildMock.Setup(g => g.Name).Returns("Test Guild");
                
                var notification = new GuildAvailableNotification(guildMock.Object);
                
                // Act
                await _handler.Handle(notification, CancellationToken.None);
                
                // Assert
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => 
                            v.ToString().Contains("Guild available") && 
                            v.ToString().Contains("Test Guild") &&
                            v.ToString().Contains("123456789")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
            }
        }

        #endregion
    }
}
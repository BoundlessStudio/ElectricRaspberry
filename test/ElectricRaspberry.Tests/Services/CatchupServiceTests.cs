using Xunit;
using Moq;
using FluentAssertions;
using ElectricRaspberry.Services;
using ElectricRaspberry.Models.Conversation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ElectricRaspberry.Tests.Services
{
    public class CatchupServiceTests
    {
        private readonly Mock<ILogger<CatchupService>> _loggerMock;
        private readonly Mock<IConversationService> _conversationServiceMock;
        private readonly CatchupService _catchupService;

        public CatchupServiceTests()
        {
            _loggerMock = new Mock<ILogger<CatchupService>>();
            _conversationServiceMock = new Mock<IConversationService>();
            
            _catchupService = new CatchupService(
                _loggerMock.Object,
                _conversationServiceMock.Object);
        }

        #region Queue Management Tests

        [Fact]
        public async Task AddToQueue_WhenCalledWithMessage_AddsMessageToQueue()
        {
            // Arrange
            var messageEvent = new MessageEvent
            {
                AuthorId = "123456789",
                ChannelId = "987654321",
                Content = "Hello, world!",
                IsMention = false,
                IsDM = false,
                MessageId = "111222333",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _catchupService.AddToQueueAsync(messageEvent);

            // Assert
            var queuedMessages = await _catchupService.GetQueuedMessagesAsync(100);
            queuedMessages.Should().HaveCount(1);
            queuedMessages.First().MessageEvent.MessageId.Should().Be(messageEvent.MessageId);
        }

        [Fact]
        public async Task GetQueuedMessages_WhenQueueIsEmpty_ReturnsEmptyList()
        {
            // Arrange
            // Queue starts empty

            // Act
            var queuedMessages = await _catchupService.GetQueuedMessagesAsync(10);

            // Assert
            queuedMessages.Should().BeEmpty();
        }

        [Fact]
        public async Task GetQueuedMessages_WhenCountLimited_ReturnsLimitedMessages()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                var messageEvent = new MessageEvent
                {
                    AuthorId = $"author{i}",
                    ChannelId = "channel",
                    Content = $"Message {i}",
                    MessageId = $"msg{i}",
                    Timestamp = DateTime.UtcNow.AddMinutes(-i)
                };
                await _catchupService.AddToQueueAsync(messageEvent);
            }

            // Act
            var limitedMessages = await _catchupService.GetQueuedMessagesAsync(3);

            // Assert
            limitedMessages.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetQueuedMessages_WhenFilteredByChannel_ReturnsMessagesFromThatChannel()
        {
            // Arrange
            var channelId = "target-channel";
            
            // Add 3 messages to target channel
            for (int i = 0; i < 3; i++)
            {
                var messageEvent = new MessageEvent
                {
                    AuthorId = $"author{i}",
                    ChannelId = channelId,
                    Content = $"Target channel message {i}",
                    MessageId = $"target{i}",
                    Timestamp = DateTime.UtcNow.AddMinutes(-i)
                };
                await _catchupService.AddToQueueAsync(messageEvent);
            }
            
            // Add 2 messages to other channels
            for (int i = 0; i < 2; i++)
            {
                var messageEvent = new MessageEvent
                {
                    AuthorId = $"author{i}",
                    ChannelId = $"other-channel{i}",
                    Content = $"Other channel message {i}",
                    MessageId = $"other{i}",
                    Timestamp = DateTime.UtcNow.AddMinutes(-i)
                };
                await _catchupService.AddToQueueAsync(messageEvent);
            }

            // Act
            var filteredMessages = await _catchupService.GetQueuedMessagesAsync(10, channelId);

            // Assert
            filteredMessages.Should().HaveCount(3);
            filteredMessages.All(m => m.MessageEvent.ChannelId == channelId).Should().BeTrue();
        }

        [Fact]
        public async Task MarkAsProcessed_WhenCalledWithMessageId_SetsIsProcessedToTrue()
        {
            // Arrange
            var messageEvent = new MessageEvent
            {
                AuthorId = "123456789",
                ChannelId = "987654321",
                Content = "Hello, world!",
                MessageId = "msg123",
                Timestamp = DateTime.UtcNow
            };
            await _catchupService.AddToQueueAsync(messageEvent);

            // Act
            await _catchupService.MarkAsProcessedAsync("msg123");

            // Assert
            var queuedMessages = await _catchupService.GetQueuedMessagesAsync(100);
            queuedMessages.First().IsProcessed.Should().BeTrue();
        }

        #endregion

        #region Priority Tests

        [Fact]
        public async Task AddToQueue_WhenMessageIsDM_AssignsHighPriority()
        {
            // Arrange
            var messageEvent = new MessageEvent
            {
                AuthorId = "123456789",
                ChannelId = "987654321",
                Content = "Hello, world!",
                IsDM = true,
                MessageId = "dm123",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _catchupService.AddToQueueAsync(messageEvent);

            // Assert
            var queuedMessages = await _catchupService.GetQueuedMessagesAsync(100);
            queuedMessages.First().Priority.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task AddToQueue_WhenMessageIsMention_AssignsHighPriority()
        {
            // Arrange
            var messageEvent = new MessageEvent
            {
                AuthorId = "123456789",
                ChannelId = "987654321",
                Content = "Hello, @bot!",
                IsMention = true,
                MessageId = "mention123",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _catchupService.AddToQueueAsync(messageEvent);

            // Assert
            var queuedMessages = await _catchupService.GetQueuedMessagesAsync(100);
            queuedMessages.First().Priority.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetQueuedMessages_WhenMultipleMessagesWithDifferentPriorities_ReturnsSortedByPriorityAndTimestamp()
        {
            // Arrange
            var now = DateTime.UtcNow;
            
            // Regular message (lowest priority)
            var regularMessage = new MessageEvent
            {
                AuthorId = "author1",
                ChannelId = "channel1",
                Content = "Regular message",
                IsDM = false,
                IsMention = false,
                MessageId = "regular",
                Timestamp = now.AddMinutes(-10)
            };
            
            // Mention message (medium priority)
            var mentionMessage = new MessageEvent
            {
                AuthorId = "author2",
                ChannelId = "channel1",
                Content = "@bot mentioned",
                IsDM = false,
                IsMention = true,
                MessageId = "mention",
                Timestamp = now.AddMinutes(-5)
            };
            
            // DM message (highest priority)
            var dmMessage = new MessageEvent
            {
                AuthorId = "author3",
                ChannelId = "dm-channel",
                Content = "Direct message",
                IsDM = true,
                IsMention = false,
                MessageId = "dm",
                Timestamp = now
            };
            
            // Add messages in reverse order of expected priority
            await _catchupService.AddToQueueAsync(regularMessage);
            await _catchupService.AddToQueueAsync(mentionMessage);
            await _catchupService.AddToQueueAsync(dmMessage);

            // Act
            var queuedMessages = await _catchupService.GetQueuedMessagesAsync(10);

            // Assert
            queuedMessages.Should().HaveCount(3);
            queuedMessages[0].MessageEvent.MessageId.Should().Be("dm");
            queuedMessages[1].MessageEvent.MessageId.Should().Be("mention");
            queuedMessages[2].MessageEvent.MessageId.Should().Be("regular");
        }

        #endregion

        #region Processing Tests

        [Fact]
        public async Task ProcessNextAsync_WhenQueueHasMessages_ProcessesHighestPriorityMessage()
        {
            // Arrange
            var dmMessage = new MessageEvent
            {
                AuthorId = "author1",
                ChannelId = "dm-channel",
                Content = "Direct message",
                IsDM = true,
                MessageId = "dm123",
                Timestamp = DateTime.UtcNow
            };
            
            var regularMessage = new MessageEvent
            {
                AuthorId = "author2",
                ChannelId = "channel1",
                Content = "Regular message",
                IsDM = false,
                MessageId = "reg123",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            };
            
            await _catchupService.AddToQueueAsync(regularMessage);
            await _catchupService.AddToQueueAsync(dmMessage);
            
            _conversationServiceMock
                .Setup(cs => cs.ProcessMessageAsync(It.IsAny<MessageEvent>()))
                .Returns(Task.CompletedTask);

            // Act
            var processed = await _catchupService.ProcessNextAsync();

            // Assert
            processed.Should().BeTrue();
            _conversationServiceMock.Verify(
                cs => cs.ProcessMessageAsync(It.Is<MessageEvent>(m => m.MessageId == "dm123")),
                Times.Once);
        }

        [Fact]
        public async Task ProcessNextAsync_WhenQueueIsEmpty_ReturnsFalse()
        {
            // Arrange
            // Empty queue

            // Act
            var processed = await _catchupService.ProcessNextAsync();

            // Assert
            processed.Should().BeFalse();
            _conversationServiceMock.Verify(
                cs => cs.ProcessMessageAsync(It.IsAny<MessageEvent>()),
                Times.Never);
        }

        [Fact]
        public async Task GetActivitySummaryAsync_WhenMessagesExist_ReturnsSummaryText()
        {
            // Arrange
            // Add messages from 3 different channels and 2 different authors
            var messages = new List<MessageEvent>
            {
                new MessageEvent { AuthorId = "author1", ChannelId = "channel1", Content = "Msg 1", MessageId = "m1", Timestamp = DateTime.UtcNow.AddMinutes(-30) },
                new MessageEvent { AuthorId = "author1", ChannelId = "channel1", Content = "Msg 2", MessageId = "m2", Timestamp = DateTime.UtcNow.AddMinutes(-25) },
                new MessageEvent { AuthorId = "author2", ChannelId = "channel1", Content = "Msg 3", MessageId = "m3", Timestamp = DateTime.UtcNow.AddMinutes(-20) },
                new MessageEvent { AuthorId = "author1", ChannelId = "channel2", Content = "Msg 4", MessageId = "m4", Timestamp = DateTime.UtcNow.AddMinutes(-15) },
                new MessageEvent { AuthorId = "author2", ChannelId = "channel3", Content = "Msg 5", MessageId = "m5", Timestamp = DateTime.UtcNow.AddMinutes(-10) },
                new MessageEvent { AuthorId = "author2", ChannelId = "channel3", Content = "Msg 6", MessageId = "m6", Timestamp = DateTime.UtcNow.AddMinutes(-5) }
            };
            
            foreach (var msg in messages)
            {
                await _catchupService.AddToQueueAsync(msg);
            }

            // Act
            var summary = await _catchupService.GetActivitySummaryAsync();

            // Assert
            summary.Should().NotBeNullOrEmpty();
            summary.Should().Contain("channel1");
            summary.Should().Contain("channel2");
            summary.Should().Contain("channel3");
            summary.Should().Contain("author1");
            summary.Should().Contain("author2");
            summary.Should().Contain("6"); // Total message count
        }

        [Fact]
        public async Task ProcessAllAsync_WhenCalled_ProcessesAllMessages()
        {
            // Arrange
            var messages = new List<MessageEvent>
            {
                new MessageEvent { AuthorId = "a1", ChannelId = "c1", Content = "M1", MessageId = "m1", Timestamp = DateTime.UtcNow.AddMinutes(-15) },
                new MessageEvent { AuthorId = "a2", ChannelId = "c2", Content = "M2", MessageId = "m2", Timestamp = DateTime.UtcNow.AddMinutes(-10) },
                new MessageEvent { AuthorId = "a3", ChannelId = "c3", Content = "M3", MessageId = "m3", Timestamp = DateTime.UtcNow.AddMinutes(-5) }
            };
            
            foreach (var msg in messages)
            {
                await _catchupService.AddToQueueAsync(msg);
            }
            
            _conversationServiceMock
                .Setup(cs => cs.ProcessMessageAsync(It.IsAny<MessageEvent>()))
                .Returns(Task.CompletedTask);

            // Act
            await _catchupService.ProcessAllAsync();

            // Assert
            _conversationServiceMock.Verify(
                cs => cs.ProcessMessageAsync(It.IsAny<MessageEvent>()),
                Times.Exactly(3));
            
            var queuedMessages = await _catchupService.GetQueuedMessagesAsync(100);
            queuedMessages.All(m => m.IsProcessed).Should().BeTrue();
        }

        #endregion

        #region Queue Management Tests

        [Fact]
        public async Task AddToQueue_WhenQueueExceedsLimit_TrimsOldestMessages()
        {
            // Arrange
            // Set a small initial queue capacity for testing
            var originalQueueCapacity = typeof(CatchupService).GetField("_maxQueueCapacity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_catchupService);
            
            // Use reflection to set a small capacity for testing
            typeof(CatchupService).GetField("_maxQueueCapacity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_catchupService, 5);
            
            try
            {
                // Add more than capacity messages
                for (int i = 0; i < 10; i++)
                {
                    var messageEvent = new MessageEvent
                    {
                        AuthorId = $"author{i}",
                        ChannelId = "channel1",
                        Content = $"Message {i}",
                        MessageId = $"msg{i}",
                        Timestamp = DateTime.UtcNow.AddMinutes(-i) // Older messages have earlier timestamps
                    };
                    await _catchupService.AddToQueueAsync(messageEvent);
                }

                // Act
                var queuedMessages = await _catchupService.GetQueuedMessagesAsync(100);

                // Assert
                queuedMessages.Should().HaveCount(5); // Should be trimmed to capacity
                
                // Should keep the 5 newest messages (lower i values have newer timestamps)
                queuedMessages.All(m => int.Parse(m.MessageEvent.MessageId.Replace("msg", "")) < 5).Should().BeTrue();
            }
            finally
            {
                // Restore original capacity
                typeof(CatchupService).GetField("_maxQueueCapacity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_catchupService, originalQueueCapacity);
            }
        }

        [Fact]
        public async Task ClearQueueAsync_WhenCalled_RemovesAllMessages()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                var messageEvent = new MessageEvent
                {
                    AuthorId = $"author{i}",
                    ChannelId = "channel1",
                    Content = $"Message {i}",
                    MessageId = $"msg{i}",
                    Timestamp = DateTime.UtcNow.AddMinutes(-i)
                };
                await _catchupService.AddToQueueAsync(messageEvent);
            }
            
            // Verify messages were added
            var initialCount = (await _catchupService.GetQueuedMessagesAsync(100)).Count;
            initialCount.Should().Be(5);

            // Act
            await _catchupService.ClearQueueAsync();

            // Assert
            var finalCount = (await _catchupService.GetQueuedMessagesAsync(100)).Count;
            finalCount.Should().Be(0);
        }

        #endregion
    }
}
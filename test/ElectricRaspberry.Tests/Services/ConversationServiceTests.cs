using Discord;
using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElectricRaspberry.Tests.Services
{
    public class ConversationServiceTests
    {
        private readonly Mock<ILogger<ConversationService>> _loggerMock;
        private readonly ConversationService _conversationService;

        public ConversationServiceTests()
        {
            _loggerMock = new Mock<ILogger<ConversationService>>();
            _conversationService = new ConversationService(_loggerMock.Object);
        }

        #region Message Processing Tests

        [Fact]
        public async Task ProcessMessage_ShouldCreateNewConversation_WhenFirstMessage()
        {
            // Arrange
            var messageEvent = CreateMessageEvent("Hello world", isDirectMessage: false);
            
            // Act
            var conversation = await _conversationService.ProcessMessageAsync(messageEvent);
            
            // Assert
            conversation.Should().NotBeNull();
            conversation.Id.Should().NotBeNullOrEmpty();
            conversation.Messages.Should().HaveCount(1);
            conversation.Messages[0].Content.Should().Be("Hello world");
            conversation.State.Should().Be(ConversationState.Active);
            
            // Verify the conversation was added to the service's internal collection
            var activeConversations = await _conversationService.GetActiveConversationsAsync();
            activeConversations.Should().HaveCount(1);
            activeConversations.First().Id.Should().Be(conversation.Id);
        }

        [Fact]
        public async Task ProcessMessage_ShouldAddToExistingConversation_WhenChannelMatches()
        {
            // Arrange - Process first message to create conversation
            var firstMessageEvent = CreateMessageEvent("First message", isDirectMessage: false);
            var conversation = await _conversationService.ProcessMessageAsync(firstMessageEvent);
            
            // Create a second message for the same channel
            var secondMessageEvent = CreateMessageEvent("Second message", isDirectMessage: false);
            secondMessageEvent.Channel = firstMessageEvent.Channel;
            
            // Act
            var updatedConversation = await _conversationService.ProcessMessageAsync(secondMessageEvent);
            
            // Assert
            updatedConversation.Should().NotBeNull();
            updatedConversation.Id.Should().Be(conversation.Id); // Same conversation
            updatedConversation.Messages.Should().HaveCount(2);
            updatedConversation.Messages[1].Content.Should().Be("Second message");
        }

        [Fact]
        public async Task ProcessMessage_ShouldCreateNewConversation_WhenDirectMessage()
        {
            // Arrange
            var dmMessageEvent = CreateMessageEvent("Hello in DM", isDirectMessage: true);
            
            // Act
            var conversation = await _conversationService.ProcessMessageAsync(dmMessageEvent);
            
            // Assert
            conversation.Should().NotBeNull();
            conversation.IsDirectMessage.Should().BeTrue();
            conversation.Messages.Should().HaveCount(1);
            conversation.Messages[0].Content.Should().Be("Hello in DM");
        }

        #endregion

        #region Conversation Retrieval Tests

        [Fact]
        public async Task GetConversation_ShouldReturnNull_WhenIdDoesNotExist()
        {
            // Act
            var result = await _conversationService.GetConversationAsync("non-existent-id");
            
            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetConversation_ShouldReturnConversation_WhenIdExists()
        {
            // Arrange - Create a conversation
            var messageEvent = CreateMessageEvent("Test message");
            var conversation = await _conversationService.ProcessMessageAsync(messageEvent);
            
            // Act
            var retrievedConversation = await _conversationService.GetConversationAsync(conversation.Id);
            
            // Assert
            retrievedConversation.Should().NotBeNull();
            retrievedConversation.Id.Should().Be(conversation.Id);
        }

        [Fact]
        public async Task GetActiveConversations_ShouldReturnAllActiveConversations()
        {
            // Arrange - Create multiple conversations
            await _conversationService.ProcessMessageAsync(CreateMessageEvent("Message 1", channelId: 1));
            await _conversationService.ProcessMessageAsync(CreateMessageEvent("Message 2", channelId: 2));
            await _conversationService.ProcessMessageAsync(CreateMessageEvent("Message 3", channelId: 3));
            
            // Act
            var activeConversations = await _conversationService.GetActiveConversationsAsync();
            
            // Assert
            activeConversations.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetChannelConversations_ShouldReturnConversationsForChannel()
        {
            // Arrange - Create conversations in different channels
            await _conversationService.ProcessMessageAsync(CreateMessageEvent("Channel 1 Msg", channelId: 1));
            await _conversationService.ProcessMessageAsync(CreateMessageEvent("Channel 2 Msg", channelId: 2));
            await _conversationService.ProcessMessageAsync(CreateMessageEvent("Channel 1 Msg 2", channelId: 1));
            
            // Act
            var channel1Conversations = await _conversationService.GetChannelConversationsAsync(1);
            var channel2Conversations = await _conversationService.GetChannelConversationsAsync(2);
            var channel3Conversations = await _conversationService.GetChannelConversationsAsync(3);
            
            // Assert
            channel1Conversations.Should().HaveCount(1); // Only 1 conversation per channel
            channel2Conversations.Should().HaveCount(1);
            channel3Conversations.Should().BeEmpty();
        }

        #endregion

        #region Conversation Management Tests

        [Fact]
        public async Task CompleteConversation_ShouldUpdateState_WhenIdExists()
        {
            // Arrange - Create a conversation
            var messageEvent = CreateMessageEvent("Test message");
            var conversation = await _conversationService.ProcessMessageAsync(messageEvent);
            
            // Act
            var result = await _conversationService.CompleteConversationAsync(conversation.Id);
            
            // Assert
            result.Should().BeTrue();
            
            // Verify the conversation state was updated
            var updatedConversation = await _conversationService.GetConversationAsync(conversation.Id);
            updatedConversation.State.Should().Be(ConversationState.Completed);
            
            // It should no longer be in active conversations
            var activeConversations = await _conversationService.GetActiveConversationsAsync();
            activeConversations.Should().BeEmpty();
        }

        [Fact]
        public async Task CompleteConversation_ShouldReturnFalse_WhenIdDoesNotExist()
        {
            // Act
            var result = await _conversationService.CompleteConversationAsync("non-existent-id");
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateConversationTopic_ShouldUpdateTopic_WhenIdExists()
        {
            // Arrange - Create a conversation
            var messageEvent = CreateMessageEvent("Test message");
            var conversation = await _conversationService.ProcessMessageAsync(messageEvent);
            
            // Act
            var result = await _conversationService.UpdateConversationTopicAsync(conversation.Id, "New Topic");
            
            // Assert
            result.Should().BeTrue();
            
            // Verify the topic was updated
            var updatedConversation = await _conversationService.GetConversationAsync(conversation.Id);
            updatedConversation.Topic.Should().Be("New Topic");
        }

        [Fact]
        public async Task SetConversationImportance_ShouldUpdateImportance_WhenIdExists()
        {
            // Arrange - Create a conversation
            var messageEvent = CreateMessageEvent("Test message");
            var conversation = await _conversationService.ProcessMessageAsync(messageEvent);
            
            // Act
            var result = await _conversationService.SetConversationImportanceAsync(conversation.Id, 0.9);
            
            // Assert
            result.Should().BeTrue();
            
            // Verify the importance was updated and state changed to Urgent
            var updatedConversation = await _conversationService.GetConversationAsync(conversation.Id);
            updatedConversation.Importance.Should().Be(0.9);
            updatedConversation.State.Should().Be(ConversationState.Urgent); // High importance causes Urgent state
        }

        [Fact]
        public async Task SetConversationImportance_ShouldClampValue_BetweenZeroAndOne()
        {
            // Arrange - Create a conversation
            var messageEvent = CreateMessageEvent("Test message");
            var conversation = await _conversationService.ProcessMessageAsync(messageEvent);
            
            // Act - Try to set importance outside valid range
            await _conversationService.SetConversationImportanceAsync(conversation.Id, 1.5);
            
            // Assert
            var updatedConversation = await _conversationService.GetConversationAsync(conversation.Id);
            updatedConversation.Importance.Should().Be(1.0); // Should be clamped to 1.0
        }

        #endregion

        #region Conversation Context Tests

        [Fact]
        public async Task GetRecentMessages_ShouldReturnCorrectMessages()
        {
            // Arrange - Create a conversation with multiple messages
            var firstMessage = CreateMessageEvent("First message");
            var conversation = await _conversationService.ProcessMessageAsync(firstMessage);
            
            // Add more messages
            for (int i = 2; i <= 12; i++)
            {
                var messageEvent = CreateMessageEvent($"Message {i}");
                messageEvent.Channel = firstMessage.Channel;
                await _conversationService.ProcessMessageAsync(messageEvent);
            }
            
            // Act - Get recent messages with limit
            var recentMessages = await _conversationService.GetRecentMessagesAsync(conversation.Id, 5);
            
            // Assert
            recentMessages.Should().HaveCount(5);
            recentMessages.Last().Content.Should().Be("Message 12"); // Most recent should be last
        }

        [Fact]
        public async Task CreateConversationContext_ShouldIncludeMetadataAndMessages()
        {
            // Arrange - Create a conversation with topic
            var messageEvent = CreateMessageEvent("Hello there");
            var conversation = await _conversationService.ProcessMessageAsync(messageEvent);
            await _conversationService.UpdateConversationTopicAsync(conversation.Id, "Test Topic");
            
            // Add another message
            var secondMessageEvent = CreateMessageEvent("General Kenobi");
            secondMessageEvent.Channel = messageEvent.Channel;
            await _conversationService.ProcessMessageAsync(secondMessageEvent);
            
            // Act
            var context = await _conversationService.CreateConversationContextAsync(conversation.Id);
            
            // Assert
            context.Should().Contain("Conversation in");
            context.Should().Contain("Topic: Test Topic");
            context.Should().Contain("Hello there");
            context.Should().Contain("General Kenobi");
        }

        [Fact]
        public async Task CreateConversationContext_ShouldReturnNotFoundMessage_ForInvalidId()
        {
            // Act
            var context = await _conversationService.CreateConversationContextAsync("non-existent-id");
            
            // Assert
            context.Should().Contain("No conversation found");
        }

        #endregion

        #region Maintenance Tests

        [Fact]
        public async Task PerformMaintenance_ShouldUpdateIdleConversationStates()
        {
            // Arrange - Create a conversation
            var messageEvent = CreateMessageEvent("Test message");
            var conversation = await _conversationService.ProcessMessageAsync(messageEvent);
            
            // Simulate conversation being idle by accessing it with reflection
            // to change LastActiveAt timestamp
            var fieldInfo = typeof(Conversation).GetField("LastActiveAt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            fieldInfo.SetValue(conversation, DateTime.UtcNow.AddMinutes(-20)); // 20 minutes ago
            
            // Act
            await _conversationService.PerformMaintenanceAsync();
            
            // Assert
            var updatedConversation = await _conversationService.GetConversationAsync(conversation.Id);
            updatedConversation.State.Should().Be(ConversationState.Idle);
        }

        #endregion

        #region Helper Methods

        private MessageEvent CreateMessageEvent(string content, bool isDirectMessage = false, ulong channelId = 1234, bool isFromBot = false)
        {
            // Create mocks for the necessary Discord objects
            var authorMock = new Mock<IUser>();
            authorMock.Setup(u => u.Id).Returns(9999);
            authorMock.Setup(u => u.Username).Returns("TestUser");
            authorMock.Setup(u => u.IsBot).Returns(isFromBot);
            
            var messageMock = new Mock<IMessage>();
            messageMock.Setup(m => m.Id).Returns(new Random().NextInt64());
            messageMock.Setup(m => m.Content).Returns(content);
            messageMock.Setup(m => m.Author).Returns(authorMock.Object);
            messageMock.Setup(m => m.Timestamp).Returns(DateTimeOffset.UtcNow);
            
            var channelMock = isDirectMessage ? 
                new Mock<IPrivateChannel>() : 
                new Mock<ITextChannel>();
            
            channelMock.Setup(c => c.Id).Returns(channelId);
            
            if (!isDirectMessage)
            {
                var textChannelMock = channelMock as Mock<ITextChannel>;
                textChannelMock.Setup(c => c.Name).Returns($"test-channel-{channelId}");
                
                var guildMock = new Mock<IGuild>();
                guildMock.Setup(g => g.Name).Returns("Test Guild");
                textChannelMock.Setup(c => c.Guild).Returns(guildMock.Object);
            }
            
            messageMock.Setup(m => m.Channel).Returns(channelMock.Object);
            
            var messageEvent = new MessageEvent(messageMock.Object, DateTimeOffset.UtcNow);
            return messageEvent;
        }

        #endregion
    }
}
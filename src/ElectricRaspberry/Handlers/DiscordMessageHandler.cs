using Discord;
using Discord.WebSocket;
using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Notifications;
using ElectricRaspberry.Services.Observation;
using MediatR;

namespace ElectricRaspberry.Handlers;

/// <summary>
/// Handles Discord message events and routes them to the observation system
/// </summary>
public class DiscordMessageHandler : INotificationHandler<MessageReceivedNotification>
{
    private readonly IObserverService _observerService;
    private readonly ILogger<DiscordMessageHandler> _logger;
    
    /// <summary>
    /// Creates a new instance of the Discord message handler
    /// </summary>
    /// <param name="observerService">Observer service for processing messages</param>
    /// <param name="logger">Logger</param>
    public DiscordMessageHandler(
        IObserverService observerService,
        ILogger<DiscordMessageHandler> logger)
    {
        _observerService = observerService;
        _logger = logger;
    }
    
    /// <summary>
    /// Handles a message received notification from Discord
    /// </summary>
    /// <param name="notification">The notification containing the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var message = notification.Message;
        
        // Ignore messages from the bot itself
        if (message.Author.Id == notification.BotUserId)
        {
            return;
        }
        
        try
        {
            // Convert to our internal message event model
            var messageEvent = ConvertToMessageEvent(message);
            var channelId = message.Channel.Id.ToString();
            
            // Forward to observer service for processing
            await _observerService.ProcessMessageEventAsync(messageEvent, channelId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Discord message {MessageId} in channel {ChannelId}", 
                message.Id, message.Channel.Id);
        }
    }
    
    private MessageEvent ConvertToMessageEvent(SocketMessage message)
    {
        // Extract all mentioned user IDs
        var mentionedUserIds = message.MentionedUsers
            .Select(u => u.Id.ToString())
            .ToList();
        
        // Create the message event
        return new MessageEvent(
            messageId: message.Id.ToString(),
            authorId: message.Author.Id.ToString(),
            authorName: message.Author.Username,
            content: message.Content,
            timestamp: message.Timestamp,
            isDirectMessage: message.Channel is SocketDMChannel,
            isMention: mentionedUserIds.Any(),
            mentionedUserIds: mentionedUserIds,
            attachments: message.Attachments.Select(a => a.Url).ToList()
        );
    }
}
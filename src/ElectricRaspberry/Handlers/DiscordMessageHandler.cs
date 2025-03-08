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
    private readonly DiscordSocketClient _discordClient;
    
    /// <summary>
    /// Creates a new instance of the Discord message handler
    /// </summary>
    /// <param name="observerService">Observer service for processing messages</param>
    /// <param name="logger">Logger</param>
    /// <param name="discordClient">Discord client</param>
    public DiscordMessageHandler(
        IObserverService observerService,
        ILogger<DiscordMessageHandler> logger,
        DiscordSocketClient discordClient)
    {
        _observerService = observerService;
        _logger = logger;
        _discordClient = discordClient;
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
        if (message.Author.Id == _discordClient.CurrentUser.Id)
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
        // Create the message event using the constructor that takes IMessage and timestamp
        var messageEvent = new MessageEvent(message, message.Timestamp);
        
        // Set mentions flag if the bot is mentioned
        if (_discordClient.CurrentUser != null && message.MentionedUsers.Any(u => u.Id == _discordClient.CurrentUser.Id))
        {
            messageEvent.MentionsBot = true;
        }
        
        return messageEvent;
    }
}
using Discord;
using Discord.WebSocket;
using ElectricRaspberry.Configuration;
using ElectricRaspberry.Notifications;
using MediatR;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Services;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly IMediator _mediator;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly DiscordOptions _options;

    public DiscordBotService(
        IMediator mediator,
        ILogger<DiscordBotService> logger,
        IOptions<DiscordOptions> options)
    {
        _mediator = mediator;
        _logger = logger;
        _options = options.Value;

        // Configure Discord client
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100
        };

        _client = new DiscordSocketClient(config);

        // Register event handlers
        RegisterEventHandlers();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _client.LoginAsync(TokenType.Bot, _options.Token);
            await _client.StartAsync();

            // Keep the service running until cancelled
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Normal shutdown, do nothing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Discord bot service");
        }
        finally
        {
            await _client.StopAsync();
        }
    }

    private void RegisterEventHandlers()
    {
        #region Gateway Events
        _client.Ready += () => PublishEvent(new ReadyNotification());
        _client.Connected += () => PublishEvent(new ConnectedNotification());
        _client.Disconnected += (ex) => PublishEvent(new DisconnectedNotification(ex));
        _client.Log += (logMessage) => PublishEvent(new LogNotification(
            logMessage.Severity, 
            logMessage.Source, 
            logMessage.Message, 
            logMessage.Exception));
        #endregion

        #region Guild Events
        _client.GuildAvailable += (guild) => PublishEvent(new GuildAvailableNotification(guild));
        _client.GuildUnavailable += (guild) => PublishEvent(new GuildUnavailableNotification(guild));
        _client.GuildUpdated += (oldGuild, newGuild) => PublishEvent(new GuildUpdatedNotification(oldGuild, newGuild));
        _client.GuildMemberUpdated += (oldUser, newUser) => PublishEvent(new GuildMemberUpdatedNotification(oldUser, newUser));
        _client.UserJoined += (user) => PublishEvent(new UserJoinedNotification(user));
        _client.UserLeft += (guild, user) => PublishEvent(new UserLeftNotification(guild, user));
        #endregion

        #region Message Events
        _client.MessageReceived += (message) => PublishEvent(new MessageReceivedNotification(message));
        _client.MessageUpdated += (oldMessage, newMessage, channel) => 
            PublishEvent(new MessageUpdatedNotification(oldMessage, newMessage, channel));
        _client.MessageDeleted += (message, channel) => 
            PublishEvent(new MessageDeletedNotification(message, channel));
        _client.ReactionAdded += (message, channel, reaction) => 
            PublishEvent(new ReactionAddedNotification(message, channel, reaction));
        _client.ReactionRemoved += (message, channel, reaction) => 
            PublishEvent(new ReactionRemovedNotification(message, channel, reaction));
        _client.ReactionsCleared += (message, channel) => 
            PublishEvent(new ReactionsAllClearedNotification(message, channel));
        _client.ReactionsRemovedForEmote += (message, channel, emote) => 
            PublishEvent(new ReactionsRemovedForEmoteNotification(message, channel, emote));
        #endregion

        #region Channel Events
        _client.ChannelCreated += (channel) => PublishEvent(new ChannelCreatedNotification(channel));
        _client.ChannelUpdated += (oldChannel, newChannel) => 
            PublishEvent(new ChannelUpdatedNotification(oldChannel, newChannel));
        _client.ChannelDestroyed += (channel) => PublishEvent(new ChannelDestroyedNotification(channel));
        #endregion

        #region Role Events
        _client.RoleCreated += (role) => PublishEvent(new RoleCreatedNotification(role));
        _client.RoleUpdated += (oldRole, newRole) => PublishEvent(new RoleUpdatedNotification(oldRole, newRole));
        _client.RoleDeleted += (role) => PublishEvent(new RoleDeletedNotification(role));
        #endregion

        #region User/Presence Events
        _client.UserUpdated += (oldUser, newUser) => PublishEvent(new UserUpdatedNotification(oldUser, newUser));
        _client.CurrentUserUpdated += (oldUser, newUser) => 
            PublishEvent(new CurrentUserUpdatedNotification(oldUser, newUser));
        _client.UserBanned += (user, guild) => PublishEvent(new UserBannedNotification(user, guild));
        _client.UserUnbanned += (user, guild) => PublishEvent(new UserUnbannedNotification(user, guild));
        _client.UserVoiceStateUpdated += (user, oldState, newState) => 
            PublishEvent(new UserVoiceStateUpdatedNotification(user, oldState, newState));
        _client.UserVoiceServerUpdated += (user, server) => 
            PublishEvent(new UserVoiceServerUpdatedNotification(user, server));
        _client.PresenceUpdated += (user, oldPresence, newPresence) => 
            PublishEvent(new PresenceUpdatedNotification(user, oldPresence, newPresence));
        #endregion

        #region Other Events
        _client.TypingStarted += (user, channel) => PublishEvent(new TypingStartedNotification(user, channel));
        _client.LatencyUpdated += (oldLatency, newLatency) => 
            PublishEvent(new LatencyUpdatedNotification(oldLatency, newLatency));
        
        _client.ButtonExecuted += (component) => PublishEvent(new ButtonExecutedNotification(component));
        _client.SelectMenuExecuted += (component) => PublishEvent(new SelectMenuExecutedNotification(component));
        _client.ModalSubmitted += (modal) => PublishEvent(new ModalSubmittedNotification(modal));
        
        _client.SlashCommandExecuted += (command) => PublishEvent(new SlashCommandExecutedNotification(command));
        _client.UserCommandExecuted += (command) => PublishEvent(new UserCommandExecutedNotification(command));
        _client.MessageCommandExecuted += (command) => PublishEvent(new MessageCommandExecutedNotification(command));
        
        _client.AutoModerationRuleCreated += (rule) => 
            PublishEvent(new AutoModerationRuleCreatedNotification(rule));
        _client.AutoModerationRuleUpdated += (oldRule, newRule) => 
            PublishEvent(new AutoModerationRuleUpdatedNotification(oldRule, newRule));
        _client.AutoModerationRuleDeleted += (rule) => 
            PublishEvent(new AutoModerationRuleDeletedNotification(rule));
        _client.AutoModerationActionExecuted += (data) => 
            PublishEvent(new AutoModerationActionExecutedNotification(data));
        #endregion
    }

    // Helper method to publish events through MediatR
    private Task PublishEvent<T>(T notification) where T : INotification
    {
        return _mediator.Publish(notification);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}
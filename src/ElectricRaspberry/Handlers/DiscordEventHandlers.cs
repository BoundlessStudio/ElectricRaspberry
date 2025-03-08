using Discord;
using ElectricRaspberry.Notifications;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ElectricRaspberry.Handlers;

// Sample handler for Discord's Ready event
public class ReadyNotificationHandler : INotificationHandler<ReadyNotification>
{
    private readonly ILogger<ReadyNotificationHandler> _logger;

    public ReadyNotificationHandler(ILogger<ReadyNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord bot is ready and connected at {Timestamp}", notification.Timestamp);
        return Task.CompletedTask;
    }
}

// Sample handler for Discord's Log event
public class LogNotificationHandler : INotificationHandler<LogNotification>
{
    private readonly ILogger<LogNotificationHandler> _logger;

    public LogNotificationHandler(ILogger<LogNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(LogNotification notification, CancellationToken cancellationToken)
    {
        var logLevel = notification.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(
            logLevel,
            notification.Exception,
            "Discord [{Source}]: {Message}",
            notification.Source,
            notification.Message);

        return Task.CompletedTask;
    }
}

// Sample handler for Discord's Message Received event
public class MessageReceivedNotificationHandler : INotificationHandler<MessageReceivedNotification>
{
    private readonly ILogger<MessageReceivedNotificationHandler> _logger;

    public MessageReceivedNotificationHandler(ILogger<MessageReceivedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        // Ignore messages from bots
        if (notification.Message.Author.IsBot)
            return Task.CompletedTask;

        _logger.LogInformation(
            "Received message from {Author} in {Channel}: {Content}",
            notification.Message.Author.Username,
            notification.Message.Channel.Name,
            notification.Message.Content);

        return Task.CompletedTask;
    }
}

// Sample handler for Guild Available event
public class GuildAvailableNotificationHandler : INotificationHandler<GuildAvailableNotification>
{
    private readonly ILogger<GuildAvailableNotificationHandler> _logger;

    public GuildAvailableNotificationHandler(ILogger<GuildAvailableNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(GuildAvailableNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Guild {GuildName} (ID: {GuildId}) is now available with {MemberCount} members",
            notification.Guild.Name,
            notification.Guild.Id,
            notification.Guild.MemberCount);

        return Task.CompletedTask;
    }
}
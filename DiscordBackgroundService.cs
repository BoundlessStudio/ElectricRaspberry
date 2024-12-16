using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public abstract class DiscordBackgroundService : BackgroundService
{
    protected readonly BotOptions options;
    protected readonly ILogger logger;
    protected DiscordSocketClient client;
    
    // TODO: Add handler to the events we want to capture
    // TODO: setup ai to observe the events and respond

    public DiscordBackgroundService(ILogger logger, BotOptions? options)
    {
        this.logger = logger ?? throw new Exception("ILogger is not set");
        this.options = options ?? throw new Exception("BotOptions is not set");
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All,
        };
        this.client = new DiscordSocketClient(config);
        this.client.Log += LogAsync;
    }


    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.client.LoginAsync(TokenType.Bot, this.options.Token);
        await this.client.StartAsync();

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await this.client.StopAsync();
        await this.client.DisposeAsync();

        await base.StopAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(-1, stoppingToken);
    }

    private Task LogAsync(LogMessage logMessage)
    {
        logger.LogInformation(logMessage.ToString());
        return Task.CompletedTask;
    }
}
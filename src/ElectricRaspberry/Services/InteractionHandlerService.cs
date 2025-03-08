using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace ElectricRaspberry.Services;

/// <summary>
/// Background service for handling Discord interactions such as slash commands
/// </summary>
public class InteractionHandlerService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InteractionHandlerService> _logger;
    
    /// <summary>
    /// Creates a new instance of the interaction handler service
    /// </summary>
    /// <param name="client">Discord client</param>
    /// <param name="interactionService">Interaction service</param>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="logger">Logger</param>
    public InteractionHandlerService(
        DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider serviceProvider,
        ILogger<InteractionHandlerService> logger)
    {
        _client = client;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Executes the interaction handler service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Ready += ClientReadyAsync;
        _client.InteractionCreated += HandleInteractionAsync;
        
        // Add modules from the assembly
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        
        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private async Task ClientReadyAsync()
    {
        try
        {
            // Register commands globally
            if (IsDebugBuild())
            {
                // Register to a specific test guild in development
                var testGuildId = GetTestGuildId();
                if (testGuildId != null)
                {
                    await _interactionService.RegisterCommandsToGuildAsync(testGuildId.Value);
                    _logger.LogInformation("Registered commands in test guild {GuildId}", testGuildId.Value);
                }
                else
                {
                    await _interactionService.RegisterCommandsGloballyAsync();
                    _logger.LogInformation("Registered commands globally (development mode)");
                }
            }
            else
            {
                // Register globally in production
                await _interactionService.RegisterCommandsGloballyAsync();
                _logger.LogInformation("Registered commands globally");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register interaction commands");
        }
    }
    
    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            // Create interaction context
            var context = new SocketInteractionContext(_client, interaction);
            
            // Execute the interaction
            var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
            
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        _logger.LogWarning("Unmet precondition: {ErrorMessage}", result.ErrorReason);
                        break;
                    case InteractionCommandError.UnknownCommand:
                        _logger.LogWarning("Unknown command: {ErrorMessage}", result.ErrorReason);
                        break;
                    case InteractionCommandError.BadArgs:
                        _logger.LogWarning("Invalid arguments: {ErrorMessage}", result.ErrorReason);
                        break;
                    case InteractionCommandError.Exception:
                        _logger.LogError("Command exception: {ErrorMessage}", result.ErrorReason);
                        break;
                    case InteractionCommandError.Unsuccessful:
                        _logger.LogError("Command unsuccessful: {ErrorMessage}", result.ErrorReason);
                        break;
                    default:
                        _logger.LogError("Unknown error: {ErrorType}, {ErrorMessage}", result.Error, result.ErrorReason);
                        break;
                }
                
                // Send error message to user if the interaction hasn't been responded to yet
                if (!interaction.HasResponded)
                {
                    await interaction.RespondAsync($"Error: {result.ErrorReason}", ephemeral: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interaction");
            
            // Send error message to user if the interaction hasn't been responded to yet
            if (!interaction.HasResponded && !interaction.IsValidToken)
            {
                await interaction.RespondAsync("An error occurred while processing the command.", ephemeral: true);
            }
        }
    }
    
    private bool IsDebugBuild()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
    
    private ulong? GetTestGuildId()
    {
        var guildIdStr = Environment.GetEnvironmentVariable("TEST_GUILD_ID");
        if (string.IsNullOrEmpty(guildIdStr))
        {
            return null;
        }
        
        if (ulong.TryParse(guildIdStr, out var guildId))
        {
            return guildId;
        }
        
        return null;
    }
}
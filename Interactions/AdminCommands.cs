using Discord;
using Discord.Interactions;
using ElectricRaspberry.Services.Admin;

namespace ElectricRaspberry.Interactions;

/// <summary>
/// Discord slash commands for bot administration
/// </summary>
[Group("bot", "Administrative commands for the bot")]
public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminCommands> _logger;
    
    /// <summary>
    /// Creates a new instance of the admin commands module
    /// </summary>
    /// <param name="adminService">Admin service</param>
    /// <param name="logger">Logger</param>
    public AdminCommands(
        IAdminService adminService,
        ILogger<AdminCommands> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }
    
    /// <summary>
    /// Command to force the bot to enter sleep mode
    /// </summary>
    /// <param name="duration">Optional sleep duration in minutes</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [SlashCommand("sleep", "Force the bot to enter sleep mode")]
    public async Task SleepCommandAsync(
        [Summary(description: "Duration in minutes (optional)")] int? duration = null)
    {
        // Check admin permissions
        if (!await CheckAdminPermissions())
        {
            return;
        }
        
        // Defer the response since this might take a moment
        await DeferAsync(ephemeral: true);
        
        // Execute admin action
        var result = await _adminService.ForceSleepAsync(duration);
        
        if (result)
        {
            var durationText = duration.HasValue ? $" for {duration} minutes" : "";
            await FollowupAsync($"Bot is now sleeping{durationText}.", ephemeral: true);
            _logger.LogInformation("Sleep command executed by {UserId} with duration {Duration}", 
                Context.User.Id, duration);
        }
        else
        {
            await FollowupAsync("Failed to put the bot to sleep. Check the logs for details.", ephemeral: true);
            _logger.LogWarning("Sleep command by {UserId} failed", Context.User.Id);
        }
    }
    
    /// <summary>
    /// Command to force the bot to wake up
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    [SlashCommand("wake", "Force the bot to wake up from sleep mode")]
    public async Task WakeCommandAsync()
    {
        // Check admin permissions
        if (!await CheckAdminPermissions())
        {
            return;
        }
        
        // Defer the response since this might take a moment
        await DeferAsync(ephemeral: true);
        
        // Execute admin action
        var result = await _adminService.ForceWakeAsync();
        
        if (result)
        {
            await FollowupAsync("Bot has been awakened.", ephemeral: true);
            _logger.LogInformation("Wake command executed by {UserId}", Context.User.Id);
        }
        else
        {
            await FollowupAsync("Failed to wake the bot. Check the logs for details.", ephemeral: true);
            _logger.LogWarning("Wake command by {UserId} failed", Context.User.Id);
        }
    }
    
    /// <summary>
    /// Command to silence the bot for a specified duration
    /// </summary>
    /// <param name="duration">Silence duration in minutes</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [SlashCommand("silence", "Stop the bot from sending messages for a duration")]
    public async Task SilenceCommandAsync(
        [Summary(description: "Duration in minutes (default: 15)")] int duration = 15)
    {
        // Check admin permissions
        if (!await CheckAdminPermissions())
        {
            return;
        }
        
        // Defer the response since this might take a moment
        await DeferAsync(ephemeral: true);
        
        // Execute admin action
        var result = await _adminService.SilenceBotAsync(duration);
        
        if (result)
        {
            await FollowupAsync($"Bot has been silenced for {duration} minutes.", ephemeral: true);
            _logger.LogInformation("Silence command executed by {UserId} with duration {Duration}", 
                Context.User.Id, duration);
        }
        else
        {
            await FollowupAsync("Failed to silence the bot. Check the logs for details.", ephemeral: true);
            _logger.LogWarning("Silence command by {UserId} failed", Context.User.Id);
        }
    }
    
    /// <summary>
    /// Command to unsilence the bot
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    [SlashCommand("unsilence", "Allow the bot to send messages again")]
    public async Task UnsilenceCommandAsync()
    {
        // Check admin permissions
        if (!await CheckAdminPermissions())
        {
            return;
        }
        
        // Defer the response since this might take a moment
        await DeferAsync(ephemeral: true);
        
        // Execute admin action
        var result = await _adminService.UnsilenceBotAsync();
        
        if (result)
        {
            await FollowupAsync("Bot has been unsilenced.", ephemeral: true);
            _logger.LogInformation("Unsilence command executed by {UserId}", Context.User.Id);
        }
        else
        {
            await FollowupAsync("Failed to unsilence the bot. Check the logs for details.", ephemeral: true);
            _logger.LogWarning("Unsilence command by {UserId} failed", Context.User.Id);
        }
    }
    
    /// <summary>
    /// Command to reset the bot's state
    /// </summary>
    /// <param name="resetMemory">Whether to also reset the knowledge graph</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [SlashCommand("reset", "Reset the bot's emotional and conversational state")]
    public async Task ResetCommandAsync(
        [Summary(description: "Reset knowledge graph (default: false)")] bool resetMemory = false)
    {
        // Check admin permissions
        if (!await CheckAdminPermissions())
        {
            return;
        }
        
        // Confirm with the user if they want to reset memory
        if (resetMemory)
        {
            var confirmationBuilder = new ComponentBuilder()
                .WithButton("Confirm Reset", "confirm_reset", ButtonStyle.Danger)
                .WithButton("Cancel", "cancel_reset", ButtonStyle.Secondary);
            
            await RespondAsync(
                "⚠️ **WARNING**: You're about to reset the bot's state **including its memory**.\n" +
                "This will delete all knowledge graph entries and relationship data.\n" +
                "This action cannot be undone. Are you sure you want to proceed?",
                components: confirmationBuilder.Build(),
                ephemeral: true);
            
            return;
        }
        
        // Defer the response for regular reset
        await DeferAsync(ephemeral: true);
        
        // Execute admin action
        var result = await _adminService.ResetBotStateAsync(resetMemory: false);
        
        if (result)
        {
            await FollowupAsync("Bot state has been reset (keeping memory intact).", ephemeral: true);
            _logger.LogInformation("Reset command executed by {UserId} (resetMemory: false)", Context.User.Id);
        }
        else
        {
            await FollowupAsync("Failed to reset the bot. Check the logs for details.", ephemeral: true);
            _logger.LogWarning("Reset command by {UserId} failed", Context.User.Id);
        }
    }
    
    /// <summary>
    /// Button component handler for confirming reset with memory wipe
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    [ComponentInteraction("confirm_reset")]
    public async Task ConfirmResetAsync()
    {
        // Check admin permissions
        if (!await CheckAdminPermissions())
        {
            return;
        }
        
        // Defer the response since this might take a moment
        await DeferAsync(ephemeral: true);
        
        // Execute admin action
        var result = await _adminService.ResetBotStateAsync(resetMemory: true);
        
        if (result)
        {
            await FollowupAsync("Bot state has been completely reset, including memory.", ephemeral: true);
            _logger.LogWarning("Reset command executed by {UserId} WITH MEMORY WIPE", Context.User.Id);
        }
        else
        {
            await FollowupAsync("Failed to reset the bot. Check the logs for details.", ephemeral: true);
            _logger.LogWarning("Reset command (with memory wipe) by {UserId} failed", Context.User.Id);
        }
    }
    
    /// <summary>
    /// Button component handler for canceling a reset operation
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    [ComponentInteraction("cancel_reset")]
    public async Task CancelResetAsync()
    {
        await ModifyOriginalResponseAsync(props => {
            props.Content = "Reset operation cancelled.";
            props.Components = new ComponentBuilder().Build();
        });
        
        _logger.LogInformation("Reset with memory wipe was cancelled by {UserId}", Context.User.Id);
    }
    
    /// <summary>
    /// Command to perform an emergency stop of the bot
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    [SlashCommand("emergency-stop", "Immediately stop all bot activities and disconnect")]
    public async Task EmergencyStopCommandAsync()
    {
        // Check admin permissions
        if (!await CheckAdminPermissions())
        {
            return;
        }
        
        // Confirm with the user
        var confirmationBuilder = new ComponentBuilder()
            .WithButton("Confirm Stop", "confirm_stop", ButtonStyle.Danger)
            .WithButton("Cancel", "cancel_stop", ButtonStyle.Secondary);
        
        await RespondAsync(
            "⚠️ **EMERGENCY STOP**: You're about to force disconnect the bot and shut it down.\n" +
            "This will terminate all current operations and the bot will go offline.\n" +
            "Are you sure you want to proceed?",
            components: confirmationBuilder.Build(),
            ephemeral: true);
    }
    
    /// <summary>
    /// Button component handler for confirming emergency stop
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    [ComponentInteraction("confirm_stop")]
    public async Task ConfirmStopAsync()
    {
        // Check admin permissions
        if (!await CheckAdminPermissions())
        {
            return;
        }
        
        await ModifyOriginalResponseAsync(props => {
            props.Content = "Emergency stop initiated. The bot is shutting down.";
            props.Components = new ComponentBuilder().Build();
        });
        
        _logger.LogCritical("Emergency stop initiated by {UserId}", Context.User.Id);
        
        // Execute the stop after responding to the user
        _ = Task.Run(async () => {
            // Small delay to ensure the UI response completes
            await Task.Delay(1000);
            await _adminService.EmergencyStopAsync();
        });
    }
    
    /// <summary>
    /// Button component handler for canceling an emergency stop
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    [ComponentInteraction("cancel_stop")]
    public async Task CancelStopAsync()
    {
        await ModifyOriginalResponseAsync(props => {
            props.Content = "Emergency stop cancelled.";
            props.Components = new ComponentBuilder().Build();
        });
        
        _logger.LogInformation("Emergency stop was cancelled by {UserId}", Context.User.Id);
    }
    
    /// <summary>
    /// Checks if the current user has admin permissions
    /// </summary>
    /// <returns>True if the user has admin permissions, false otherwise</returns>
    private async Task<bool> CheckAdminPermissions()
    {
        var userId = Context.User.Id.ToString();
        var isAdmin = await _adminService.IsAdminAsync(userId);
        
        if (!isAdmin)
        {
            await RespondAsync("You don't have permission to use this command.", ephemeral: true);
            _logger.LogWarning("Unauthorized admin command attempt by {UserId}", userId);
        }
        
        return isAdmin;
    }
}
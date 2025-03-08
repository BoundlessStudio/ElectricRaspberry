using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ElectricRaspberry.Services;
using Microsoft.Extensions.Logging;

namespace ElectricRaspberry.Controllers;

[Group("voice", "Commands for voice channel management")]
public class VoiceCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<VoiceCommands> _logger;
    private readonly IVoiceService _voiceService;
    private readonly IStaminaService _staminaService;

    public VoiceCommands(
        ILogger<VoiceCommands> logger,
        IVoiceService voiceService,
        IStaminaService staminaService)
    {
        _logger = logger;
        _voiceService = voiceService;
        _staminaService = staminaService;
    }

    [SlashCommand("join", "Make the bot join your current voice channel")]
    public async Task JoinCommand()
    {
        await DeferAsync();

        try
        {
            // Get the user's voice channel
            var guildUser = Context.User as SocketGuildUser;
            var voiceChannel = guildUser?.VoiceChannel;

            if (voiceChannel == null)
            {
                await FollowupAsync("You need to be in a voice channel first!", ephemeral: true);
                return;
            }

            // Check if we're already connected to voice
            if (await _voiceService.IsConnectedToVoiceAsync())
            {
                var currentChannel = await _voiceService.GetCurrentVoiceChannelAsync();
                if (currentChannel?.Id == voiceChannel.Id)
                {
                    await FollowupAsync($"I'm already in {voiceChannel.Name}!", ephemeral: true);
                    return;
                }
            }

            // Check if we have enough stamina
            var stamina = await _staminaService.GetCurrentStaminaAsync();
            if (stamina < 20)
            {
                await FollowupAsync("I don't have enough energy to join voice right now. Please try again later.", ephemeral: true);
                return;
            }

            // Join the voice channel
            await _voiceService.JoinVoiceChannelAsync(voiceChannel);
            
            await FollowupAsync($"Joined voice channel: {voiceChannel.Name}");
            _logger.LogInformation("Joined voice channel {ChannelName} via command from {Username}",
                voiceChannel.Name, Context.User.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing join voice command");
            await FollowupAsync("Failed to join voice channel. Please try again later.", ephemeral: true);
        }
    }

    [SlashCommand("leave", "Make the bot leave its current voice channel")]
    public async Task LeaveCommand()
    {
        await DeferAsync();

        try
        {
            // Check if we're connected to voice
            if (!await _voiceService.IsConnectedToVoiceAsync())
            {
                await FollowupAsync("I'm not currently in any voice channel!", ephemeral: true);
                return;
            }

            var currentChannel = await _voiceService.GetCurrentVoiceChannelAsync();
            
            // Leave the voice channel
            await _voiceService.LeaveVoiceChannelAsync();
            
            await FollowupAsync($"Left voice channel: {currentChannel?.Name ?? "Unknown"}");
            _logger.LogInformation("Left voice channel {ChannelName} via command from {Username}",
                currentChannel?.Name ?? "Unknown", Context.User.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing leave voice command");
            await FollowupAsync("Failed to leave voice channel. Please try again later.", ephemeral: true);
        }
    }

    [SlashCommand("status", "Check the bot's current voice status")]
    public async Task StatusCommand()
    {
        await DeferAsync();

        try
        {
            // Check if we're connected to voice
            if (!await _voiceService.IsConnectedToVoiceAsync())
            {
                await FollowupAsync("I'm not currently in any voice channel.", ephemeral: true);
                return;
            }

            var currentChannel = await _voiceService.GetCurrentVoiceChannelAsync();
            var usersInChannel = await _voiceService.GetUsersInVoiceChannelAsync();
            var stamina = await _staminaService.GetCurrentStaminaAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🎤 **Voice Status**");
            sb.AppendLine($"📢 Channel: {currentChannel?.Name ?? "Unknown"}");
            sb.AppendLine($"👥 Users: {usersInChannel.Count}");
            sb.AppendLine($"⚡ Current Stamina: {stamina:F1}/100");
            
            await FollowupAsync(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing voice status command");
            await FollowupAsync("Failed to retrieve voice status. Please try again later.", ephemeral: true);
        }
    }
}
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using ElectricRaspberry.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ElectricRaspberry.Services;

public class VoiceService : IVoiceService
{
    private readonly ILogger<VoiceService> _logger;
    private readonly DiscordSocketClient _discordClient;
    private readonly IStaminaService _staminaService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly StaminaSettings _staminaSettings;
    
    private readonly SemaphoreSlim _voiceLock = new(1, 1);
    private IAudioClient? _audioClient;
    private IVoiceChannel? _currentVoiceChannel;
    private DateTime _joinedVoiceAt = DateTime.MinValue;
    private readonly ConcurrentDictionary<ulong, DateTime> _userSpeakingTimes = new();
    
    // Track the last time the stamina was consumed for voice presence
    private DateTime _lastVoiceStaminaConsumption = DateTime.MinValue;
    
    public VoiceService(
        ILogger<VoiceService> logger,
        DiscordSocketClient discordClient,
        IStaminaService staminaService,
        IKnowledgeService knowledgeService,
        IOptions<StaminaSettings> staminaSettings)
    {
        _logger = logger;
        _discordClient = discordClient;
        _staminaService = staminaService;
        _knowledgeService = knowledgeService;
        _staminaSettings = staminaSettings.Value;
        
        // Register voice activity events
        _discordClient.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        
        // Start a background timer to consume stamina while in voice
        StartVoiceStaminaConsumptionTimer();
    }
    
    public async Task<bool> IsConnectedToVoiceAsync()
    {
        await _voiceLock.WaitAsync();
        try
        {
            return _audioClient != null && _currentVoiceChannel != null;
        }
        finally
        {
            _voiceLock.Release();
        }
    }
    
    public async Task<IAudioClient> JoinVoiceChannelAsync(IVoiceChannel voiceChannel)
    {
        if (voiceChannel == null)
            throw new ArgumentNullException(nameof(voiceChannel));
        
        await _voiceLock.WaitAsync();
        try
        {
            // If already connected to this channel, return existing client
            if (_currentVoiceChannel?.Id == voiceChannel.Id && _audioClient != null)
                return _audioClient;
            
            // Leave current channel if in a different one
            if (_currentVoiceChannel != null && _audioClient != null)
                await LeaveVoiceChannelAsync();
            
            _logger.LogInformation("Joining voice channel: {ChannelName} (ID: {ChannelId})",
                voiceChannel.Name, voiceChannel.Id);
            
            // Connect to the new voice channel
            _audioClient = await voiceChannel.ConnectAsync();
            _currentVoiceChannel = voiceChannel;
            _joinedVoiceAt = DateTime.UtcNow;
            
            // Register handlers for audio client events
            // Temporarily commenting out these event handlers since they're causing issues
            // Will need to update the signatures to match Discord.NET's requirements
            //_audioClient.StreamCreated += OnStreamCreated;
            //_audioClient.StreamDestroyed += OnStreamDestroyed;
            //_audioClient.UserSpeaking += OnUserSpeaking;
            
            // Initial stamina consumption for joining voice
            await _staminaService.ConsumeStaminaAsync(1.0);
            _lastVoiceStaminaConsumption = DateTime.UtcNow;
            
            return _audioClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining voice channel: {ChannelName} (ID: {ChannelId})",
                voiceChannel.Name, voiceChannel.Id);
            
            // Clear state in case of error
            _audioClient = null;
            _currentVoiceChannel = null;
            
            throw;
        }
        finally
        {
            _voiceLock.Release();
        }
    }
    
    public async Task LeaveVoiceChannelAsync()
    {
        await _voiceLock.WaitAsync();
        try
        {
            if (_audioClient != null)
            {
                _logger.LogInformation("Leaving voice channel: {ChannelName} (ID: {ChannelId})",
                    _currentVoiceChannel?.Name, _currentVoiceChannel?.Id);
                
                // Unregister handlers
                // Temporarily commenting out these event handlers since they're causing issues
                //_audioClient.StreamCreated -= OnStreamCreated;
                //_audioClient.StreamDestroyed -= OnStreamDestroyed;
                //_audioClient.UserSpeaking -= OnUserSpeaking;
                
                // Calculate total time spent in voice and consume any remaining stamina
                if (_joinedVoiceAt != DateTime.MinValue)
                {
                    var timeInVoice = DateTime.UtcNow - _joinedVoiceAt;
                    var minutesInVoice = timeInVoice.TotalMinutes;
                    
                    _logger.LogInformation("Spent {Minutes:F2} minutes in voice channel", minutesInVoice);
                }
                
                // Disconnect
                await _audioClient.StopAsync();
                _audioClient = null;
                _currentVoiceChannel = null;
                _joinedVoiceAt = DateTime.MinValue;
                _userSpeakingTimes.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving voice channel");
            
            // Clear state even in case of error
            _audioClient = null;
            _currentVoiceChannel = null;
            _joinedVoiceAt = DateTime.MinValue;
            _userSpeakingTimes.Clear();
        }
        finally
        {
            _voiceLock.Release();
        }
    }
    
    public async Task<IVoiceChannel?> GetCurrentVoiceChannelAsync()
    {
        await _voiceLock.WaitAsync();
        try
        {
            return _currentVoiceChannel;
        }
        finally
        {
            _voiceLock.Release();
        }
    }
    
    public async Task<IReadOnlyCollection<SocketGuildUser>> GetUsersInVoiceChannelAsync()
    {
        await _voiceLock.WaitAsync();
        try
        {
            if (_currentVoiceChannel == null)
                return Array.Empty<SocketGuildUser>();
            
            // If not a guild voice channel, return empty collection
            if (_currentVoiceChannel is not SocketVoiceChannel socketVoiceChannel)
                return Array.Empty<SocketGuildUser>();
            
            // Get users in the channel
            return socketVoiceChannel.ConnectedUsers;
        }
        finally
        {
            _voiceLock.Release();
        }
    }
    
    public async Task ProcessVoiceStateUpdateAsync(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        // Skip if it's the bot's own state update
        if (user.Id == _discordClient.CurrentUser.Id)
            return;
        
        // User joined a voice channel
        if (oldState.VoiceChannel == null && newState.VoiceChannel != null)
        {
            _logger.LogDebug("User {Username} joined voice channel {ChannelName}",
                user.Username, newState.VoiceChannel.Name);
            
            // Consider joining this channel if there are people we interact with
            if (await ShouldJoinVoiceChannelAsync(newState.VoiceChannel))
            {
                await JoinVoiceChannelAsync(newState.VoiceChannel);
            }
        }
        // User left a voice channel
        else if (oldState.VoiceChannel != null && newState.VoiceChannel == null)
        {
            _logger.LogDebug("User {Username} left voice channel {ChannelName}",
                user.Username, oldState.VoiceChannel.Name);
            
            // If we're in the same voice channel and it's now empty, consider leaving
            if (_currentVoiceChannel?.Id == oldState.VoiceChannel.Id)
            {
                if (await ShouldLeaveVoiceChannelAsync())
                {
                    await LeaveVoiceChannelAsync();
                }
            }
        }
        // User moved between voice channels
        else if (oldState.VoiceChannel != null && newState.VoiceChannel != null && 
                 oldState.VoiceChannel.Id != newState.VoiceChannel.Id)
        {
            _logger.LogDebug("User {Username} moved from voice channel {OldChannelName} to {NewChannelName}",
                user.Username, oldState.VoiceChannel.Name, newState.VoiceChannel.Name);
            
            // If we're in the channel they left, consider leaving or following them
            if (_currentVoiceChannel?.Id == oldState.VoiceChannel.Id)
            {
                if (await ShouldLeaveVoiceChannelAsync())
                {
                    await LeaveVoiceChannelAsync();
                }
                else if (await ShouldJoinVoiceChannelAsync(newState.VoiceChannel))
                {
                    await JoinVoiceChannelAsync(newState.VoiceChannel);
                }
            }
            // If we're not in a voice channel, consider joining the new one
            else if (_currentVoiceChannel == null)
            {
                if (await ShouldJoinVoiceChannelAsync(newState.VoiceChannel))
                {
                    await JoinVoiceChannelAsync(newState.VoiceChannel);
                }
            }
        }
    }
    
    public async Task<bool> ShouldJoinVoiceChannelAsync(IVoiceChannel voiceChannel)
    {
        // Don't join if we're already in a different voice channel
        if (_currentVoiceChannel != null && _currentVoiceChannel.Id != voiceChannel.Id)
            return false;
        
        // Don't join if currently sleeping
        if (await _staminaService.IsSleepingAsync())
            return false;
        
        // Don't join if low on stamina
        double currentStamina = await _staminaService.GetCurrentStaminaAsync();
        if (currentStamina < 30)
            return false;
        
        // Only join if there are at least 2 other users in the channel
        if (voiceChannel is SocketVoiceChannel socketVoiceChannel && 
            socketVoiceChannel.ConnectedUsers.Count < 2)
            return false;
        
        // Check if users in the channel are ones we interact with regularly
        // This would require accessing the knowledge graph to check relationship strengths
        if (voiceChannel is SocketVoiceChannel svc)
        {
            int closeRelationships = 0;
            
            foreach (var user in svc.ConnectedUsers)
            {
                // Skip bots
                if (user.IsBot) continue;
                
                // Check relationship strength with this user
                // This is a simplified version; in practice you'd query the knowledge graph
                var relationship = await _knowledgeService.GetUserRelationshipAsync(user.Id.ToString());
                if (relationship != null && relationship.Strength > 0.3)
                {
                    closeRelationships++;
                }
            }
            
            // Join if there are users we have relationships with
            return closeRelationships > 0;
        }
        
        return false;
    }
    
    public async Task<bool> ShouldLeaveVoiceChannelAsync()
    {
        if (_currentVoiceChannel == null || _audioClient == null)
            return false;
        
        // Leave if currently sleeping
        if (await _staminaService.IsSleepingAsync())
            return true;
        
        // Leave if stamina gets too low
        double currentStamina = await _staminaService.GetCurrentStaminaAsync();
        if (currentStamina < 10)
            return true;
        
        // Check how many users are in the channel
        var usersInChannel = await GetUsersInVoiceChannelAsync();
        
        // Leave if the channel is empty (except for the bot)
        if (usersInChannel.Count <= 1)
            return true;
        
        // Leave if all remaining users are bots
        if (usersInChannel.All(u => u.IsBot))
            return true;
        
        // Leave if been in voice for too long (30 minutes max)
        var timeInVoice = DateTime.UtcNow - _joinedVoiceAt;
        if (timeInVoice.TotalMinutes > 30)
            return true;
        
        return false;
    }
    
    private Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        // Process the voice state update asynchronously
        _ = Task.Run(() => ProcessVoiceStateUpdateAsync(user, oldState, newState));
        return Task.CompletedTask;
    }
    
    private Task OnStreamCreated(uint streamId, uint userId)
    {
        _logger.LogDebug("Audio stream created - StreamId: {StreamId}, UserId: {UserId}", streamId, userId);
        return Task.CompletedTask;
    }
    
    private Task OnStreamDestroyed(uint streamId, uint userId)
    {
        _logger.LogDebug("Audio stream destroyed - StreamId: {StreamId}, UserId: {UserId}", streamId, userId);
        return Task.CompletedTask;
    }
    
    private Task OnUserSpeaking(ulong userId, bool speaking)
    {
        if (speaking)
        {
            _logger.LogDebug("User {UserId} started speaking", userId);
            _userSpeakingTimes[userId] = DateTime.UtcNow;
        }
        else
        {
            _logger.LogDebug("User {UserId} stopped speaking", userId);
            
            // Calculate speaking duration
            if (_userSpeakingTimes.TryGetValue(userId, out var startTime))
            {
                var speakingDuration = DateTime.UtcNow - startTime;
                _logger.LogDebug("User {UserId} spoke for {Duration} seconds", 
                    userId, speakingDuration.TotalSeconds);
                
                // Remove from tracking dictionary
                _userSpeakingTimes.TryRemove(userId, out _);
            }
        }
        
        return Task.CompletedTask;
    }
    
    private void StartVoiceStaminaConsumptionTimer()
    {
        // Start a timer to periodically consume stamina while in voice
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    
                    // Only consume stamina if we're in a voice channel
                    if (await IsConnectedToVoiceAsync())
                    {
                        var timeSinceLastConsumption = DateTime.UtcNow - _lastVoiceStaminaConsumption;
                        var minutesSinceLastConsumption = timeSinceLastConsumption.TotalMinutes;
                        
                        // Only consume if at least a minute has passed
                        if (minutesSinceLastConsumption >= 1)
                        {
                            double staminaToConsume = _staminaSettings.VoiceMinuteCost * minutesSinceLastConsumption;
                            
                            await _staminaService.ConsumeStaminaAsync(staminaToConsume);
                            _lastVoiceStaminaConsumption = DateTime.UtcNow;
                            
                            _logger.LogDebug("Consumed {StaminaAmount} stamina for {Minutes} minutes in voice", 
                                staminaToConsume, minutesSinceLastConsumption);
                            
                            // Check if we should leave due to low stamina
                            if (await ShouldLeaveVoiceChannelAsync())
                            {
                                _logger.LogInformation("Leaving voice channel due to low stamina");
                                await LeaveVoiceChannelAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in voice stamina consumption timer");
                }
            }
        });
    }
}
using Discord;
using Discord.WebSocket;
using ElectricRaspberry.Services.Admin.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Services.Admin;

/// <summary>
/// Service for bot administration operations
/// </summary>
public class AdminService : IAdminService
{
    private readonly IStaminaService _staminaService;
    private readonly IEmotionalService _emotionalService;
    private readonly IConversationService _conversationService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly DiscordSocketClient _discordClient;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<AdminService> _logger;
    private readonly AdminOptions _options;
    
    private bool _isSilenced;
    private DateTime? _silenceEndTime;
    private readonly object _silenceLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _operationTokens = new();
    
    /// <summary>
    /// Creates a new instance of the admin service
    /// </summary>
    /// <param name="staminaService">Stamina service</param>
    /// <param name="emotionalService">Emotional service</param>
    /// <param name="conversationService">Conversation service</param>
    /// <param name="knowledgeService">Knowledge service</param>
    /// <param name="discordClient">Discord client</param>
    /// <param name="applicationLifetime">Application lifetime</param>
    /// <param name="options">Admin options</param>
    /// <param name="logger">Logger</param>
    public AdminService(
        IStaminaService staminaService,
        IEmotionalService emotionalService,
        IConversationService conversationService,
        IKnowledgeService knowledgeService,
        DiscordSocketClient discordClient,
        IHostApplicationLifetime applicationLifetime,
        IOptions<AdminOptions> options,
        ILogger<AdminService> logger)
    {
        _staminaService = staminaService;
        _emotionalService = emotionalService;
        _conversationService = conversationService;
        _knowledgeService = knowledgeService;
        _discordClient = discordClient;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _options = options.Value;
        
        // Start background task to check silence state
        _ = Task.Run(MonitorSilenceStateAsync);
    }
    
    /// <summary>
    /// Forces the bot to enter sleep mode
    /// </summary>
    /// <param name="durationMinutes">Optional sleep duration in minutes (null for indefinite)</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    public async Task<bool> ForceSleepAsync(int? durationMinutes = null)
    {
        try
        {
            // Cancel any active operations except emergency stop
            CancelActiveOperations(except: "emergencyStop");
            
            // Register a new sleep operation token
            var sleepToken = new CancellationTokenSource();
            _operationTokens["sleep"] = sleepToken;
            
            // Set duration (use default if not specified, and cap at maximum)
            var duration = durationMinutes ?? _options.DefaultSleepDurationMinutes;
            if (duration > _options.MaxSleepDurationMinutes)
            {
                duration = _options.MaxSleepDurationMinutes;
            }
            
            // Set sleep mode
            var sleepDuration = duration > 0 ? TimeSpan.FromMinutes(duration) : TimeSpan.FromMinutes(_options.DefaultSleepDurationMinutes);
            await _staminaService.ForceSleepModeAsync(sleepDuration);
            
            // Update Discord status
            await _discordClient.SetStatusAsync(UserStatus.Idle);
            await _discordClient.SetActivityAsync(new Game("Sleeping", ActivityType.CustomStatus));
            
            _logger.LogInformation("Bot forced to sleep for {Duration} minutes", duration);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing bot to sleep");
            return false;
        }
    }
    
    /// <summary>
    /// Forces the bot to wake up from sleep mode
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    public async Task<bool> ForceWakeAsync()
    {
        try
        {
            // Cancel any active sleep operation
            if (_operationTokens.TryGetValue("sleep", out var sleepToken))
            {
                sleepToken.Cancel();
                _operationTokens.Remove("sleep");
            }
            
            // Wake up the bot
            await _staminaService.ForceWakeAsync();
            
            // Update Discord status
            await _discordClient.SetStatusAsync(UserStatus.Online);
            await _discordClient.SetActivityAsync(new Game("Awake and ready!", ActivityType.CustomStatus));
            
            _logger.LogInformation("Bot forced to wake up");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing bot to wake up");
            return false;
        }
    }
    
    /// <summary>
    /// Stops the bot from sending messages for a specified duration
    /// </summary>
    /// <param name="durationMinutes">Silence duration in minutes</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    public async Task<bool> SilenceBotAsync(int durationMinutes)
    {
        try
        {
            // Set duration (use default if not specified or invalid, and cap at maximum)
            var duration = durationMinutes <= 0 ? _options.DefaultSilenceDurationMinutes : durationMinutes;
            if (duration > _options.MaxSilenceDurationMinutes)
            {
                duration = _options.MaxSilenceDurationMinutes;
            }
            
            // Set silence state
            lock (_silenceLock)
            {
                _isSilenced = true;
                _silenceEndTime = DateTime.UtcNow.AddMinutes(duration);
            }
            
            // Update Discord status
            await _discordClient.SetStatusAsync(UserStatus.DoNotDisturb);
            await _discordClient.SetActivityAsync(new Game("Silenced", ActivityType.CustomStatus));
            
            _logger.LogInformation("Bot silenced for {Duration} minutes", duration);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error silencing bot");
            return false;
        }
    }
    
    /// <summary>
    /// Removes the silence restriction from the bot
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    public async Task<bool> UnsilenceBotAsync()
    {
        try
        {
            // Clear silence state
            lock (_silenceLock)
            {
                _isSilenced = false;
                _silenceEndTime = null;
            }
            
            // Update Discord status based on current stamina state
            var isSleeping = await _staminaService.IsSleepingAsync();
            await _discordClient.SetStatusAsync(isSleeping ? UserStatus.Idle : UserStatus.Online);
            await _discordClient.SetActivityAsync(null);
            
            _logger.LogInformation("Bot unsilenced");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsilencing bot");
            return false;
        }
    }
    
    /// <summary>
    /// Resets the bot's emotional and conversational state
    /// </summary>
    /// <param name="resetMemory">Whether to also reset the knowledge graph (defaults to false)</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    public async Task<bool> ResetBotStateAsync(bool resetMemory = false)
    {
        try
        {
            // Reset stamina
            await _staminaService.ResetStaminaAsync();
            
            // Reset emotional state
            await _emotionalService.ResetEmotionalStateAsync();
            
            // Reset conversations
            await _conversationService.ResetAllConversationsAsync();
            
            // Reset memory if requested
            if (resetMemory)
            {
                await _knowledgeService.ResetKnowledgeGraphAsync();
                _logger.LogWarning("Bot knowledge graph has been reset");
            }
            
            // Update Discord status
            await _discordClient.SetStatusAsync(UserStatus.Online);
            await _discordClient.SetActivityAsync(new Game("Reset complete", ActivityType.CustomStatus));
            
            _logger.LogInformation("Bot state reset (including memory: {ResetMemory})", resetMemory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting bot state");
            return false;
        }
    }
    
    /// <summary>
    /// Immediately stops all bot activities and disconnects from Discord
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. Returns true if successful</returns>
    public async Task<bool> EmergencyStopAsync()
    {
        try
        {
            // Cancel all active operations
            CancelActiveOperations();
            
            // Register emergency stop token
            var emergencyToken = new CancellationTokenSource();
            _operationTokens["emergencyStop"] = emergencyToken;
            
            // Log the emergency stop
            _logger.LogCritical("Emergency stop initiated");
            
            // Update Discord status
            await _discordClient.SetStatusAsync(UserStatus.Invisible);
            
            // Disconnect from Discord
            await _discordClient.LogoutAsync();
            await _discordClient.StopAsync();
            
            // Trigger application shutdown after a brief delay to allow logs to flush
            Task.Delay(1000).ContinueWith(_ => _applicationLifetime.StopApplication());
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during emergency stop");
            return false;
        }
    }
    
    /// <summary>
    /// Checks if the given user ID has admin permissions for the bot
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if the user is an admin</returns>
    public async Task<bool> IsAdminAsync(string userId)
    {
        try
        {
            // Check if user ID is in the admin list
            if (_options.AdminUserIds.Contains(userId))
            {
                return true;
            }
            
            // Check if user has any admin roles
            var userSocketId = ulong.Parse(userId);
            var guildUsers = _discordClient.Guilds
                .SelectMany(g => g.Users)
                .Where(u => u.Id == userSocketId);
            
            foreach (var guildUser in guildUsers)
            {
                foreach (var role in guildUser.Roles)
                {
                    if (_options.AdminRoleIds.Contains(role.Id.ToString()))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin status for user {UserId}", userId);
            return false;
        }
    }
    
    /// <summary>
    /// Checks the current silence state
    /// </summary>
    /// <returns>True if the bot is currently silenced</returns>
    public bool IsSilenced()
    {
        lock (_silenceLock)
        {
            if (!_isSilenced)
            {
                return false;
            }
            
            // Check if silence period has expired
            if (_silenceEndTime.HasValue && DateTime.UtcNow >= _silenceEndTime.Value)
            {
                _isSilenced = false;
                _silenceEndTime = null;
                _logger.LogInformation("Bot silence period has ended");
                return false;
            }
            
            return true;
        }
    }
    
    private async Task MonitorSilenceStateAsync()
    {
        while (true)
        {
            try
            {
                // Check for expired silence state
                bool wasSilenced = false;
                lock (_silenceLock)
                {
                    if (_isSilenced && _silenceEndTime.HasValue && DateTime.UtcNow >= _silenceEndTime.Value)
                    {
                        _isSilenced = false;
                        _silenceEndTime = null;
                        wasSilenced = true;
                    }
                }
                
                // Update Discord status if silence just ended
                if (wasSilenced)
                {
                    _logger.LogInformation("Bot silence period has ended");
                    
                    // Update Discord status based on current stamina state
                    var isSleeping = await _staminaService.IsSleepingAsync();
                    await _discordClient.SetStatusAsync(isSleeping ? UserStatus.Idle : UserStatus.Online);
                    await _discordClient.SetActivityAsync(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in silence state monitor");
            }
            
            // Check every minute
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
    
    private void CancelActiveOperations(string except = null)
    {
        foreach (var (key, tokenSource) in _operationTokens)
        {
            if (key != except && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
                _logger.LogInformation("Canceled operation: {OperationType}", key);
            }
        }
    }
}
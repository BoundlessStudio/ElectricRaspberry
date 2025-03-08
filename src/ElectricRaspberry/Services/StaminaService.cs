using ElectricRaspberry.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Discord;

namespace ElectricRaspberry.Services;

public class StaminaService : IStaminaService
{
    private readonly ILogger<StaminaService> _logger;
    private readonly StaminaSettings _staminaSettings;
    private readonly SemaphoreSlim _staminaLock = new(1, 1);
    private readonly IDiscordClient _discordClient;
    
    private double _currentStamina;
    private bool _isSleeping;
    private DateTime _sleepUntil = DateTime.MinValue;
    private DateTime _lastStaminaUpdate = DateTime.UtcNow;
    
    public StaminaService(
        ILogger<StaminaService> logger,
        IOptions<StaminaSettings> staminaSettings,
        IDiscordClient discordClient)
    {
        _logger = logger;
        _staminaSettings = staminaSettings.Value;
        _discordClient = discordClient;
        _currentStamina = _staminaSettings.MaxStamina;
    }
    
    public async Task<double> GetCurrentStaminaAsync()
    {
        await _staminaLock.WaitAsync();
        try
        {
            // Calculate passive recovery since last update
            await UpdateStaminaFromLastCheckAsync();
            return _currentStamina;
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task<double> ConsumeStaminaAsync(double amount)
    {
        await _staminaLock.WaitAsync();
        try
        {
            // First apply any pending recovery
            await UpdateStaminaFromLastCheckAsync();
            
            // Then consume the stamina
            _currentStamina = Math.Max(0, _currentStamina - amount);
            
            _logger.LogDebug("Consumed {Amount} stamina. Current: {CurrentStamina}", amount, _currentStamina);
            
            // Check if we should enter sleep mode
            if (await ShouldSleepAsync() && !_isSleeping)
            {
                _logger.LogInformation("Stamina below threshold ({Threshold}). Automatically entering sleep mode.", 
                    _staminaSettings.LowStaminaThreshold);
                
                await EnterSleepModeAsync();
            }
            
            return _currentStamina;
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task<double> RecoverStaminaAsync(double minutes)
    {
        await _staminaLock.WaitAsync();
        try
        {
            double recoveryRate = _staminaSettings.RecoveryRatePerMinute;
            
            // Apply sleep multiplier if sleeping
            if (_isSleeping)
            {
                recoveryRate *= _staminaSettings.SleepRecoveryMultiplier;
            }
            
            double recovery = minutes * recoveryRate;
            _currentStamina = Math.Min(_staminaSettings.MaxStamina, _currentStamina + recovery);
            
            _logger.LogDebug("Recovered {Recovery} stamina over {Minutes} minutes. Current: {CurrentStamina}", 
                recovery, minutes, _currentStamina);
            
            // Update the last check time
            _lastStaminaUpdate = DateTime.UtcNow;
            
            // Check if we should wake up
            if (_isSleeping && await ShouldWakeAsync())
            {
                await ExitSleepModeAsync();
            }
            
            return _currentStamina;
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task<bool> IsSleepingAsync()
    {
        await _staminaLock.WaitAsync();
        try
        {
            // Check if timed sleep has expired
            if (_isSleeping && _sleepUntil != DateTime.MinValue && DateTime.UtcNow > _sleepUntil)
            {
                _logger.LogInformation("Timed sleep duration has ended. Waking up.");
                await ExitSleepModeAsync();
            }
            
            return _isSleeping;
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task ForceSleepModeAsync(TimeSpan? duration = null)
    {
        await _staminaLock.WaitAsync();
        try
        {
            if (duration.HasValue)
            {
                _sleepUntil = DateTime.UtcNow.Add(duration.Value);
                _logger.LogInformation("Entering forced sleep mode for {Duration}", duration.Value);
            }
            else
            {
                _sleepUntil = DateTime.MinValue;
                _logger.LogInformation("Entering forced sleep mode indefinitely");
            }
            
            await EnterSleepModeAsync();
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task ForceWakeAsync()
    {
        await _staminaLock.WaitAsync();
        try
        {
            _logger.LogInformation("Force waking from sleep mode");
            await ExitSleepModeAsync();
            
            // Reset the sleep timer
            _sleepUntil = DateTime.MinValue;
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task<bool> ShouldSleepAsync()
    {
        await _staminaLock.WaitAsync();
        try
        {
            return _currentStamina < _staminaSettings.LowStaminaThreshold;
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task<bool> ShouldWakeAsync()
    {
        await _staminaLock.WaitAsync();
        try
        {
            // Wake up if stamina is at least 80%
            double wakeThreshold = _staminaSettings.MaxStamina * 0.8;
            return _currentStamina >= wakeThreshold;
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task ResetStaminaAsync()
    {
        await _staminaLock.WaitAsync();
        try
        {
            _currentStamina = _staminaSettings.MaxStamina;
            _logger.LogInformation("Stamina reset to maximum value: {MaxStamina}", _staminaSettings.MaxStamina);
            
            // If sleeping, wake up since we have full stamina
            if (_isSleeping)
            {
                await ExitSleepModeAsync();
            }
        }
        finally
        {
            _staminaLock.Release();
        }
    }
    
    public async Task<double> GetMaxStaminaAsync()
    {
        return _staminaSettings.MaxStamina;
    }
    
    private async Task UpdateStaminaFromLastCheckAsync()
    {
        // Calculate time since last update
        var now = DateTime.UtcNow;
        var timeDiff = now - _lastStaminaUpdate;
        double minutesElapsed = timeDiff.TotalMinutes;
        
        if (minutesElapsed > 0)
        {
            double recoveryRate = _staminaSettings.RecoveryRatePerMinute;
            
            // Apply sleep multiplier if sleeping
            if (_isSleeping)
            {
                recoveryRate *= _staminaSettings.SleepRecoveryMultiplier;
            }
            
            double recovery = minutesElapsed * recoveryRate;
            _currentStamina = Math.Min(_staminaSettings.MaxStamina, _currentStamina + recovery);
            
            _logger.LogDebug("Passive recovery: {Recovery} stamina over {Minutes} minutes", recovery, minutesElapsed);
            
            // Update the timestamp
            _lastStaminaUpdate = now;
            
            // Check if we should wake up from sleep mode
            if (_isSleeping && await ShouldWakeAsync())
            {
                await ExitSleepModeAsync();
            }
        }
    }
    
    private async Task EnterSleepModeAsync()
    {
        if (!_isSleeping)
        {
            _isSleeping = true;
            
            // Set Discord status to sleeping
            var client = _discordClient as Discord.WebSocket.DiscordSocketClient;
            if (client != null)
            {
                await client.SetStatusAsync(UserStatus.Idle);
                await client.SetActivityAsync(new Game("Sleeping... Zzz", ActivityType.Playing));
            }
            else
            {
                _logger.LogWarning("Unable to set Discord status: client is not a DiscordSocketClient");
            }
            
            _logger.LogInformation("Entered sleep mode");
        }
    }
    
    private async Task ExitSleepModeAsync()
    {
        if (_isSleeping)
        {
            _isSleeping = false;
            _sleepUntil = DateTime.MinValue;
            
            // Reset Discord status
            var client = _discordClient as Discord.WebSocket.DiscordSocketClient;
            if (client != null)
            {
                await client.SetStatusAsync(UserStatus.Online);
                await client.SetActivityAsync(new Game("Active and Energized", ActivityType.Playing));
            }
            else
            {
                _logger.LogWarning("Unable to set Discord status: client is not a DiscordSocketClient");
            }
            
            _logger.LogInformation("Exited sleep mode");
        }
    }
}
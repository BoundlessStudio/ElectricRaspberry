using ElectricRaspberry.Notifications;
using ElectricRaspberry.Services;
using MediatR;

namespace ElectricRaspberry.Handlers;

public class UserVoiceStateUpdatedHandler : INotificationHandler<UserVoiceStateUpdatedNotification>
{
    private readonly ILogger<UserVoiceStateUpdatedHandler> _logger;
    private readonly IVoiceService _voiceService;
    private readonly IStaminaService _staminaService;

    public UserVoiceStateUpdatedHandler(
        ILogger<UserVoiceStateUpdatedHandler> logger,
        IVoiceService voiceService,
        IStaminaService staminaService)
    {
        _logger = logger;
        _voiceService = voiceService;
        _staminaService = staminaService;
    }

    public async Task Handle(UserVoiceStateUpdatedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            // Check if we're sleeping, if so, don't process voice updates
            if (await _staminaService.IsSleepingAsync())
                return;

            // Process the voice state update through the voice service
            await _voiceService.ProcessVoiceStateUpdateAsync(
                notification.User, 
                notification.OldState, 
                notification.NewState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling voice state update for user {UserId}", notification.User.Id);
        }
    }
}

public class UserVoiceServerUpdatedHandler : INotificationHandler<UserVoiceServerUpdatedNotification>
{
    private readonly ILogger<UserVoiceServerUpdatedHandler> _logger;

    public UserVoiceServerUpdatedHandler(
        ILogger<UserVoiceServerUpdatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserVoiceServerUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Voice server updated for user {UserId}", notification.User.Id);
        
        // This event is primarily for voice reconnection handling
        // We don't need to do much here except log it for now
        
        return Task.CompletedTask;
    }
}
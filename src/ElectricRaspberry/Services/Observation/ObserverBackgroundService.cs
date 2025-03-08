using ElectricRaspberry.Services.Observation.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Background service for performing scheduled observation tasks
/// </summary>
public class ObserverBackgroundService : BackgroundService
{
    private readonly IObserverService _observerService;
    private readonly ILogger<ObserverBackgroundService> _logger;
    private readonly ObserverBackgroundOptions _options;
    
    /// <summary>
    /// Creates a new instance of the observer background service
    /// </summary>
    /// <param name="observerService">Observer service</param>
    /// <param name="options">Background service options</param>
    /// <param name="logger">Logger</param>
    public ObserverBackgroundService(
        IObserverService observerService,
        IOptions<ObserverBackgroundOptions> options,
        ILogger<ObserverBackgroundService> logger)
    {
        _observerService = observerService;
        _logger = logger;
        _options = options.Value;
    }
    
    /// <summary>
    /// Executes the background service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Observer background service starting");
        
        // Create recurring tasks
        var processingTask = ProcessPrioritizedEventsAsync(stoppingToken);
        var maintenanceTask = PerformMaintenanceAsync(stoppingToken);
        
        // Wait for any task to complete (or be cancelled)
        await Task.WhenAny(processingTask, maintenanceTask);
        
        _logger.LogInformation("Observer background service stopping");
    }
    
    private async Task ProcessPrioritizedEventsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process prioritized events
                await _observerService.ProcessPrioritizedEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing prioritized events");
            }
            
            // Wait before next processing cycle
            await Task.Delay(_options.ProcessingIntervalMs, stoppingToken);
        }
    }
    
    private async Task PerformMaintenanceAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Perform maintenance operations
                _observerService.PerformMaintenance();
                _logger.LogDebug("Performed observer maintenance");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing observer maintenance");
            }
            
            // Maintenance runs less frequently than processing
            await Task.Delay(_options.MaintenanceIntervalMs, stoppingToken);
        }
    }
}
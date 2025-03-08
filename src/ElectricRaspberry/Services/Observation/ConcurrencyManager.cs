using System.Collections.Concurrent;
using ElectricRaspberry.Services.Observation.Configuration;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Service for managing concurrency and shared state access
/// </summary>
public class ConcurrencyManager : IConcurrencyManager
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _resourceLocks = new();
    private readonly ConcurrentDictionary<string, DateTime> _resourceAccessTimes = new();
    private readonly ILogger<ConcurrencyManager> _logger;
    private readonly ConcurrencyOptions _options;
    
    /// <summary>
    /// Creates a new instance of the concurrency manager
    /// </summary>
    /// <param name="options">Concurrency options</param>
    /// <param name="logger">Logger</param>
    public ConcurrencyManager(
        IOptions<ConcurrencyOptions> options,
        ILogger<ConcurrencyManager> logger)
    {
        _logger = logger;
        _options = options.Value;
    }
    
    /// <summary>
    /// Acquires a lock for the specified resource
    /// </summary>
    /// <param name="resourceKey">The resource identifier</param>
    /// <param name="cancellationToken">A token to cancel the lock acquisition</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task AcquireResourceLockAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        var lockObject = GetOrCreateLock(resourceKey);
        
        try
        {
            // Wait to acquire the lock with timeout
            var lockAcquired = await lockObject.WaitAsync(_options.LockAcquisitionTimeoutMs, cancellationToken);
            
            if (!lockAcquired)
            {
                throw new TimeoutException($"Timeout waiting for resource lock: {resourceKey}");
            }
            
            // Record access time
            _resourceAccessTimes[resourceKey] = DateTime.UtcNow;
            
            _logger.LogDebug("Acquired lock for resource {ResourceKey}", resourceKey);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Lock acquisition for resource {ResourceKey} was canceled", resourceKey);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error acquiring lock for resource {ResourceKey}", resourceKey);
            throw;
        }
    }
    
    /// <summary>
    /// Releases a lock for the specified resource
    /// </summary>
    /// <param name="resourceKey">The resource identifier</param>
    public void ReleaseResourceLock(string resourceKey)
    {
        if (_resourceLocks.TryGetValue(resourceKey, out var lockObject))
        {
            try
            {
                lockObject.Release();
                _logger.LogDebug("Released lock for resource {ResourceKey}", resourceKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error releasing lock for resource {ResourceKey}", resourceKey);
            }
        }
    }
    
    /// <summary>
    /// Executes an action with a lock on the specified resource
    /// </summary>
    /// <typeparam name="T">The return type of the action</typeparam>
    /// <param name="resourceKey">The resource identifier</param>
    /// <param name="action">The action to execute</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The result of the action</returns>
    public async Task<T> ExecuteWithResourceLockAsync<T>(string resourceKey, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        await AcquireResourceLockAsync(resourceKey, cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            ReleaseResourceLock(resourceKey);
        }
    }
    
    /// <summary>
    /// Executes an action with a lock on the specified resource
    /// </summary>
    /// <param name="resourceKey">The resource identifier</param>
    /// <param name="action">The action to execute</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ExecuteWithResourceLockAsync(string resourceKey, Func<Task> action, CancellationToken cancellationToken = default)
    {
        await AcquireResourceLockAsync(resourceKey, cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            ReleaseResourceLock(resourceKey);
        }
    }
    
    /// <summary>
    /// Cleans up locks that haven't been accessed for a while
    /// </summary>
    public void CleanupStaleResources()
    {
        var staleThreshold = DateTime.UtcNow.AddMilliseconds(-_options.ResourceTimeoutMs);
        
        foreach (var resourceKey in _resourceAccessTimes.Keys)
        {
            if (_resourceAccessTimes.TryGetValue(resourceKey, out var lastAccess) && lastAccess < staleThreshold)
            {
                // Try to remove the resource if it's not being used
                if (_resourceLocks.TryGetValue(resourceKey, out var lockObject) && lockObject.CurrentCount == 1)
                {
                    // Only one permit available means the lock is not being held
                    if (_resourceLocks.TryRemove(resourceKey, out _))
                    {
                        _resourceAccessTimes.TryRemove(resourceKey, out _);
                        _logger.LogInformation("Removed stale resource lock for {ResourceKey}", resourceKey);
                    }
                }
            }
        }
    }
    
    private SemaphoreSlim GetOrCreateLock(string resourceKey)
    {
        return _resourceLocks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));
    }
}
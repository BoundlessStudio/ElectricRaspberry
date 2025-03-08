namespace ElectricRaspberry.Services.Observation;

/// <summary>
/// Interface for managing concurrency and shared state access
/// </summary>
public interface IConcurrencyManager
{
    /// <summary>
    /// Acquires a lock for the specified resource
    /// </summary>
    /// <param name="resourceKey">The resource identifier</param>
    /// <param name="cancellationToken">A token to cancel the lock acquisition</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task AcquireResourceLockAsync(string resourceKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Releases a lock for the specified resource
    /// </summary>
    /// <param name="resourceKey">The resource identifier</param>
    void ReleaseResourceLock(string resourceKey);
    
    /// <summary>
    /// Executes an action with a lock on the specified resource
    /// </summary>
    /// <typeparam name="T">The return type of the action</typeparam>
    /// <param name="resourceKey">The resource identifier</param>
    /// <param name="action">The action to execute</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The result of the action</returns>
    Task<T> ExecuteWithResourceLockAsync<T>(string resourceKey, Func<Task<T>> action, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes an action with a lock on the specified resource
    /// </summary>
    /// <param name="resourceKey">The resource identifier</param>
    /// <param name="action">The action to execute</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteWithResourceLockAsync(string resourceKey, Func<Task> action, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up locks that haven't been accessed for a while
    /// </summary>
    void CleanupStaleResources();
}
using Microsoft.AspNetCore.SignalR;

public class SignalRLogger : ILogger
{
  private readonly IHubContext<ClientHub> hub;
  private readonly string connectionId;

  public SignalRLogger(IHubContext<ClientHub> context, string connectionId)
  {
    this.hub = context;
    this.connectionId = connectionId;
  }

  public IDisposable BeginScope<TState>(TState state)
  {
    return null;
  }

  public bool IsEnabled(LogLevel logLevel)
  {
    return logLevel == LogLevel.Information;
  }

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
  {
    if (!IsEnabled(logLevel)) return;
    string message = formatter(state, exception) + Environment.NewLine;
    this.hub.Clients.Client(this.connectionId).SendAsync("Log", message);
  }
}

public class SignalRLoggerFactory : ILoggerFactory
{
  private readonly IHubContext<ClientHub> hub;
  private readonly string connectionId;

  public SignalRLoggerFactory(IHubContext<ClientHub> context, string connectionId)
  {
    this.hub = context;
    this.connectionId = connectionId;
  }

  public void AddProvider(ILoggerProvider provider)
  {
    // Not implemented
  }

  public ILogger CreateLogger(string categoryName)
  {
    return new SignalRLogger(this.hub, this.connectionId);
  }

  public void Dispose()
  {
    // Cleanup if necessary
  }
}
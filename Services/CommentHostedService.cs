using Microsoft.Extensions.Options;

public sealed class CommentHostedService : BackgroundService
{
  private readonly ICommentTaskQueue queue;
  private readonly ILogger<CommentHostedService> logger;
  private readonly IServiceProvider serviceProvider;
  private Dictionary<string, Type> agents;

  public CommentHostedService(ICommentTaskQueue queue, ILogger<CommentHostedService> logger, IServiceProvider serviceProvider)
  {
    this.queue = queue;
    this.logger = logger;
    this.serviceProvider = serviceProvider;
    this.agents = new Dictionary<string, Type>()
    {
      {"@george", typeof(GeorgeAgent)},
      {"@charles", typeof(CharlesAgent)},
      {"@tesla", typeof(TeslaAgent)},
      {"@dalle", typeof(DalleAgent)},
      {"@jeeves", typeof(JeeveAgent)},
      {"@alex", typeof(AlexAgent)},
      {"@shuri", typeof(ShuriAgent)}
    };
  }

  protected override async Task ExecuteAsync(CancellationToken ct)
  {
    while (ct.IsCancellationRequested == false)
    {
      try
      {
        var (user, comment) = await queue.DequeueAsync(ct);
        var scope = this.serviceProvider.CreateScope();
        var invoked = this.agents.Where(a => comment.Body.Contains(a.Key, StringComparison.OrdinalIgnoreCase));
        if(invoked.Any())
        {
          foreach (var item in invoked)
          {
            var agent = scope.ServiceProvider.GetService(item.Value) as IAgent ?? throw new InvalidOperationException($"Could not get {item.Value.Name} from the scoped service provider for {item.Key}");
            await agent.InvokeAsync(user, comment, ct);
          }
        } 
        else if(!comment.Author.IsBot)
        {
          var george = scope.ServiceProvider.GetService<GeorgeAgent>() ?? throw new InvalidOperationException($"Could not get GeorgeAgent from the scoped service provider.");
          await george.InvokeAsync(user, comment, ct);
        }
      }
      catch (OperationCanceledException) {} // Prevent throwing if stoppingToken was signaled
      catch (Exception ex)
      {
        logger.LogError(ex, "Error occurred executing task work item.");
      }
    }
  }
}
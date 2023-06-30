using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextCompletion;
using Microsoft.SemanticKernel.SkillDefinition;

// Move to Background Service
// https://learn.microsoft.com/en-us/dotnet/core/extensions/queue-service?pivots=dotnet-7-0

public sealed class CommentHostedService : BackgroundService
{
  private readonly ICommentTaskQueue queue;
  private readonly ILogger<CommentHostedService> logger;
  //private readonly IKernel myKernel;
  // private readonly ISKFunction skill;
  private readonly IAgent george;
  private readonly IAgent dalle;

  public CommentHostedService(ICommentTaskQueue queue, IOptions<OpenAIOptions> options, ILogger<CommentHostedService> logger, GeorgeAgent george, DalleAgent dalle)
  {
    this.queue = queue;
    this.logger = logger;
    this.george = george;
    this.dalle = dalle;

    // this.myKernel = Kernel.Builder
    //   .WithLogger(logger)
    //   .WithAIService<ITextCompletion>("TextCompletion", new OpenAITextCompletion("text-davinci-003", options.Value.ApiKey, options.Value.OrgId)) // OpenAITextCompletion/OpenAIChatCompletion | gpt-3.5-turbo/gpt-3.5-turbo-16k/text-davinci-003
    //   .Build();

    //var prompt = @"Please take the following text and transform it into a compressed JSON array of objects.Each object should have properties for 'user' and 'intent'. If no user is mentioned, the 'user' property should be 'unknown'. Please ensure the JSON output is in a minified format. The text for transformation is: {{$INPUT}}";
    //this.skill = this.myKernel.CreateSemanticFunction(prompt, maxTokens: 2000);
  }

  protected override async Task ExecuteAsync(CancellationToken ct)
  {
    while (ct.IsCancellationRequested == false)
    {
      try
      {
        var (user, model) = await queue.DequeueAsync(ct);
        if(model.Body.Contains("@george"))
        {
          await this.george.InvokeAsync(user, model, ct);
        }
        if(model.Body.Contains("@dalle"))
        {
          await this.dalle.InvokeAsync(user, model, ct);
        }

        // var ctx = await skill.InvokeAsync(input: model.Body, cancellationToken: ct);
        // var mentions = JsonSerializer.Deserialize<List<MentionModel>>(ctx.Result) ?? new List<MentionModel>();
        // foreach (var mention in mentions)
        // {
        //   switch (mention.User)
        //   {
        //     case "george":
        //       await this.george.InvokeAsync(user, model, mention, ct);
        //       break;
        //     case "unknown":
        //       break;
        //     default: 
        //       // Suggest 3 Follow Up Prompts
        //       break;
        //   }
        // }
      }
      catch (OperationCanceledException) {} // Prevent throwing if stoppingToken was signaled
      catch (Exception ex)
      {
        logger.LogError(ex, "Error occurred executing task work item.");
      }
    }
  }
}
using System.Text;
using Auth0.ManagementApi;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Stepwise;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors;
using Microsoft.SemanticKernel.Skills.OpenAPI.Extensions;
using Microsoft.SemanticKernel.Skills.Web;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

public sealed class CommentLogger : ILogger
{
  private readonly StringBuilder builder;
  private readonly FeedsContext context; 
  private readonly IHubContext<FeedHub> hub;
  private readonly CommentRecord comment;

  public CommentLogger(FeedsContext context, IHubContext<FeedHub> hub, CommentRecord comment)
  {
    this.builder = new StringBuilder();
    this.context = context;
    this.hub = hub;
    this.comment = comment;
  }

  public IDisposable? BeginScope<TState>(TState state) where TState : notnull
  {
      return null;
  }

  public bool IsEnabled(LogLevel logLevel)
  {
    return logLevel == LogLevel.Information;
  }

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    if(logLevel == LogLevel.Information)
    {
      this.builder.AppendLine(formatter(state, exception));
      this.builder.AppendLine();
      this.comment.Body = this.builder.ToString();
      //var body = this.builder.ToString();
      //this.comment.Body = $"```shell {Environment.NewLine}{body}{Environment.NewLine}```";
      this.context.SaveChanges();
      this.hub.Clients.Group(this.comment.FeedId).SendAsync("onCommentsChanged", this.comment.FeedId).Wait();
    }
  }
}

public class TeslaAgent : IAgent
{
  // public const string  Id = "946840c094964c79819f102453824757";

  private readonly IOptions<OpenAIOptions> aiOptions;
  private readonly IOptions<BingOptions> bingOptions;
  private readonly IHubContext<FeedHub> hub;
  private readonly IMemoryStore memory;
  private readonly FeedsContext context;
  private readonly ISecurityService securityService;

   public TeslaAgent(
    IOptions<OpenAIOptions> aiOptions,
    IOptions<BingOptions> bingOptions,
    IHubContext<FeedHub> hub,
    IMemoryStore memory,
    FeedsContext context,
    ISecurityService securityService
  )
  {
    this.aiOptions = aiOptions;
    this.bingOptions = bingOptions;
    this.context = context;
    this.hub = hub;
    this.memory = memory;
    this.context = context;
    this.securityService = securityService;
  }

  public async Task InvokeAsync(IAuthorizedUser user, CommentRecord comment, CancellationToken ct)
  {
    var placeholder = await AddComment(comment.FeedId, "planing", string.Empty, ct);
    var goal = await GetGoal(comment, ct);
    await RunPlaner(user, placeholder, goal, ct);
  }

  private Task<string> GetGoal(CommentRecord comment, CancellationToken ct) 
  {
    return Task.FromResult(comment.Body);
  }


  private async Task RunPlaner(IAuthorizedUser user, CommentRecord placeholder, string goal, CancellationToken ct) 
  {
    await UpdateResponse(placeholder, "planing", "Creating Kernel", ct);

    var logger = new CommentLogger(this.context, this.hub, placeholder);

    var myKernel = Kernel.Builder
      .WithLogger(logger)
      .WithMemoryStorage(this.memory)
      .WithAIService<ITextEmbeddingGeneration>("TextEmbedding", new OpenAITextEmbeddingGeneration("text-embedding-ada-002", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
      .WithAIService<IChatCompletion>("ChatCompletion", new OpenAIChatCompletion("gpt-4", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
      .WithAIService<ITextCompletion>("TextCompletion", new OpenAIChatCompletion("gpt-3.5-turbo-16k", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
      .Build();

    await UpdateResponse(placeholder, "planing", "Importing Skills", ct);

    var feed = await this.context.Feeds.Include(f => f.Skills).FirstOrDefaultAsync(f => f.FeedId == placeholder.FeedId, ct)  
      ?? throw new NullReferenceException($"TeslaAgent RunPlaner() => Feed {placeholder.FeedId} not found");

    var result = await securityService.GetAuthenticatedUser(user);
    if(result.TryPickT3(out var info, out var remainder) == false) return;
    var graphClient = await securityService.GetGraphClient(info);

    var skills = new Dictionary<string, object>()
    {
      { nameof(TimeSkill), new TimeSkill() },
      { nameof(LanguageCalculatorSkill), new LanguageCalculatorSkill(myKernel) },
      { nameof(TextMemorySkill), new TextMemorySkill() },
      { nameof(WaitSkill), new WaitSkill() },
      { nameof(WebSearchEngineSkill), new WebSearchEngineSkill(new BingConnector(this.bingOptions.Value.ApiKey)) },
      { nameof(CalendarSkill), new CloudDriveSkill(new OneDriveConnector(graphClient)) },
      { nameof(CloudDriveSkill), new CloudDriveSkill(new OneDriveConnector(graphClient)) },
      { nameof(EmailSkill), new EmailSkill(new OutlookMailConnector(graphClient)) },
      { nameof(TaskListSkill), new TaskListSkill(new MicrosoftToDoConnector(graphClient)) },
    };

    foreach (var skill in feed.Skills)
    {
      switch (skill.Type)
      {
        case SkillType.Coded when skill.TypeOf is not null:
          myKernel.ImportSkill(skills[skill.TypeOf], skill.TypeOf);
          break;
        case SkillType.Semantic when skill.Prompt is not null:
          var fn = myKernel.CreateSemanticFunction(skill.Prompt, new PromptTemplateConfig() { Description = skill.Description ?? string.Empty });
          myKernel.ImportSkill(fn, skill.TypeOf);
          break;
        case SkillType.OpenApi when skill.Url is not null && skill.TypeOf is not null:
          await myKernel.ImportOpenApiSkillFromUrlAsync(skill.TypeOf, new Uri(skill.Url), new OpenApiSkillExecutionParameters(), cancellationToken: ct);
          break;
        case SkillType.Unknown:
        default:
          break;
      }
    }

    await UpdateResponse(placeholder, "planing", "Creating Plan", ct);

    var config = new StepwisePlannerConfig
    {
      MinIterationTimeMs = 1000,
      MaxIterations = 32,
      MaxTokens = 4000,
    };
    StepwisePlanner planner = new(myKernel, config);
    var input = myKernel.CreateNewContext(ct);

    // var goal = "Who is the current president of the United States? What is his current age divided by 2?";
    var plan = planner.CreatePlan(goal);

    await UpdateResponse(placeholder, "planing", "Invoking Plan", ct);

    var output = await plan.InvokeAsync(input);

    if(output.Result.Contains("Result not found"))
      await AddComment(placeholder.FeedId, "comment", "I failed to reach a final conclusion. There can be a number of reasons why from bad planing to lack skill. Sometimes these errors are transient please try again.", ct);
    else  
      await AddComment(placeholder.FeedId, "comment", output.Result, ct);
  }
  private async Task<CommentRecord> AddComment(string feedId, string type, string body, CancellationToken ct) 
  {
    var model = new CommentRecord()
    {
      CommentId = Guid.NewGuid().ToString(),
      FeedId = feedId,
      Type = type,
      Body = body,
      Timestamp = DateTimeOffset.UtcNow,
      Author = new AuthorRecord()
      {
        Id = "946840c094964c79819f102453824757",
        IsBot = true,
        Name = "Tesla",
        Mention = "@tesla",
        Picture = "https://i.imgur.com/lW4OkVJ.png",
      },
    };
    context.Comments.Add(model);
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(feedId).SendAsync("onCommentsChanged", feedId, ct);
    return model;
  }
  
  private async Task UpdateResponse(CommentRecord model, string type, string body, CancellationToken ct)
  {
    model.Type = type;
    model.Body = body;
    model.Tokens = GPT3Tokenizer.Encode(body).Count;
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(model.FeedId!).SendAsync("onCommentsChanged", model.FeedId, ct);
  }
}
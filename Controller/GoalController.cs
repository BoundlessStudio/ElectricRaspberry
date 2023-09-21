using ElectricRaspberry.Services;
using ElectricRaspberry.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Stepwise;
using Microsoft.SemanticKernel.Skills.Web;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using System.Security.Claims;
using System.Text;

public class GoalController
{
  public async Task<string> Plan(
    ClaimsPrincipal principal,
    IOptions<OpenAIOptions> aiOptions,
    IOptions<BingOptions> bingOptions,
    IOptions<LeonardoOptions> leonardoOptions,
    IOptions<ConvertioOptions> convertioOptions,
    IOptions<BrowserlessOptions> browserlessOptions,
    IStorageService storageService,
    IHubContext<ClientHub> hub,
    IHttpClientFactory httpFactory,
    // ILoggerFactory logFactory,
    [FromBody]GoalDocument dto)
  {
    var user = principal.GetUser();

    var logFactory = new SignalRLoggerFactory(hub, dto.ConnectionId);

    var myKernel = Kernel.Builder
      .WithLoggerFactory(logFactory)
      //.WithAIService<ITextCompletion>("CodeCompletion", new OpenAITextCompletion("gpt-3.5-turbo-16k-0613", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
      .WithAIService<ITextEmbeddingGeneration>("TextEmbedding", new OpenAITextEmbeddingGeneration("text-embedding-ada-002", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
      .WithAIService<ITextCompletion>("TextCompletion", new OpenAIChatCompletion("gpt-4-0613", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
      .WithAIService<IChatCompletion>("ChatCompletion", new OpenAIChatCompletion("gpt-4-0613", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
      .Build();

    myKernel.ImportSkill(new JsCodingSkill(myKernel, hub, dto.ConnectionId), nameof(JsCodingSkill));
    myKernel.ImportSkill(new WebSearchEngineSkill(new BingConnector(bingOptions.Value.ApiKey)), nameof(WebSearchEngineSkill));
    myKernel.ImportSkill(new DrawImageSkill(user, leonardoOptions, storageService, httpFactory), nameof(DrawImageSkill));
    myKernel.ImportSkill(new CalendarSkill(user), nameof(CalendarSkill));
    // myKernel.ImportSkill(new ConverterSkill(myKernel, user, convertioOptions, storageService, httpFactory), nameof(ConverterSkill));
    myKernel.ImportSkill(new UserFeedbackSkill(hub, dto.ConnectionId), nameof(UserFeedbackSkill));
    // myKernel.ImportSkill(new PuppeteerSkill(user, browserlessOptions, storageService), nameof(PuppeteerSkill));

    var config = new StepwisePlannerConfig
    {
      MinIterationTimeMs = 1000,
      MaxIterations = 16,
      MaxTokens = 4000,
    };
    var instructions = new StringBuilder();
    instructions.AppendLine("You are a Stepwise Planner that uses Thought, Action, Observation steps to achieve goals using semantic function.");
    instructions.AppendLine($"You should plan carefully your goal as you only have {config.MaxIterations} steps");
    instructions.AppendLine("Your final message should include markdown to format the message with basic styling, images, and links.");
    instructions.AppendLine($"The user's name is {user.Name}.");

    var settings = new CompleteRequestSettings()
    {
      ChatSystemPrompt = instructions.ToString(),
      MaxTokens = config.MaxTokens,
      Temperature = 0.7,
      TopP = 1,
      ResultsPerPrompt = 1,
    };
    StepwisePlanner planner = new(myKernel, config);

    var plan = planner.CreatePlan(dto.Goal);
    var cxt = myKernel.CreateNewContext();
    var output = await plan.InvokeAsync(cxt, settings);
    return output.Result;

    //var cxt = myKernel.CreateNewContext();
    //var planner = new StepwisePlanner(myKernel);
    //await planner.ExecutePlanAsync(dto.Goal, cxt);
    //return cxt.Result;
  }
}

// Goal:
// "Who is the current president of the United States and what is their current age divided by 2?"

// More Skills?:
// [ ] OpenApiCustomSkill?
// [ ] EmailSkill?
// [ ] SqlSkill?

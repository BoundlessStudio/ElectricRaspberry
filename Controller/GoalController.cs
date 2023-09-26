using ElectricRaspberry.Services;
using ElectricRaspberry.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Stepwise;
using System.Security.Claims;
using System.Text;

public class GoalController
{
  public async Task<string> Plan(
    ClaimsPrincipal principal,
    //IOptions<OpenAIOptions> aiOptions,
    IOptions<AzureAIOptions> aiOptions,
    IOptions<BingOptions> bingOptions,
    IOptions<LeonardoOptions> leonardoOptions,
    IOptions<ConvertioOptions> convertioOptions,
    IOptions<BrowserlessOptions> browserlessOptions,
    IStorageService storageService,
    IHubContext<ClientHub> hub,
    IHttpClientFactory httpFactory,
    ILoggerFactory logFactory,
    [FromBody]GoalDocument dto)
  {
    var user = principal.GetUser();

    var myKernel = Kernel.Builder
      .WithLoggerFactory(logFactory)
      .WithAzureChatCompletionService(aiOptions.Value.DeploymentName, aiOptions.Value.Endpoint, aiOptions.Value.ApiKey, true)
      .WithRetryBasic()
      .Build();

    myKernel.ImportSkill(new JsCodingSkill(myKernel, hub, dto.ConnectionId), nameof(JsCodingSkill));
    myKernel.ImportSkill(new WebSearchSkill(new BingConnector(bingOptions.Value.ApiKey, httpFactory)), nameof(WebSearchSkill));
    myKernel.ImportSkill(new DrawImageSkill(user, leonardoOptions, storageService, httpFactory), nameof(DrawImageSkill));
    myKernel.ImportSkill(new CalendarSkill(user), nameof(CalendarSkill));
    myKernel.ImportSkill(new UserFeedbackSkill(hub, dto.ConnectionId), nameof(UserFeedbackSkill));
    // myKernel.ImportSkill(new ConverterSkill(myKernel, user, convertioOptions, storageService, httpFactory), nameof(ConverterSkill));
    // myKernel.ImportSkill(new PuppeteerSkill(user, browserlessOptions, storageService), nameof(PuppeteerSkill));

    var config = new StepwisePlannerConfig
    {
      MinIterationTimeMs = 100,
      MaxIterations = 16,
    };
    var instructions = new StringBuilder();
    instructions.AppendLine($"You are a Stepwise Planner that uses Thought, Action, Observation steps to achieve goals using semantic function. You only have {config.MaxIterations} steps. Your final message should formated via markdown.");

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

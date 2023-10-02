using ElectricRaspberry.Services;
using ElectricRaspberry.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Stepwise;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Security.Claims;

public class GoalController
{
  public async Task<MessageDocument> Plan(
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
      .WithAzureTextEmbeddingGenerationService(aiOptions.Value.DeploymentName, aiOptions.Value.Endpoint, aiOptions.Value.ApiKey)
      .WithAzureChatCompletionService(aiOptions.Value.DeploymentName, aiOptions.Value.Endpoint, aiOptions.Value.ApiKey, true)
      .WithRetryBasic()
      .Build();

    myKernel.ImportSkill(new JsCodingSkill(myKernel, hub, dto.ConnectionId), nameof(JsCodingSkill));
    myKernel.ImportSkill(new WebSearchSkill(new BingConnector(bingOptions.Value.ApiKey, httpFactory)), nameof(WebSearchSkill));
    myKernel.ImportSkill(new DrawImageSkill(user, leonardoOptions, storageService, httpFactory), nameof(DrawImageSkill));
    myKernel.ImportSkill(new UserFeedbackSkill(hub, dto.ConnectionId), nameof(UserFeedbackSkill));
    //myKernel.ImportSkill(new ConverterSkill(myKernel, user, convertioOptions, storageService, httpFactory), nameof(ConverterSkill));
    //myKernel.ImportSkill(new PuppeteerSkill(user, browserlessOptions, storageService), nameof(PuppeteerSkill));

    var planner = new StepwisePlanner(myKernel).WithInstrumentation(logFactory);

    var plan = planner.CreatePlan(dto.Goal);
    var cxt = myKernel.CreateNewContext();
    var output = await plan.InvokeAsync(cxt);
    var steps = output.Variables["stepsTaken"];
    var logs = JsonConvert.DeserializeObject<List<StepDocument>>(steps) ?? new List<StepDocument>();
    var filtered = logs.Where(l => !l.IsEmpty()).ToList();

    var msg = new MessageDocument()
    {
      Role = Role.Assistant,
      Content = output.Result,
      Logs = filtered
    };
    return msg;
  }
}
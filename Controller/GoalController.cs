using ElectricRaspberry.Services;
using ElectricRaspberry.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Stepwise;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.Skills.Web;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;

public class GoalController
{
  public async Task<MessageDocument> Plan(
    ClaimsPrincipal principal,
    //IOptions<OpenAIOptions> openAiOptions,
    IOptions<AzureAIOptions> azureAiOptions,
    IOptions<BingOptions> bingOptions,
    IOptions<LeonardoOptions> leonardoOptions,
    IOptions<BrowserlessOptions> browserlessOptions,
    IOptions<PythonInterpreterOptions> pyOptions,
    //IOptions<ConvertioOptions> convertioOptions,
    // IHubContext<ClientHub> hub,
    IStorageService storageService,
    IHttpClientFactory httpFactory,
    ILoggerFactory logFactory,
    IMemoryStore store,
    [FromBody]GoalDocument dto)
  {
    var user = principal.GetUser();

    var myKernel = Kernel.Builder
      .WithLoggerFactory(logFactory)
      .WithAzureTextEmbeddingGenerationService("text-embedding-ada-002", azureAiOptions.Value.Endpoint, azureAiOptions.Value.ApiKey)
      .WithAzureChatCompletionService("gpt-4", azureAiOptions.Value.Endpoint, azureAiOptions.Value.ApiKey, true, "gpt4-8k", true)
      .WithAzureChatCompletionService("gpt-4-32k", azureAiOptions.Value.Endpoint, azureAiOptions.Value.ApiKey, false, "gpt4-32k")
      //.WithOpenAITextEmbeddingGenerationService("text-embedding-ada-002", openAiOptions.Value.ApiKey, openAiOptions.Value.OrgId)
      //.WithOpenAIChatCompletionService("gpt-4", openAiOptions.Value.ApiKey, openAiOptions.Value.OrgId, "gpt4-8k", true, true)
      //.WithOpenAIChatCompletionService("gpt-4-32k", openAiOptions.Value.ApiKey, openAiOptions.Value.OrgId, serviceId: "gpt4-32k")
      .WithMemoryStorage(store)
      .WithRetryBasic()
      .Build();

    //myKernel.ImportSkill(new TextMemorySkill(myKernel.Memory), "SemanticTextMemory");
    myKernel.ImportSkill(new PythonInterpreterSkill(pyOptions, user, storageService, httpFactory), "PythonInterpreterSkill");
    myKernel.ImportSkill(new WebSearchEngineSkill(new BingConnector(bingOptions.Value.ApiKey, logFactory)), "WebSearchEngineSkill");
    myKernel.ImportSkill(new DrawImageSkill(user, leonardoOptions, storageService, httpFactory), "DrawImageSkill");

    var instructions = new StringBuilder();
    instructions.AppendLine("[USER]");
    instructions.AppendLine($"Name: {user.Name}");
    instructions.AppendLine($"Region: {user.City}, {user.Country}");
    instructions.AppendLine($"Position: ({user.Latitude},{user.Longitude})");
    instructions.AppendLine($"Timezone: {user.Timezone}");
    instructions.AppendLine("[ADDITIONAL INSTRUCTIONS]");
    instructions.AppendLine("The FINAL ANSWER is shown to the user it should be formated with markdown for links, images, tables and script tags.");

    // instructions.AppendLine("You have MEMORY use it to save and recall anything by collection and key. Make sure to include any collection/key pairs in the FINAL ANSWER.");

    //var history = dto.History.TakeLast(10).ToList();
    //if (history.Count > 0)
    //{
    //  instructions.AppendLine("[CHAT]");
    //  foreach (var item in history)
    //  {
    //    switch (item.Role)
    //    {
    //      case Role.Assistant:
    //        instructions.AppendLine("Assistant: " + item.Content);
    //        break;
    //      case Role.User:
    //        instructions.AppendLine("User: " + item.Content);
    //        break;
    //      default:
    //        break;
    //    }
    //  }
    //}

    var config = new StepwisePlannerConfig()
    {
      Suffix = instructions.ToString()
    };

    var planner = new StepwisePlanner(myKernel, config).WithInstrumentation(logFactory);

    var goal = new StringBuilder();
    goal.AppendLine( dto.Goal);
    
    var plan = planner.CreatePlan(goal.ToString());
    var cxt = myKernel.CreateNewContext();
    var output = await plan.InvokeAsync(cxt);

    var json = output.Variables["stepsTaken"];
    var steps = JsonConvert.DeserializeObject<List<StepDocument>>(json) ?? new List<StepDocument>();
    var logs = steps.Where(l => !l.IsEmpty()).ToList();

    var msg = new MessageDocument()
    {
      Role = Role.Assistant,
      Content = output.Result,
      Logs = logs
    };
    return msg;
  }
}

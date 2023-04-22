using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors.Client;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Planners;

// Load configuration
IConfigurationRoot configuration = new ConfigurationBuilder()
  .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
  .AddJsonFile(path: "appsettings.Development.json", optional: true, reloadOnChange: true)
  .AddEnvironmentVariables()
  .AddUserSecrets<Program>()
  .Build();

// Initialize logger
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
  builder.AddConfiguration(configuration.GetSection("Logging"))
    .AddConsole()
    .AddDebug();
});

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

MsGraphConfiguration graphApiConfiguration = configuration.GetRequiredSection("MsGraph").Get<MsGraphConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Microsoft Graph API.");

var appAuth = PublicClientApplicationBuilder.Create(graphApiConfiguration.ClientId)
  .WithRedirectUri(graphApiConfiguration.RedirectUri.ToString())
  .WithAuthority(AzureCloudInstance.AzurePublic, graphApiConfiguration.TenantId)
  .Build();

// Add authentication handler.
IList<DelegatingHandler> handlers = GraphClientFactory.CreateDefaultHandlers(new DelegateAuthenticationProvider(async (req) =>
{
  var scopes = graphApiConfiguration.Scopes.ToArray();
  var authResult = await appAuth.AcquireTokenInteractive(scopes).ExecuteAsync();
  req.Headers.Authorization = new AuthenticationHeaderValue(scheme: "bearer", parameter: authResult.AccessToken);
}));

// Add logging handler to log Graph API requests and responses request IDs.
using MsGraphClientLoggingHandler loggingHandler = new(logger);
handlers.Add(loggingHandler);

// Create the Graph client.
using HttpClient httpClient = GraphClientFactory.Create(handlers);
GraphServiceClient graphServiceClient = new(httpClient);

OpenAIConfiguration openAIConfiguration = configuration.GetRequiredSection("OpenAI").Get<OpenAIConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Open AI.");

var config = new KernelConfig()
  .AddOpenAIChatCompletionService("GPT-4", "gpt-4-0314", openAIConfiguration.ApiKey, openAIConfiguration.OrgId)
  .AddOpenAITextCompletionService("GPT-3.5", "gpt-3.5-turbo", openAIConfiguration.ApiKey, openAIConfiguration.OrgId)
  .AddOpenAITextEmbeddingGenerationService("Embeddings", "text-embedding-ada-002", openAIConfiguration.ApiKey, openAIConfiguration.OrgId);

IKernel myKernel = Kernel.Builder
  .WithMemoryStorage(new VolatileMemoryStore())
  .WithConfiguration(config)
  .Build();

myKernel.RegisterSemanticSkills("skills");
myKernel.RegisterNativeSkills();
myKernel.RegisterNativeGraphSkills(graphServiceClient);
myKernel.RegisterTextMemory();

var goal = "Create a slogan for the BBQ Pit in London that specializes in Mustard Sauce then email it to jamie_maxwell_webster@hotmail.com with the subject 'New Marketing Slogan'";
var planner = new SequentialPlanner(myKernel);
var plan = await planner.CreatePlanAsync(goal);

var id = DateTime.UtcNow.ToFileTime();
await System.IO.File.AppendAllTextAsync($"plans/plan-{id}.json", plan.ToJson());

var ctx = new ContextVariables();
while(plan.HasNextStep)
  plan = await plan.RunNextStepAsync(myKernel, ctx);

Console.WriteLine("Plan Complete");

record OpenAIConfiguration(string ApiKey, string OrgId);
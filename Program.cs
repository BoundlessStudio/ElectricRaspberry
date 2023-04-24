using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors.Client;
using Microsoft.SemanticKernel.Planning.Planners;
using Microsoft.SemanticKernel.Orchestration;

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

var graphApiConfiguration = configuration.GetRequiredSection("MsGraph").Get<MsGraphConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Microsoft Graph API.");
var openAIConfiguration = configuration.GetRequiredSection("OpenAI").Get<OpenAIConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Open AI.");
var bingConfiguration = configuration.GetRequiredSection("Bing").Get<BingConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Bing.");

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
using var loggingHandler = new MsGraphClientLoggingHandler(logger);
handlers.Add(loggingHandler);

// Create the Graph client.
using HttpClient httpClient = GraphClientFactory.Create(handlers);
var graphServiceClient = new GraphServiceClient(httpClient);

var config = new KernelConfig()
  .AddOpenAIChatCompletionService("GPT-4", "gpt-4-0314", openAIConfiguration.ApiKey, openAIConfiguration.OrgId)
  .AddOpenAITextCompletionService("GPT-3.5", "gpt-3.5-turbo", openAIConfiguration.ApiKey, openAIConfiguration.OrgId)
  .AddOpenAITextEmbeddingGenerationService("Embeddings", "text-embedding-ada-002", openAIConfiguration.ApiKey, openAIConfiguration.OrgId);

IMemoryStore memory = new VolatileMemoryStore(); // Replace this with a more interesting one later

IKernel myKernel = Kernel.Builder
  .WithMemoryStorage(memory)
  .WithConfiguration(config)
  .Build();

myKernel.RegisterTextMemory();
myKernel.RegisterSemanticSkills("semantics");
myKernel.RegisterSystemSkills();
myKernel.RegisterFilesSkills();
myKernel.RegisterOfficeSkills();
myKernel.RegisterWebSkills(bingConfiguration.ApiKey);
myKernel.RegisterMicrosoftServiceSkills(graphServiceClient);
// myKernel.RegisterGoogleServices();
//await myKernel.ImportChatGptPluginSkillFromUrlAsync("wolframalpha", new Uri("https://www.wolframalpha.com/.well-known/apispec.json"));
//await myKernel.ImportChatGptPluginSkillFromUrlAsync("zapier", new Uri("https://zapier-deployment.com/.well-known/ai-plugin.jsonn"));

// var goal = "write a short story about a boy and this dog into a txt file named novel in the output directory";
// var goal = "Get the current date then add 4 days to it.";
// var goal = "Get the current time then add 4 days to it.";
var goal = "Get {{time.today}} then add 5 days to it";
var planner = new SequentialPlanner(myKernel);
var plan = await planner.CreatePlanAsync(goal);
var result = await plan.InvokeAsync();

System.IO.File.AppendAllText($"plans/{DateTime.Now.ToFileTime()}.json", plan.ToJson().PrettyJson());

Console.WriteLine("Plan Complete");
Console.WriteLine("Result:" + result);
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors.Client;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Skills.OpenAPI.Authentication;

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
    builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole().AddDebug();
  });

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

var graphApiConfiguration = configuration.GetRequiredSection("MsGraph").Get<MsGraphConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Microsoft Graph API.");
var openAIConfiguration = configuration.GetRequiredSection("OpenAI").Get<OpenAIConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Open AI.");
var bingConfiguration = configuration.GetRequiredSection("Bing").Get<BingConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Bing.");

var graphServiceClient = await MSALHelper.CreateGraphServiceClientAsync(graphApiConfiguration, logger);

var kernelConfig = new KernelConfig()
  .AddOpenAIChatCompletionService("GPT-4", "gpt-4", openAIConfiguration.ApiKey, openAIConfiguration.OrgId)
  .AddOpenAITextCompletionService("GPT-3.5", "gpt-3.5-turbo", openAIConfiguration.ApiKey, openAIConfiguration.OrgId)
  .AddOpenAITextEmbeddingGenerationService("Embeddings", "text-embedding-ada-002", openAIConfiguration.ApiKey, openAIConfiguration.OrgId);

var memoryStore = new VolatileMemoryStore(); 
// var memoryStore = new Microsoft.SemanticKernel.Connectors.Memory.Qdrant.QdrantMemoryStore("http://localhost", 6333, 1536);

IKernel myKernel = Kernel.Builder
  .WithConfiguration(kernelConfig)
  .WithLogger(logger)
  .WithMemoryStorage(memoryStore)
  .Build();

//@Monkey TODO: Test These Skills: Memory, Text, and MicrosoftService (Task/Calendar)
//@rainbow-pineapple TODO: Interact on ImportChatGptPluginSkillFromUrlAsync function with unit tests using https://www.wolframcloud.com/.well-known/ai-plugin.json
myKernel.RegisterMemorySkills();
myKernel.RegisterSemanticSkills();
myKernel.RegisterSystemSkills();
myKernel.RegisterFilesSkills();
myKernel.RegisterOfficeSkills();
myKernel.RegisterWebSkills(bingConfiguration.ApiKey);
myKernel.RegisterMicrosoftServiceSkills(graphServiceClient);
//@RGBKnights TODO: Test ChatGptPluginSkill
await myKernel.ImportChatGptPluginSkillFromUrlAsync("Klarna", new Uri("https://www.klarna.com/.well-known/ai-plugin.json"));
//@RGBKnights TODO: Test OpenApiSkill 
await myKernel.ImportOpenApiSkillFromUrlAsync("Scryfall", new Uri("https://raw.githubusercontent.com/smgoller/scryfall-openapi/master/openapi.yml"));

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) => {
  e.Cancel = true;
  cts.Cancel();
};

Microsoft.SemanticKernel.Planning.Sequential.SequentialPlannerConfig config = new ();
config.ExcludedSkills.Add("WebFileDownloadSkill");
var planner = new SequentialPlanner(myKernel, config);

var goal = @"get the Weather for Toronto then write the results into a file in the 'output/files' directory.";
var plan = await planner.CreatePlanAsync(goal);
await plan.WriteAsync();
Console.WriteLine("Plan Created");
var cxt = myKernel.CreateNewContext();
Console.WriteLine("Plan Invoked");
await plan.InvokeAsync(context: cxt, cancel: cts.Token);
await plan.WriteAsync();
Console.WriteLine("Plan Complete");

foreach (var variable in plan.State)
{
  var p = Path.Combine("output", "state", $"{variable.Key.ToLower()}.md");
  await File.WriteAllTextAsync(p, variable.Value);
}
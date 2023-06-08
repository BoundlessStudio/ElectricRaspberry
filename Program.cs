using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors.Client;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Skills.OpenAPI.Authentication;
using Microsoft.SemanticKernel.Orchestration;
using System.Diagnostics;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextCompletion;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

long timestamp = 0L;

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
var githubConfiguration = configuration.GetRequiredSection("Github").Get<GithubConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Github.");

var graphServiceClient = await MSALHelper.CreateGraphServiceClientAsync(graphApiConfiguration, logger);

var client = new HttpClient();

  //var kernelConfig = new KernelConfig();
  //.AddChatCompletionService((k) => new OpenAIChatCompletionService(k, s, openAIConfiguration.ApiKey, openAIConfiguration.OrgId, false, httpClient: client));
  // .ChatCompletionServices("gpt-4", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, false, httpClient: client)
  // .TextCompletionServices("text-davinci-003", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, httpClient: client)
  // .AddTextEmbeddingGenerationService("text-embedding-ada-002", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, httpClient: client);
  //.AddOpenAIChatCompletionService("gpt-4", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, false, httpClient: client)
  //.AddOpenAITextCompletionService("text-davinci-003", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, httpClient: client)
  //.AddOpenAITextEmbeddingGenerationService("text-embedding-ada-002", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, httpClient: client);

var memoryStore = new VolatileMemoryStore();
// var memoryStore = new Microsoft.SemanticKernel.Connectors.Memory.Qdrant.QdrantMemoryStore("https://acad301e-0b51-4ac6-b51d-280fb1f57822.us-east-1-0.aws.cloud.qdrant.io", 6333, 1536);

timestamp = Stopwatch.GetTimestamp();

IKernel myKernel = Kernel.Builder
  .WithAIService<IChatCompletion>("ChatCompletion", new OpenAIChatCompletion("gpt-4", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, httpClient: client))
  .WithAIService<ITextCompletion>("TextCompletion", new OpenAITextCompletion("text-davinci-003", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, httpClient: client))
  .WithAIService<ITextEmbeddingGeneration>("TextEmbedding", new OpenAITextEmbeddingGeneration("text-embedding-ada-002", openAIConfiguration.ApiKey, openAIConfiguration.OrgId, httpClient: client))
  .WithLogger(logger)
  .WithMemoryStorage(memoryStore)
  .Build();

myKernel.RegisterMemorySkills();
myKernel.RegisterSemanticSkills();
//myKernel.RegisterSystemSkills();
//myKernel.RegisterFilesSkills();
//myKernel.RegisterOfficeSkills();
//myKernel.RegisterWebSkills(bingConfiguration.ApiKey);
myKernel.RegisterMicrosoftServiceSkills(graphServiceClient);

Console.WriteLine("Keneral Build Time: " + Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
timestamp = Stopwatch.GetTimestamp();

var config = new Microsoft.SemanticKernel.Planning.Sequential.SequentialPlannerConfig();
config.ExcludedFunctions.Add("Retrieve");
var planner = new SequentialPlanner(myKernel, config);

await myKernel.Memory.SaveInformationAsync("contacts", "jamie_maxwell_webster@hotmail.com", "1");
await myKernel.Memory.SaveInformationAsync("contacts", "master@rgbknights.com", "2");
await myKernel.Memory.SaveInformationAsync("contacts", "admin@highgroundvision.com", "3");

Console.WriteLine("Keneral Memory Time: " + Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
timestamp = Stopwatch.GetTimestamp();

//var goal = @"Create a slogan for the BBQ Pit in London that specializes in Mustard Sauce then search your contacts memory for jamie then email the slogan to them with the subject 'New Marketing Slogan'";
var goal = "Create a slogan for the BBQ Pit in London that specializes in Mustard Sauce then email it to jamie_maxwell_webster@hotmail.com with the subject 'New Marketing Slogan'"; //
var plan = await planner.CreatePlanAsync(goal);
plan.Name = "Sequential Plan Test 1";

Console.WriteLine("Created Plan Time: " + Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);

await plan.WriteAsync();
var cxt = myKernel.CreateNewContext();

timestamp = Stopwatch.GetTimestamp();

await plan.InvokeAsync(context: cxt);

Console.WriteLine("Invoked Plan Time: " + Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);

await plan.WriteAsync();

foreach (var variable in plan.State)
{
  var p = Path.Combine("output", "state", $"{variable.Key.ToLower()}.md");
  await File.WriteAllTextAsync(p, variable.Value);
}

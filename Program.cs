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



// var skill = myKernel.Func("TestSkillFlex", "SloganMakerFlex");
// var context = new ContextVariables(); 
// context.Set("BUSINESS", "Wiped Balls"); 
// context.Set("CITY", "Seattle"); 
// context.Set("SPECIALTY", "basketball cleaning"); 
// var slogan = await myKernel.RunAsync(context, skillSloganMaker);

// var skillEmail = myKernel.Func(nameof(EmailSkill), "SendEmailAsync");
// ContextVariables emailMemory = new ContextVariables($"New Slogan{Environment.NewLine}{Environment.NewLine}{slogan}");
// emailMemory.Set(EmailSkill.Parameters.Recipients, "esustachah@gmail.com");
// emailMemory.Set(EmailSkill.Parameters.Subject, $"New Slogan");
// var result = await myKernel.RunAsync(emailMemory,skillEmail);

var job = new Plan("Create a Slogan");
{
  var skill = myKernel.Func("TestSkillFlex", "SloganMakerFlex");
  var context = new ContextVariables(); 
  context.Set("BUSINESS", "Wiped Balls"); 
  context.Set("CITY", "Seattle"); 
  context.Set("SPECIALTY", "basketball cleaning"); 
  var plan = new Plan(skill)
  {
    NamedParameters = context,
  };
  job.AddSteps(plan);
}
{
  var skill = myKernel.Func(nameof(EmailSkill), "SendEmailAsync");
  var context = new ContextVariables($"New Slogan{Environment.NewLine}{Environment.NewLine}{slogan}");
  context.Set(EmailSkill.Parameters.Recipients, "jamie_maxwell_webster@hotmail.com");
  context.Set(EmailSkill.Parameters.Subject, $"New Slogan");
  var plan = new Plan(skill)
  {
    NamedParameters = context,
  };
  job.AddSteps(plan);
}
var result = await job.InvokeAsync();
Console.WriteLine(result);

record OpenAIConfiguration(string ApiKey, string OrgId);

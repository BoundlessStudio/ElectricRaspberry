using System.Net.Http.Headers;
using System.Security.Claims;
using Auth0.ManagementApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

var builder = WebApplication.CreateBuilder(args);

// builder.Logging.AddJsonConsole();

builder.Services.Configure<JsonOptions>(opts => opts.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(c =>
// {
//   c.SwaggerDoc("v1", new() { 
//       Title = "TODO API",
//       Description = "Web APIs for managing a TODO list",
//       Version = "v1" 
//   });
// });
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader());
});
builder.Services
.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
  options.Authority = builder.Configuration["Auth0:Domain"];
  options.Audience = builder.Configuration["Auth0:Audience"];
});
builder.Services.AddAuthorization(options =>
{
  options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
});

var app = builder.Build();

// app.MapSwagger();
// app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapGet("/me", async (ClaimsPrincipal principal) => 
{
  // For debugging the account linking and refresh tokens
  // TODO: Remove this endpoint

  app.Logger.LogInformation("[GET]/me");

  var auth_client = await GetManagementApiClient(app.Configuration);
  var identifier = GetIdentifier(principal);
  var user = await auth_client.Users.GetAsync(identifier);
  var graph_client = GraphServiceClient(app.Configuration, user.Identities);
  var me = await graph_client.Me.Request().GetAsync();

  return new {
    User = user,
    Me = me
  };

}).RequireAuthorization();

app.MapGet("/plan", async (ClaimsPrincipal principal) =>
{
    app.Logger.LogInformation("[GET]/plan");

    var auth_client = await GetManagementApiClient(app.Configuration);
    var identifier = GetIdentifier(principal);
    var user = await auth_client.Users.GetAsync(identifier);
    var graph_client = GraphServiceClient(app.Configuration, user.Identities);
    
    var myKernel = GetKernel(app.Configuration, app.Logger, graph_client);
    var planner = new SequentialPlanner(myKernel);
    var goal = "Create a slogan for the BBQ Pit in London that specializes in Mustard Sauce then email it to me with the subject 'New Marketing Slogan'";
    var plan = await planner.CreatePlanAsync(goal);
    var cxt = myKernel.CreateNewContext();
    await plan.InvokeAsync(context: cxt);

   return Results.Text(plan.ToJson(true), "application/json");
}).RequireAuthorization();

app.Run();

static string GetIdentifier(ClaimsPrincipal principal) 
{
  return principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
}

static async Task<ManagementApiClient> GetManagementApiClient(IConfiguration config) 
{
  var domain = config["Auth0:Domain"];
  var client_id = config["Auth0:ClientId"];
  var client_secret = config["Auth0:ClientSecret"];

  var restClient = new RestClient();
  var request = new RestRequest($"{domain}/oauth/token", Method.Post);
  request.AddHeader("content-type", "application/json");
  var data = new {
    client_id,
    client_secret,
    audience = $"{domain}/api/v2/",
    grant_type = "client_credentials"
  };
  var json = JsonConvert.SerializeObject(data);
  request.AddParameter("application/json", json, ParameterType.RequestBody);
  var response = await restClient.ExecuteAsync(request);
  var result = JObject.Parse(response.Content ?? string.Empty);
  string access_token = result["access_token"]?.Value<string>() ?? string.Empty;
  var authClient = new ManagementApiClient(access_token, new Uri($"{domain}/api/v2"));
  return authClient;
}

static GraphServiceClient GraphServiceClient(IConfiguration config, Auth0.ManagementApi.Models.Identity[] identities) 
{
  var client_id = config["MsGraph:ClientId"];
  var client_secret = config["MsGraph:ClientSecret"];

  var identity = identities.FirstOrDefault(i => i.Provider == "windowslive");
  var refresh_token = identity?.RefreshToken ?? string.Empty;
  var client = new RestClient();
  var request = new RestRequest("https://login.microsoftonline.com/consumers/oauth2/v2.0/token", Method.Post);
  request.AddHeader("content-type", "application/x-www-form-urlencoded");
  request.AddParameter("application/x-www-form-urlencoded", $"grant_type=refresh_token&client_id={client_id}&client_secret={client_secret}&refresh_token={refresh_token}", ParameterType.RequestBody);
  var response = client.Execute(request);
  var result = JObject.Parse(response.Content ?? string.Empty);
  string access_token = result["access_token"]?.Value<string>() ?? string.Empty;

  var handlers = GraphClientFactory.CreateDefaultHandlers(new DelegateAuthenticationProvider((req) => 
  {
    req.Headers.Authorization = new AuthenticationHeaderValue(scheme: "bearer", parameter: access_token);
    return Task.CompletedTask;
  }));

  return new GraphServiceClient(GraphClientFactory.Create(handlers));
}

static IKernel GetKernel(IConfiguration config, ILogger logger, GraphServiceClient graph)
{
  var api_key = config["OpenAI:ApiKey"] ?? string.Empty;
  var org_id = config["OpenAI:OrgId"] ?? string.Empty;
  
  IKernel myKernel = Kernel.Builder
    .WithLogger(logger)
    .WithAIService<ITextEmbeddingGeneration>("TextEmbedding", new OpenAITextEmbeddingGeneration("text-embedding-ada-002", api_key, org_id))
    .WithAIService<IChatCompletion>("ChatCompletion", new OpenAIChatCompletion("gpt-4", api_key, org_id))
    //.WithAIService<ITextCompletion>("TextCompletion", new OpenAIChatCompletion("gpt-4", api_key, org_id))
    //.WithAIService<ITextCompletion>("TextCompletion", new OpenAIChatCompletion("gpt-3.5-turbo",  api_key, org_id))
    .WithAIService<ITextCompletion>("TextCompletion", new OpenAITextCompletion("text-davinci-003",  api_key, org_id))
    .Build();

  var template_config = new PromptTemplateConfig()
  {
      Schema = 1,
      Type = "completion",
      Description = "a function that generates marketing slogans",
      IsSensitive = false,
      Completion = new()
      {
          MaxTokens = 500,
          Temperature = 0,
          TopP = 0,
          FrequencyPenalty = 0,
          PresencePenalty = 0,
      },
      Input = new()
      {
        Parameters = new() {
          new() {
            Name = "CITY",
            Description = "The city of the business.",
          },
          new() {
            Name = "BUSINESS",
            Description = "The business name.",
          },
          new() {
            Name = "SPECIALTY",
            Description = "the specialty of the business.",
          }
        },
      },
  };
  var prompt = "Write me a marketing slogan for my {{$BUSINESS}} in {{$CITY}} with a focus on {{$SPECIALTY}} and without sacrificing quality.";
  var template = new PromptTemplate(prompt, template_config, myKernel);
  myKernel.RegisterSemanticFunction("WriterSkill", "SloganMaker", new SemanticFunctionConfig(template_config, template));

  myKernel.ImportSkill(new CloudDriveSkill(new OneDriveConnector(graph)), nameof(CloudDriveSkill));
  myKernel.ImportSkill(new TaskListSkill(new MicrosoftToDoConnector(graph)), nameof(TaskListSkill));
  myKernel.ImportSkill(new EmailSkill(new OutlookMailConnector(graph)), nameof(EmailSkill));
  myKernel.ImportSkill(new CalendarSkill(new OutlookCalendarConnector(graph)), nameof(CalendarSkill));

  return myKernel;
}
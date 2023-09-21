using ElectricRaspberry.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<BingOptions>(builder.Configuration.GetSection("Bing"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth0"));
builder.Services.Configure<BrowserlessOptions>(builder.Configuration.GetSection("Browserless"));
builder.Services.Configure<LeonardoOptions>(builder.Configuration.GetSection("Leonardo"));
builder.Services.Configure<ConvertioOptions>(builder.Configuration.GetSection("Convertio"));
builder.Services.Configure<TinyUrlOptions>(builder.Configuration.GetSection("TinyUrl"));
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

builder.Services.AddSwaggerGen(c =>
{
  c.SchemaFilter<EnumSchemaFilter>();
  c.SupportNonNullableReferenceTypes();
  c.SwaggerDoc("v1", new() { 
    Title = "Electric Raspberry",
    Description = "Apis for Volcano Lime",
    Version = "v1",
  });
  if(builder.Environment.IsDevelopment())
  {
    c.AddServer(new OpenApiServer()
    {
      Url = "https://electric-raspberry.ngrok.app",
      Description = "Development",
    });
  }
  c.AddServer(new OpenApiServer()
  {
    Url = "https://electric-raspberry.azurewebsites.net",
    Description = "Production",
  });
  c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme,
    new OpenApiSecurityScheme{
      Description = "JWT Authorization header using the Bearer scheme.",
      Type = SecuritySchemeType.Http, 
      Scheme = JwtBearerDefaults.AuthenticationScheme
  });
  c.AddSecurityRequirement(new OpenApiSecurityRequirement{ 
    {
      new OpenApiSecurityScheme
      {
        Reference = new OpenApiReference
        {
          Id = JwtBearerDefaults.AuthenticationScheme,
          Type = ReferenceType.SecurityScheme
        }
      }, new List<string>()
    }
  });
});
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(p => p.WithOrigins("https://volcano-lime.com", "https://volcano-lime.ngrok.app", "https://editor.swagger.io").AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});
builder.Services
  .AddAuthentication(options =>
  {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
  })
  .AddJwtBearer(options => 
  {
    options.Authority = builder.Configuration["Auth0:Domain"];
    options.Audience = builder.Configuration["Auth0:Audience"];
    options.Events = new JwtBearerEvents();
    options.Events.OnMessageReceived +=  context => {
      var accessToken = context.Request.Query["access_token"];
      var path = context.HttpContext.Request.Path;
      if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/feed")) context.Token = accessToken;
      return Task.CompletedTask;
    };
  });
builder.Services.AddAuthorization(options =>
{
  options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Polly.Caching.IAsyncCacheProvider, Polly.Caching.Memory.MemoryCacheProvider>();
builder.Services.AddSingleton<IStorageService, StorageService>();
//builder.Services.AddSingleton<IMemoryStore,VolatileMemoryStore>();
//builder.Services.AddSingleton<ISemanticTextMemory>(sp => {
//  var store = sp.GetRequiredService<IMemoryStore>();
//  var options = sp.GetRequiredService<IOptions<OpenAIOptions>>();
//  return new SemanticTextMemory(store, new OpenAITextEmbeddingGeneration("text-embedding-ada-002", options.Value.ApiKey, options.Value.OrgId));
//});

var app = builder.Build();

app.MapSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
if(app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app.MapHub<ClientHub>("/hub/feed");

app.Map("/", () => Results.Redirect("/swagger/"));

var goal = new GoalController();
var file = new FileController();
var api = app.MapGroup("/api");
api.RequireAuthorization();
api.WithMetadata(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized));
api.MapPost("/goal", goal.Plan).WithName("Goal");
api.MapPost("/whisper", file.Whisper).WithName("Whisper");
api.MapPost("/upload", file.Upload).WithName("Upload");

app.Run();
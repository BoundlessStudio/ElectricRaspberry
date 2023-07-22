using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<BingOptions>(builder.Configuration.GetSection("Bing"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth0"));
builder.Services.Configure<MsGraphOptions>(builder.Configuration.GetSection("MsGraph"));
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
  c.AddServer(new OpenApiServer() { 
    Url = "https://electric-raspberry.ngrok.app",
    Description = "Development Server",
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
  options.AddDefaultPolicy(p => p.WithOrigins("https://volcano-lime.ngrok.app", "https://editor.swagger.io").AllowAnyHeader().AllowAnyMethod().AllowCredentials());
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
builder.Services.AddSingleton<ITextChunkerService, TextChunkerService>();
builder.Services.AddSingleton<IFeedStorageService, FeedStorageService>();
builder.Services.AddSingleton<ICommentTaskQueue>(_ => new CommentTaskQueue(10));
builder.Services.AddSingleton<ISecurityService,SecurityService>();
builder.Services.AddSingleton<IMemoryStore,VolatileMemoryStore>();
builder.Services.AddSingleton<ISemanticTextMemory>(sp => {
  var store = sp.GetRequiredService<IMemoryStore>();
  var options = sp.GetRequiredService<IOptions<OpenAIOptions>>();
  return new SemanticTextMemory(store, new OpenAITextEmbeddingGeneration("text-embedding-ada-002", options.Value.ApiKey, options.Value.OrgId));
});
builder.Services.AddHostedService<CommentHostedService>();
builder.Services.AddDbContext<FeedsContext>(opt => opt.UseInMemoryDatabase("FeedsContext")); // builder.Services.AddDbContext<FeedsContext>(opt => opt.UseSqlite("Data Source=Database.db"));
builder.Services.AddScoped<GeorgeAgent>();
builder.Services.AddScoped<CharlesAgent>();
builder.Services.AddScoped<DalleAgent>();
builder.Services.AddScoped<JeeveAgent>();
builder.Services.AddScoped<TeslaAgent>();
builder.Services.AddScoped<AlexAgent>();
builder.Services.AddScoped<ShuriAgent>();

var app = builder.Build();

app.MapSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDeveloperExceptionPage();

app.MapHub<FeedHub>("/hub/feed");

app.Map("/", () => Results.Redirect("/swagger/index.html"));

var api = app.MapGroup("/api");
api.RequireAuthorization();
api.WithMetadata(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized));
var skill = new SkillController();
api.MapGet("/skills", skill.List).WithName("ListSkills");
var feed = new FeedController();
api.MapGet("/feeds", feed.List).WithName("ListFeeds");
api.MapPost("/feed", feed.Create).WithName("CreateFeed");
api.MapPut("/feed", feed.Edit).WithName("EditFeed");
api.MapGet("/feed/{feed_id}", feed.Get).WithName("GetFeed");
api.MapDelete("/feed/{feed_id}", feed.Delete).WithName("DeleteFeed");
var comments = new CommentController();
api.MapGet("/comments/{feed_id}", comments.List).WithName("ListComments");
api.MapPost("/comment", comments.Create).WithName("CreateComment");
api.MapDelete("/comment/{comment_id}", comments.Delete).WithName("DeleteComment");
var file = new FileController();
api.MapPost("/whisper", file.Whisper).WithName("Whisper");
api.MapPost("/upload/{feed_id}", file.Upload).WithName("Upload");

app.Services.CreateScope().ServiceProvider.GetService<FeedsContext>()?.Database?.EnsureCreated();

app.Run();
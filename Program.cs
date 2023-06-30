using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
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
builder.Services.AddDbContext<FeedsContext>(opt => opt.UseInMemoryDatabase("FeedsContext"));
//builder.Services.AddDbContext<FeedsContext>(opt => opt.UseSqlite("Data Source=Database.db"));
builder.Services.AddSingleton<GeorgeAgent>();
builder.Services.AddSingleton<DalleAgent>();
builder.Services.AddSingleton<ICommentTaskQueue>(_ => new CommentTaskQueue(10));
builder.Services.AddSingleton<ISecurityService,SecurityService>();
builder.Services.AddHostedService<CommentHostedService>();

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
var feed = new FeedController();
api.MapGet("/feeds", feed.List).WithName("ListFeed");
api.MapPost("/feed", feed.Create).WithName("CreateFeed");
api.MapPut("/feed", feed.Edit).WithName("EditFeed");
api.MapGet("/feed/{feed_id}", feed.Get).WithName("GetFeed");
api.MapDelete("/feed/{feed_id}", feed.Delete).WithName("DeleteFeed");
var comments = new CommentController();
api.MapGet("/comments/{feed_id}", comments.List).WithName("ListComments");
api.MapPost("/comment", comments.Create).WithName("CreateComment");
api.MapDelete("/comment/{comment_id}", comments.Delete).WithName("DeleteComment");

app.Run();
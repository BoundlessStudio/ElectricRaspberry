using ElectricRaspberry.Services;
using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Emotions;
using System.Reflection;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Events;

// Set up structured logging with Serilog
var logger = LoggingSetup.ConfigureStructuredLogger(
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build(),
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");

Log.Logger = logger;

// Create the web application builder with Serilog
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add configuration options
builder.Services.Configure<DiscordOptions>(
    builder.Configuration.GetSection(DiscordOptions.Discord));
builder.Services.Configure<CosmosDbOptions>(
    builder.Configuration.GetSection(CosmosDbOptions.CosmosDB));
builder.Services.Configure<StaminaSettings>(
    builder.Configuration.GetSection(StaminaSettings.Stamina));
builder.Services.Configure<PersonaOptions>(
    builder.Configuration.GetSection(PersonaOptions.ConfigSection));
builder.Services.Configure<PersonalityOptions>(
    builder.Configuration.GetSection(PersonalityOptions.ConfigSection));
builder.Services.Configure<ContextOptions>(
    builder.Configuration.GetSection(ContextOptions.ConfigSection));
builder.Services.Configure<ToolRegistryOptions>(
    builder.Configuration.GetSection(ToolRegistryOptions.ConfigSection));
builder.Services.Configure<AIEngineOptions>(
    builder.Configuration.GetSection(AIEngineOptions.ConfigSection));

// Add Observation System configuration
builder.Services.Configure<ElectricRaspberry.Services.Observation.Configuration.ObserverOptions>(
    builder.Configuration.GetSection("Observer"));
builder.Services.Configure<ElectricRaspberry.Services.Observation.Configuration.ObserverBackgroundOptions>(
    builder.Configuration.GetSection("Observer:Background"));
builder.Services.Configure<ElectricRaspberry.Services.Observation.Configuration.EventPrioritizationOptions>(
    builder.Configuration.GetSection("Observer:EventPrioritization"));
builder.Services.Configure<ElectricRaspberry.Services.Observation.Configuration.RateLimitingOptions>(
    builder.Configuration.GetSection("Observer:RateLimiting"));
builder.Services.Configure<ElectricRaspberry.Services.Observation.Configuration.ConcurrencyOptions>(
    builder.Configuration.GetSection("Observer:Concurrency"));

// Add Application Insights - Serilog is already configured to use Application Insights
var appInsightsConnectionString = builder.Configuration.GetSection("ApplicationInsights:ConnectionString").Value;
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
    
    // Use our custom telemetry initializer from LoggingOptions.cs
    builder.Services.Configure<TelemetryConfiguration>(config =>
    {
        var botName = builder.Configuration.GetSection("Persona:Name").Value ?? "ElectricRaspberry";
        var environment = builder.Environment.EnvironmentName;
        
        config.TelemetryInitializers.Add(
            new ElectricRaspberryTelemetryInitializer("1.0.0", botName, environment));
    });
}

// Add MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add Core Services
builder.Services.AddSingleton<IStaminaService, StaminaService>();
builder.Services.AddSingleton<IEmotionalService, EmotionalService>();
builder.Services.AddSingleton<IConversationService, ConversationService>();
builder.Services.AddSingleton<ICatchupService, CatchupService>();
builder.Services.AddSingleton<IKnowledgeService, KnowledgeService>();
builder.Services.AddSingleton<IPersonaService, PersonaService>();
builder.Services.AddSingleton<IPersonalityService, PersonalityService>();
builder.Services.AddSingleton<IContextBuilder, ContextBuilder>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();

// Add Observation System services
builder.Services.AddSingleton<ElectricRaspberry.Services.Observation.IChannelBufferManager, ElectricRaspberry.Services.Observation.ChannelBufferManager>();
builder.Services.AddSingleton<ElectricRaspberry.Services.Observation.IEventPrioritizationService, ElectricRaspberry.Services.Observation.EventPrioritizationService>();
builder.Services.AddSingleton<ElectricRaspberry.Services.Observation.IRateLimitingService, ElectricRaspberry.Services.Observation.RateLimitingService>();
builder.Services.AddSingleton<ElectricRaspberry.Services.Observation.IConcurrencyManager, ElectricRaspberry.Services.Observation.ConcurrencyManager>();
builder.Services.AddSingleton<ElectricRaspberry.Services.Observation.IObserverService, ElectricRaspberry.Services.Observation.ObserverService>();
builder.Services.AddHostedService<ElectricRaspberry.Services.Observation.ObserverBackgroundService>();

// Add Discord client and service
builder.Services.AddSingleton<Discord.IDiscordClient>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiscordOptions>>().Value;
    var client = new Discord.WebSocket.DiscordSocketClient(new Discord.WebSocket.DiscordSocketConfig
    {
        GatewayIntents = Discord.GatewayIntents.All,
        LogLevel = Discord.LogSeverity.Info,
        MessageCacheSize = 100,
        AlwaysDownloadUsers = true
    });
    
    return client;
});
builder.Services.AddHostedService<DiscordBotService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    app.Run();
    
    // Close and flush the logger when the application exits
    Log.Information("Application shutting down gracefully");
    Log.CloseAndFlush();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
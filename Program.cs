using ElectricRaspberry.Services;
using ElectricRaspberry.Configuration;
using ElectricRaspberry.Models.Emotions;
using System.Reflection;
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

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

// Add Application Insights
var appInsightsConnectionString = builder.Configuration.GetSection("ApplicationInsights:ConnectionString").Value;
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
    
    builder.Services.Configure<TelemetryConfiguration>(config =>
    {
        config.TelemetryInitializers.Add(new CustomTelemetryInitializer());
    });
}

// Add MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add Core Services
builder.Services.AddSingleton<IStaminaService, StaminaService>();
builder.Services.AddSingleton<IEmotionalService, EmotionalService>();
// Add remaining services as they are implemented

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

app.Run();

/// <summary>
/// Custom telemetry initializer to add bot properties to all telemetry
/// </summary>
public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(Microsoft.ApplicationInsights.Channel.ITelemetry telemetry)
    {
        telemetry.Context.Component.Version = "1.0.0";
        telemetry.Context.Cloud.RoleName = "ElectricRaspberry";
        
        // Add bot-specific properties
        if (telemetry is Microsoft.ApplicationInsights.DataContracts.TraceTelemetry trace)
        {
            trace.Properties["BotComponent"] = "Core";
        }
    }
}
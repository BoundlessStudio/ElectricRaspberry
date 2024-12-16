using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection("Discord"));

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddHttpClient();
builder.Services.AddHostedService<BotJaneSmith>();
//builder.Services.AddHostedService<BotNicholasRoach>();

using var host = builder.Build();

await host.RunAsync();
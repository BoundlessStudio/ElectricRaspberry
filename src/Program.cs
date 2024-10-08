using ElectricRaspberry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddHttpClient();
builder.Services.AddHostedService<Application>();

using var host = builder.Build();
await host.RunAsync();


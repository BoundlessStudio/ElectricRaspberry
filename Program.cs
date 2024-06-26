using Azure.AI.OpenAI.Assistants;
using ElectricRaspberry.Controllers;
using ElectricRaspberry.Extensions;
using ElectricRaspberry.Models;
using ElectricRaspberry.Swagger;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SettingsModel>(builder.Configuration.GetSection("Settings"));
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddDefaultSwaggerGen(builder.Environment.IsDevelopment());
builder.Services.AddDefaultCors();
builder.Services.AddDefaultAuthentication(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<AssistantController>();
builder.Services.AddSingleton(sp =>
{
  var settings = sp.GetRequiredService<IOptions<SettingsModel>>();
  return new AssistantsClient(settings.Value.OpenAiApiKey);
});

var app = builder.Build();

app.MapSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDeveloperExceptionPage();
app.MapHub<ClientHub>("/hub/feed");
app.Map("/", () => Results.Redirect("/swagger/"));

var api = app.MapGroup("/api");
// api.RequireAuthorization();
// api.WithMetadata(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized));

var assistantController = app.Services.GetService<AssistantController>() ?? throw new NullReferenceException("AssistantController");
api.MapPost("/assistant/create", assistantController.Create).WithName("Assistant Create");

app.Run();
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Audio;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddHttpClient();
builder.Services.AddSingleton(new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
builder.Services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetChatClient("gpt-4o-mini"));
builder.Services.AddKeyedSingleton("tts", (sp, key) => sp.GetRequiredService<OpenAIClient>().GetAudioClient("tts-1"));
builder.Services.AddKeyedSingleton("stt", (sp, key) => sp.GetRequiredService<OpenAIClient>().GetAudioClient("whisper-1"));

using var host = builder.Build();
await host.RunAsync();


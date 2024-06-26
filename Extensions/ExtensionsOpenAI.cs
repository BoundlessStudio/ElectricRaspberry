using ElectricRaspberry.Models;
using Microsoft.Extensions.Options;
using OpenAI;

namespace ElectricRaspberry.Extensions;

public static class ExtensionsOpenAI
{
  internal static void AddOpenAIClient(this IServiceCollection services)
  {
    services.AddSingleton(sp => {
      var settings = sp.GetRequiredService<IOptions<AiSettingsModel>>();
      var key = settings.Value.OpenAiApiKey ?? throw new InvalidOperationException("Missing OpenAi ApiKey");
      var options = new OpenAIClientOptions()
      {
        ApplicationId = "electric-raspberry",
        NetworkTimeout = TimeSpan.FromMinutes(3),
        RetryPolicy = new ExponentialRetryPipelinePolicy(maxRetryCount: 6, initialDelay: TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30)),
      };
      return new OpenAIClient(key, options);
    });
    services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetAssistantClient());
    services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetFileClient());
    services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetVectorStoreClient());
    services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetChatClient("gpt-4o"));
    services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetEmbeddingClient("text-embedding-3-large"));
    services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetImageClient("dall-e-3"));
    services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetAudioClient("whisper-1"));
  }
}
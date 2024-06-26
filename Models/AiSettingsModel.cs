namespace ElectricRaspberry.Models;

public class AiSettingsModel
{
  // Open AI
  public string? OpenAiApiKey { get; set; }
  public string? OpenAiOrgId { get; set; }

  // Anthropic
  public string? AnthropicApiKey { get; set; }

  // Groq
  public string? GroqApiKey { get; set; }
}

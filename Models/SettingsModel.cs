namespace ElectricRaspberry.Models;

public class SettingsModel
{
  public string CosmosConectionString { get; set; } = String.Empty;

  public string AnthropicApiKey { get; set; } = String.Empty;
  public string GroqApiKey { get; set; } = String.Empty;

  public string OpenAiApiKey { get; set; } = String.Empty;
  public string OpenAiOrgId { get; set; } = String.Empty;

  public string Auth0Domain { get; set; } = string.Empty;
  public string Auth0Audience { get; set; } = string.Empty;
  public string Auth0ClientId { get; set; } = string.Empty;
  public string Auth0ClientSecret { get; set; } = string.Empty;
}

using System.Text.Json.Serialization;

namespace ElectricRaspberry.Models;

public sealed class WebPage
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("url")]
  public string Url { get; set; } = string.Empty;

  [JsonPropertyName("snippet")]
  public string Snippet { get; set; } = string.Empty;
}
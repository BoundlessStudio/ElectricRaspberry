using System.Text.Json.Serialization;

public class ShuriAddArg
{
  [JsonPropertyName("id")]
  public string Id {get; set;} = string.Empty;

  [JsonPropertyName("prompt")]
  public string Prompt {get; set;} = string.Empty;
}
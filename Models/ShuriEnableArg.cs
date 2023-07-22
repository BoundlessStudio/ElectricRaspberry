using System.Text.Json.Serialization;

public class ShuriEnableArg
{
  [JsonPropertyName("id")]
  public string Id {get; set;} = string.Empty;
}
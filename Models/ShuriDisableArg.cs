using System.Text.Json.Serialization;

public class ShuriDisableArg
{
  [JsonPropertyName("id")]
  public string Id {get; set;} = string.Empty;
}
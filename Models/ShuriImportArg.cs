using System.Text.Json.Serialization;

public class ShuriImportArg
{
  [JsonPropertyName("id")]
  public string Id {get; set;} = string.Empty;

  [JsonPropertyName("url")]
  public string Url {get; set;} = string.Empty;
}
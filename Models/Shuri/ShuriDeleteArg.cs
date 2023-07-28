using System.Text.Json.Serialization;

public class ShuriDeleteArg
{

  [JsonPropertyName("id")]
  public string Id {get; set;} = string.Empty;
}
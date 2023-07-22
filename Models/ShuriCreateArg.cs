using System.Text.Json.Serialization;

public class ShuriCreateArg
{
  [JsonPropertyName("name")]
  public string Name {get; set;} = string.Empty;

  [JsonPropertyName("description")]
  public string Description {get; set;} = string.Empty;
}
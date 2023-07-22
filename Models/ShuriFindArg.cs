using System.Text.Json.Serialization;

public class ShuriFindArg
{

  [JsonPropertyName("name")]
  public string Name {get; set;} = string.Empty;
}
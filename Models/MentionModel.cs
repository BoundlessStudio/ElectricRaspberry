
using System.Text.Json.Serialization;

public class MentionModel 
{
  [JsonPropertyName("user")]
  public string User {get; set;} = string.Empty;
  
  [JsonPropertyName("intent")]
  public string Intent {get; set;} = string.Empty;
}
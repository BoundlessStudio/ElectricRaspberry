using System.Text.Json.Serialization;

public class JeeveRecallArg
{
  [JsonPropertyName("text")]
  public string Text { get; set; } = string.Empty;

  [JsonPropertyName("limit")]
  public int Limit { get; set; } = 3;

  [JsonPropertyName("relevance")]
  public double Relevance { get; set; } = 0.3;
}
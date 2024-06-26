using System.Text.Json.Serialization;

public class GraphStatementsDto
{
  [JsonPropertyName("statements")]
  public List<string> Statements { get; set; } =  new List<string>();
}
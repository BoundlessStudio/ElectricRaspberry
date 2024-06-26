using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ElectricRaspberry.Views;

public class ModifyAssistantDto
{
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("description")]
  public string Description { get; set; }

  [JsonPropertyName("instructions")]
  public string Instructions { get; set; }

  [JsonPropertyName("interpreter")]
  public List<string> InterpreterFiles { get; set; }

  [JsonPropertyName("stores")]
  public List<string> VectorStores { get; set; }

  [JsonPropertyName("graphs")]
  public List<string> Graphs { get; set; }

  [JsonPropertyName("tables")]
  public List<string> Tables { get; set; }

  [JsonConstructor]
  public ModifyAssistantDto(string name, string description, string instructions)
  {
    this.Name = name;
    this.Description = description;
    this.Instructions = instructions;
    this.InterpreterFiles = new List<string>();
    this.VectorStores = new List<string>();
    this.Graphs = new List<string>();
    this.Tables = new List<string>();
  }
}

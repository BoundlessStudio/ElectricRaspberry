using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Assistants;

namespace ElectricRaspberry.Views;

public class AssistantDto
{
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("description")]
  public string Description { get; set; }

  [JsonPropertyName("instructions")]
  public string Instructions { get; set; }

  [JsonPropertyName("version")]
  public string Version { get; set; }

  [JsonPropertyName("interpreter")]
  public List<string> InterpreterFiles { get; set; }

  [JsonPropertyName("stores")]
  public List<string> VectorStores { get; set; }

  [JsonPropertyName("graphs")]
  public List<string> Graphs { get; set; }

  [JsonPropertyName("tables")]
  public List<string> Tables { get; set; }

  [JsonConstructor]
  public AssistantDto(string name, string description, string instructions, string version, List<string> files, List<string> stores, List<string> graphs, List<string> tables)
  {
    this.Name = name;
    this.Description = description;
    this.Instructions = instructions;
    this.Version = version;
    this.InterpreterFiles = files;
    this.VectorStores = stores;
    this.Graphs = graphs;
    this.Tables = tables;
  }

  internal AssistantDto(Assistant assistant)
  {
    this.Name = assistant.Name;
    this.Description = assistant.Description;
    this.Instructions = assistant.Instructions;
    this.Version = assistant.Metadata["version"];
    this.InterpreterFiles = assistant.ToolResources.CodeInterpreter.FileIds.ToList();
    this.VectorStores = assistant.ToolResources.FileSearch.VectorStoreIds.ToList();
    this.Tables = JsonSerializer.Deserialize<List<string>>(assistant.Metadata["tables"]) ?? new List<string>();
    this.Graphs = JsonSerializer.Deserialize<List<string>>(assistant.Metadata["graphs"]) ?? new List<string>();
  }
}

using System.Text.Json.Serialization;

namespace ElectricRaspberry.Views;

public class CreateAssistantDto
{
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("description")]
  public string Description { get; set; }

  [JsonConstructor]
  public CreateAssistantDto(string name, string description)
  {
    this.Name = name;
    this.Description = description;
  }
}
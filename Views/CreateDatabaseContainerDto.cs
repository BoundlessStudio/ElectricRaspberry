using System.Text.Json.Serialization;

public class CreateDatabaseContainerDto
{
  [JsonConstructor]
  public CreateDatabaseContainerDto(string container)
  {
    this.Container = container;

  }

  public string Container { get; set; }
}
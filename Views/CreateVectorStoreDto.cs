using System.Text.Json.Serialization;

public class CreateVectorStoreDto
{
  [JsonConstructor]
  public CreateVectorStoreDto(string name, int? expiration)
  {
    this.Name = name;
    this.Expiration = expiration;
  }

  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("expiration")]
  public int? Expiration { get; set; }
}
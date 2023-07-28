

using System.Text.Json.Serialization;

public class GenerationJob
{
  [JsonPropertyName("sdGenerationJob")]
  public StandardGenerationJob Job {get; set;} = new StandardGenerationJob();
}

public class StandardGenerationJob
{
  [JsonPropertyName("generationId")]
  public string GenerationId { get; set; } = string.Empty;
}

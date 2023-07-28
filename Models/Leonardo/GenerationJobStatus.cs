
using System.Text.Json.Serialization;

public class GeneratedImage
{
  [JsonPropertyName("url")]
  public string Url { get; set; } = string.Empty;

  [JsonPropertyName("nsfw")]
  public bool Nsfw { get; set; }

  [JsonPropertyName("id")]
  public string Id { get; set; } = string.Empty;

  // [JsonPropertyName("likeCount")]
  // public int LikeCount { get; set; }

  // [JsonPropertyName("generated_image_variation_generics")]
  // public List<object> GeneratedImageVariationGenerics { get; set; }
}

public class GenerationsByPk
{
  [JsonPropertyName("generated_images")]
  public List<GeneratedImage> GeneratedImages { get; set; } = new List<GeneratedImage>();

  // [JsonPropertyName("modelId")]
  // public object ModelId { get; set; }

  // [JsonPropertyName("prompt")]
  // public string Prompt { get; set; }

  // [JsonPropertyName("negativePrompt")]
  // public string NegativePrompt { get; set; }

  // [JsonPropertyName("imageHeight")]
  // public int ImageHeight { get; set; }

  // [JsonPropertyName("imageWidth")]
  // public int ImageWidth { get; set; }

  // [JsonPropertyName("inferenceSteps")]
  // public int InferenceSteps { get; set; }

  [JsonPropertyName("seed")]
  public int Seed { get; set; } = 0;

  // [JsonPropertyName("public")]
  // public bool Public { get; set; }

  // [JsonPropertyName("scheduler")]
  // public string Scheduler { get; set; }

  // [JsonPropertyName("sdVersion")]
  // public string SdVersion { get; set; }

  [JsonPropertyName("status")]
  public string Status { get; set; } = string.Empty;

  // [JsonPropertyName("presetStyle")]
  // public object PresetStyle { get; set; }

  // [JsonPropertyName("initStrength")]
  // public object InitStrength { get; set; }

  // [JsonPropertyName("guidanceScale")]
  // public int GuidanceScale { get; set; }

  [JsonPropertyName("id")]
  public string Id { get; set; } = string.Empty;

  // [JsonPropertyName("createdAt")]
  // public DateTime CreatedAt { get; set; }
}

public class GenerationJobStatus
{
  [JsonPropertyName("generations_by_pk")]
  public GenerationsByPk Generations { get; set; } = new GenerationsByPk();
}

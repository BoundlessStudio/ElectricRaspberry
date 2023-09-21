
using System.Text.Json.Serialization;

public class ImageGenerationRequest
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("negative_prompt")]
    public string NegativePrompt { get; set; } = string.Empty;

    // [JsonPropertyName("nsfw")]
    // public bool Nsfw { get; set; } = true;

    [JsonPropertyName("num_images")]
    public int NumImages { get; set; } = 1;

    [JsonPropertyName("width")]
    public int Width { get; set; } = 1024;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 768;

    [JsonPropertyName("num_inference_steps")]
    public int NumInferenceSteps { get; set; } = 10;

    [JsonPropertyName("guidance_scale")]
    public int GuidanceScale { get; set; } = 15;

    [JsonPropertyName("init_strength")]
    public double InitStrength { get; set; } = 0.55;

    // [JsonPropertyName("sd_version")]
    // public string SdVersion { get; set; } = "v1_5";

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = "ac614f96-1082-45bf-be9d-757f2d31c174";

    [JsonPropertyName("presetStyle")]
    public string PresetStyle { get; set; } = "DYNAMIC"; //LEONARDO

    [JsonPropertyName("scheduler")]
    public string Scheduler { get; set; } = "LEONARDO"; // EULER_DISCRETE 

    [JsonPropertyName("public")]
    public bool Public { get; set; } = false;

    [JsonPropertyName("tiling")]
    public bool Tiling { get; set; } = false;

    [JsonPropertyName("promptMagic")]
    public bool PromptMagic { get; set; } = true;

    // [JsonPropertyName("imagePromptWeight")]
    // public double ImagePromptWeight { get; set; } = 0.65;

    //[JsonPropertyName("alchemy")]
    //public bool Alchemy { get; set; } = true;

    // [JsonPropertyName("highResolution")]
    // public bool HighResolution { get; set; } = false;

    // [JsonPropertyName("leonardoMagicVersion")]
    // public string LeonardoMagicVersion { get; set; } = "v3";
}
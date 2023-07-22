using System.Text.Json.Serialization;

public class JeeveSaveArg
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
}
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ElectricRaspberry.Models.TinyUrl
{
  public class TinyUrlRequest
  {
    [JsonProperty("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("domain")]
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "tinyurl.com";
  }
}

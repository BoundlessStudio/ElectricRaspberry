using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ElectricRaspberry.Models.TinyUrl
{
  public class TinyUrlResponse
  {
    [JsonProperty("data")]
    [JsonPropertyName("data")]
    public Data Data { get; set; } = new Data();

    [JsonProperty("code")]
    [JsonPropertyName("code")]
    public int Code { get; set; } = 0;
  }

  public class Data
  {
    [JsonProperty("alias")]
    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonProperty("tiny_url")]
    [JsonPropertyName("tiny_url")]
    public string Url { get; set; } = string.Empty;
  }
}

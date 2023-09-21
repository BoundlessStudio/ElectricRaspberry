using System.Text.Json.Serialization;

namespace ElectricRaspberry.Models.Converter
{

  public class ReponseStatusDocument
  {
    [JsonPropertyName("data")]
    public ReponseStatusData Data { get; set; } = new ReponseStatusData();
  }

  public class ReponseStatusData
  {
    [JsonPropertyName("step_percent")]
    public int Percent { get; set; }

    [JsonPropertyName("output")]
    public ReponseStatusOutput Output { get; set; } = new ReponseStatusOutput();
  }

  public class ReponseStatusOutput
  {
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
  }
}

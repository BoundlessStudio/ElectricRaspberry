using System.Text.Json.Serialization;

namespace ElectricRaspberry.Models.Converter
{

  public class RequestConvertDocument
  {
    public RequestConvertDocument(string apikey, string url, string output)
    {
      this.apikey = apikey;
      this.input = "url";
      this.file = url;
      this.outputformat = output;
    }

    public string apikey { get; set; }

    public string input { get; private set; }

    public string file { get; set; }

    public string outputformat { get; set; }
  }

  public class ReponseConvertDocument
  {
    [JsonPropertyName("data")]
    public ReponseConvertData Data { get; set; } = new ReponseConvertData();
  }

  public class ReponseConvertData
  {
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("minutes")]
    public long minutes { get; set; }
  }
}

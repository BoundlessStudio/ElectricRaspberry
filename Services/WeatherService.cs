
using System.Text.Json.Serialization;
using OpenAI;

public class Location
{
  [JsonPropertyName("latitude")]
  public double Latitude {get; set;}

  [JsonPropertyName("longitude")]
  public double Longitude  {get; set;}
}

public class WeatherService
{
  public static async Task<ToolResult> GetWeatherAsync(Location location)
  {
    var httpClient = new HttpClient();
    var url = $"https://api.open-meteo.com/v1/forecast?latitude={location.Latitude}&longitude={location.Longitude}&hourly=temperature_2m";
    var response = await httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();

    var content = await response.Content.ReadAsStringAsync();
    var result =  new ToolResult()
    {
      Content = content
    };
    return result;
  }
}

using System.Globalization;
using System.Text.Json.Serialization;

namespace ElectricRaspberry.Models;

public interface IUser
{
  string Id { get; set; }
  string Name { get; set; }
  string Lanauge { get; set; }
  string City { get; set; }
  string Country { get; set; }
  string Timezone { get; set; }
  string Picture { get; set; }
  string Latitude { get; set; }
  string Longitude { get; set; }

  [JsonIgnore]
  CultureInfo CultureInfo { get; }

  [JsonIgnore]
  TimeZoneInfo TimeZoneInfo { get; }
}

public class User : IUser
{
  public string Id { get; set; }
  public string Name { get; set; }
  public string Lanauge { get; set; }
  public string City { get; set; }
  public string Country { get; set; }
  public string Timezone { get; set; }
  public string Latitude { get; set; }
  public string Longitude { get; set; }
  public string Picture { get; set; }

  [JsonIgnore]
  public CultureInfo CultureInfo
  {
    get
    {
      try
      {
        return new CultureInfo(this.Lanauge);
      }
      catch (CultureNotFoundException)
      {
        return new CultureInfo("en-US");
      }
    }
  }

  [JsonIgnore]
  public TimeZoneInfo TimeZoneInfo
  {
    get
    {
      try
      {
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(this.Timezone, out string? timezone))
          return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        else
          throw new TimeZoneNotFoundException();
      }
      catch (Exception)
      {
        return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
      }
    }

    
  }
}
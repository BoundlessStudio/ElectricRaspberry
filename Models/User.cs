using System.Globalization;
using System.Text.Json.Serialization;

namespace ElectricRaspberry.Models;

public interface IUser
{
  string Id { get; set; }
  string OrganizationId { get; set; }
  string Name { get; set; }
  string? Language { get; set; }
  string? City { get; set; }
  string? Country { get; set; }
  string? Timezone { get; set; }
  string? Picture { get; set; }
  string? Latitude { get; set; }
  string? Longitude { get; set; }

  [JsonIgnore]
  CultureInfo CultureInfo { get; }

  [JsonIgnore]
  TimeZoneInfo TimeZoneInfo { get; }
}

public class User : IUser
{
  public string Id { get; set; } = string.Empty;
  public string OrganizationId { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string? Language { get; set; }
  public string? City { get; set; }
  public string? Country { get; set; }
  public string? Timezone { get; set; }
  public string? Latitude { get; set; }
  public string? Longitude { get; set; }
  public string? Picture { get; set; }

  [JsonIgnore]
  public CultureInfo CultureInfo
  {
    get
    {
      try
      {
        return new CultureInfo(this.Language ?? string.Empty);
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
        if(this.Timezone is null)
          throw new TimeZoneNotFoundException();
        else if (TimeZoneInfo.TryConvertIanaIdToWindowsId(this.Timezone, out string? timezone))
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

  public static User Default => new User()
  {
    Id = "b7bfedf9-55b9-4946-a8e6-f222fe93c93f",
    Name = "RGBKnights",
    City = "New York",
    Country = "USA",
    Language = "en-US",
    Latitude = "40.7128",
    Longitude = "-74.0060",
    Picture = "default_picture.png",
    Timezone = "America/New_York",
  };
}
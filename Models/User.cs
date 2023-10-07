namespace ElectricRaspberry.Models
{
  public interface IUser
  {
    string Id { get; set; }
    string Container { get; }
    string Name { get; set; }
    string Lanauge { get; set; }
    string Ip { get; set; }
    string City { get; set; }
    string Continent { get; set; }
    string Country { get; set; }
    string Latitude { get; set; }
    string Longitude { get; set; }
    string Timezone { get; set; }
    string Picture { get; set; }
  }

  public class User : IUser
  {
    public User(string id, string name, string lanauge, string ip, string city, string continent, string country, string latitude, string longitude, string timezone, string picture)
    {
      Id = id;
      Name = name;
      Lanauge = lanauge;
      Ip = ip;
      City = city;
      Continent = continent;
      Country = country;
      Latitude = latitude;
      Longitude = longitude;
      Timezone = timezone;
      Picture = picture;
    }

    public string Id { get; set; }
    public string Container => Id.Replace("|", "-");
    public string Name { get; set; }
    public string Lanauge { get; set; }
    public string Ip { get; set; }
    public string City { get; set; }
    public string Continent { get; set; }
    public string Country { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }
    public string Timezone { get; set; }
    public string Picture { get; set; }
  }
}

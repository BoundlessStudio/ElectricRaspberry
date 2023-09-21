namespace ElectricRaspberry.Models
{
  public interface IUser
  {
    string Id { get; set; }
    string Container { get; }
    string Name { get; set; }
    string Lanauge { get; set; }
    string Ip { get; set; }
    string Country { get; set; }
    string Timezone { get; set; }
    string Picture { get; set; }
  }

  public class User : IUser
  {
    public User(string id, string name, string lanauge, string ip, string country, string timezone, string picture)
    {
      Id = id;
      Name = name;
      Lanauge = lanauge;
      Ip = ip;
      Country = country;
      Timezone = timezone;
      Picture = picture;
    }

    public string Id { get; set; }
    public string Container => Id.Replace("|", "-");
    public string Name { get; set; }
    public string Lanauge { get; set; }
    public string Ip { get; set; }
    public string Country { get; set; }
    public string Timezone { get; set; }
    public string Picture { get; set; }
  }
}

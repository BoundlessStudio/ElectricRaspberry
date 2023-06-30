public interface IAuthorizedUser
{
  string UserId { get; set; }
  string Name { get; set; }
  string Mention { get; set; }
  string Picture { get; set; }
}

public class AuthorizedUser : IAuthorizedUser
{
    public AuthorizedUser(string user, string name, string mention, string picture)
    {
      this.UserId = user;
      this.Name = name;
      this.Mention = mention;
      this.Picture = picture;
    }

    public string UserId { get; set; }
    public string Name { get; set; }
    public string Mention { get; set; }
    public string Picture { get; set; }
    
}

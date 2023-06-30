using System.ComponentModel.DataAnnotations;

public class AuthorDocument
{
  [Required]
  public string Picture {get; set;}  = string.Empty;

  [Required]
  public string Name {get; set;}  = string.Empty;
  
  [Required]
  public string Mention {get; set;}  = string.Empty;

  public static AuthorDocument Unknown => new AuthorDocument() { Name = "Unknown", Mention = "", Picture = "https://i.imgur.com/xZNxZDa.png" };
}
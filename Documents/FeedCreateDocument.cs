
using System.ComponentModel.DataAnnotations;

public class FeedCreateDocument
{
  [Required]
  public string Name {get; set;} = string.Empty;

  [Required]
  public string Template {get; set;} = string.Empty;
}
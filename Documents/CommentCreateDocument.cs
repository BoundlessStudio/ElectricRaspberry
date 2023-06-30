

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class CommentCreateDocument
{
  [Required]
  public string FeedId {get; set;} = string.Empty;

  [DefaultValue("comment")]
  public string Type {get; set;} = "comment";
  
  [Required]
  public string Body {get; set;} = string.Empty;
}
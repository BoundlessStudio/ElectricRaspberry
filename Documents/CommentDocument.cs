

using System.ComponentModel.DataAnnotations;

public class CommentDocument
{
  [Required]
  public string CommentId { get; set;} = string.Empty;

  [Required]
  public string Type { get; set; } = string.Empty;

  [Required]
  public string Body { get; set; } = string.Empty;

  [Required]
  public int Tokens {get; set;} = 0;

  [Required]
  public int Characters {get; set;} = 0;

  [Required]
  public string FeedId {get; set;} = string.Empty;

  [Required]
  public AuthorDocument Author { get; set;} = new AuthorDocument();

  [Required]
  public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
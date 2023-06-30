
using System.ComponentModel.DataAnnotations;

public class FeedEditDocument
{
  [Required]
  public string FeedId {get; set;}  = string.Empty;
  
  [Required]
  public string Name { get; set; } = string.Empty;

  [Required]
  public string Description  { get; set; } = string.Empty;

  [Required]
  public FeedAccess Access { get; set; } = FeedAccess.Private;
}
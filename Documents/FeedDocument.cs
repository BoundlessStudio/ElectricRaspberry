
using System.ComponentModel.DataAnnotations;

public class FeedDocument
{
  [Required]
  public string FeedId { get; set; } = string.Empty;

  [Required]
  public string Name { get; set; } = string.Empty;

  [Required]
  public string Description  { get; set; } = string.Empty;

  [Required]
  public FeedAccess Access { get; set; } = FeedAccess.Private;

  [Required]
  public List<string> SelectedSkills { get; set; } = new();
}
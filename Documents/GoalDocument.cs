using System.ComponentModel.DataAnnotations;

public class GoalDocument
{
  [Required]
  public string Id { get; set; } = string.Empty;

  [Required]
  public string Goal {get; set;} = string.Empty;

  [Required]
  public List<MessageDocument> History { get; set; } = new List<MessageDocument>();
}
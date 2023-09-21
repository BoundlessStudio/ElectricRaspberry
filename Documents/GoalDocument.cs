using System.ComponentModel.DataAnnotations;

public class GoalDocument
{
  [Required]
  public string Goal {get; set;} = string.Empty;

  [Required]
  public string ConnectionId { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

public class GoalDocument
{
  [Required]
  public string Goal {get; set;} = string.Empty;

  [Required]
  public List<MessageDocument> Messages { get; set; } = new List<MessageDocument>();

  [Required]
  public string ConnectionId { get; set; } = string.Empty;
}
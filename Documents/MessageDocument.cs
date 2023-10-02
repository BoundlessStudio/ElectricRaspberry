using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

public class MessageDocument
{
  [Required]
  public Role Role { get; set; }

  [Required]
  public string Content { get; set; } = string.Empty;

  public List<StepDocument> Logs { get; set; } = new List<StepDocument>();
}


public enum Role
{
  [EnumMember(Value = "system")]
  System = 1,
  [EnumMember(Value = "assistant")]
  Assistant,
  [EnumMember(Value = "user")]
  User,
}


public class StepDocument
{
  /// <summary>
  /// Gets or sets the step number.
  /// </summary>
  [JsonPropertyName("thought")]
  public string Thought { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the action of the step
  /// </summary>
  [JsonPropertyName("action")]
  public string Action { get; set; } = string.Empty;


  /// <summary>
  /// Gets or sets the output of the action
  /// </summary>
  [JsonPropertyName("observation")]
  public string Observation { get; set; } = string.Empty;

  public bool IsEmpty()
  {
    return this.Thought == string.Empty && this.Action == string.Empty && this.Observation == string.Empty;
  }
}
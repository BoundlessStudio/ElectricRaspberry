using System.ComponentModel.DataAnnotations;

public class SkillDocument
{
  [Required]
  public string SkillId { get; set; } = string.Empty;
  [Required]
  public string Name { get; set; } = string.Empty;
  [Required]
  public string Owner { get; set; } = string.Empty;
  [Required]
  public SkillType Type { get; set; } = SkillType.Unknown;
  public string? TypeOf { get; set; }
  public string? Prompt { get; set; }
  public string? Description{ get; set; }
  public string? Url  { get; set; }
}
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.SemanticFunctions;

public enum SkillType
{
  Unknown,
  Coded,
  Semantic,
  OpenApi,
}

public class SkillRecord
{
  public const string SystemOwner = "system";
  public const string MicrosoftOwner = "windowslive";

  public string SkillId { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Owner { get; set; } = string.Empty;
  public SkillType Type { get; set; } = SkillType.Unknown;
  public string? TypeOf { get; set; } = null;
  public string? Prompt { get; set; }= null;
  public string? Description{ get; set; } = null;
  public string? Url  { get; set; } = null;
}
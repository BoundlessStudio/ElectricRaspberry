using System.ComponentModel.DataAnnotations;

public class FileDocument
{
  [Required]
  public string Name {get; set;} = string.Empty;
  
  [Required]
  public string Url {get; set;} = string.Empty;

  [Required]
  public string Type {get; set;} = string.Empty;

  [Required]
  public long Size {get; set;} = 0;
}
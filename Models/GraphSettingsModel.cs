using System.ComponentModel.DataAnnotations;

namespace ElectricRaspberry.Models;

public class GraphSettingsModel
{ 
  [Required]
  public string Host { get; set; } = string.Empty;
  [Required]
  public string Key { get; set; } = string.Empty;
}

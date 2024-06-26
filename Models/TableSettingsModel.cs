using System.ComponentModel.DataAnnotations;

namespace ElectricRaspberry.Models;

public class TableSettingsModel
{ 
  [Required]
  public string ConnectionString { get; set; } = string.Empty;

}

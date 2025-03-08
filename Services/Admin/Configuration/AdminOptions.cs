namespace ElectricRaspberry.Services.Admin.Configuration;

/// <summary>
/// Configuration options for the admin service
/// </summary>
public class AdminOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string Admin = "Admin";
    
    /// <summary>
    /// List of Discord user IDs that have admin permissions
    /// </summary>
    public List<string> AdminUserIds { get; set; } = new List<string>();
    
    /// <summary>
    /// List of Discord role IDs that have admin permissions
    /// </summary>
    public List<string> AdminRoleIds { get; set; } = new List<string>();
    
    /// <summary>
    /// Default duration in minutes for sleep mode if not specified
    /// </summary>
    public int DefaultSleepDurationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Default duration in minutes for silence mode if not specified
    /// </summary>
    public int DefaultSilenceDurationMinutes { get; set; } = 15;
    
    /// <summary>
    /// Maximum allowed duration in minutes for sleep mode
    /// </summary>
    public int MaxSleepDurationMinutes { get; set; } = 480; // 8 hours
    
    /// <summary>
    /// Maximum allowed duration in minutes for silence mode
    /// </summary>
    public int MaxSilenceDurationMinutes { get; set; } = 120; // 2 hours
    
    /// <summary>
    /// Require confirmation for emergency stop
    /// </summary>
    public bool RequireEmergencyStopConfirmation { get; set; } = true;
    
    /// <summary>
    /// Whether to log admin commands to Application Insights
    /// </summary>
    public bool LogAdminCommands { get; set; } = true;
}
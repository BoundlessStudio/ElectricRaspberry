namespace ElectricRaspberry.Configuration;

public class ApplicationInsightsOptions
{
    public const string ApplicationInsights = "ApplicationInsights";
    
    public string ConnectionString { get; set; } = string.Empty;
}
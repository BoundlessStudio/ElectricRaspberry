using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog.Events;

namespace ElectricRaspberry.Configuration
{
    /// <summary>
    /// Configuration options for the logging system
    /// </summary>
    public class LoggingOptions
    {
        public const string ConfigSection = "Logging";

        /// <summary>
        /// Minimum log level to capture
        /// </summary>
        public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

        /// <summary>
        /// Whether to log to the console
        /// </summary>
        public bool EnableConsoleLogging { get; set; } = true;

        /// <summary>
        /// Whether to use structured logging format
        /// </summary>
        public bool UseStructuredLogging { get; set; } = true;

        /// <summary>
        /// Whether to enrich log events with thread IDs
        /// </summary>
        public bool EnrichWithThreadId { get; set; } = true;

        /// <summary>
        /// Whether to enrich log events with process information
        /// </summary>
        public bool EnrichWithProcessInfo { get; set; } = true;

        /// <summary>
        /// Whether to enrich log events with memory usage
        /// </summary>
        public bool EnrichWithMemoryUsage { get; set; } = true;

        /// <summary>
        /// Whether to enrich log events with environment information
        /// </summary>
        public bool EnrichWithEnvironment { get; set; } = true;

        /// <summary>
        /// Whether to enrich log events with machine name
        /// </summary>
        public bool EnrichWithMachineName { get; set; } = true;

        /// <summary>
        /// The template used for console output
        /// </summary>
        public string ConsoleOutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Log level overrides for specific namespaces
        /// </summary>
        public Dictionary<string, LogEventLevel> LogLevelOverrides { get; set; } = new Dictionary<string, LogEventLevel>
        {
            { "Microsoft", LogEventLevel.Warning },
            { "System", LogEventLevel.Warning },
            { "ElectricRaspberry.Services", LogEventLevel.Information }
        };
    }

    /// <summary>
    /// Custom telemetry initializer for Application Insights
    /// </summary>
    public class ElectricRaspberryTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _componentVersion;
        private readonly string _botName;
        private readonly string _environmentName;

        public ElectricRaspberryTelemetryInitializer(string componentVersion, string botName, string environmentName)
        {
            _componentVersion = componentVersion;
            _botName = botName;
            _environmentName = environmentName;
        }

        public void Initialize(ITelemetry telemetry)
        {
            // Set standard properties
            telemetry.Context.Component.Version = _componentVersion;
            telemetry.Context.Cloud.RoleName = _botName;
            
            // Add custom properties to all telemetry
            if (telemetry is Microsoft.ApplicationInsights.DataContracts.TraceTelemetry trace)
            {
                trace.Properties["BotComponent"] = "Core";
                trace.Properties["Environment"] = _environmentName;
            }
            else if (telemetry is Microsoft.ApplicationInsights.DataContracts.RequestTelemetry request)
            {
                request.Properties["BotComponent"] = "Core";
                request.Properties["Environment"] = _environmentName;
            }
            else if (telemetry is Microsoft.ApplicationInsights.DataContracts.DependencyTelemetry dependency)
            {
                dependency.Properties["BotComponent"] = "Core";
                dependency.Properties["Environment"] = _environmentName;
            }
            else if (telemetry is Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry exception)
            {
                exception.Properties["BotComponent"] = "Core";
                exception.Properties["Environment"] = _environmentName;
            }
        }
    }
}
using ElectricRaspberry.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Handles setup and configuration of the logging system
    /// </summary>
    public static class LoggingSetup
    {
        /// <summary>
        /// Configures the Serilog logger with structured logging capabilities
        /// </summary>
        public static Logger ConfigureStructuredLogger(IConfiguration configuration, string environment)
        {
            // Get logging options from configuration
            var loggingOptions = new LoggingOptions();
            configuration.GetSection(LoggingOptions.ConfigSection).Bind(loggingOptions);

            // Get app insights connection string
            var appInsightsConnectionString = configuration.GetSection("ApplicationInsights:ConnectionString").Value;
            
            // Get bot name from persona options
            var botName = configuration.GetSection("Persona:Name").Value ?? "ElectricRaspberry";

            // Build logger configuration
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(loggingOptions.MinimumLevel)
                .Enrich.FromLogContext();

            // Add specific log level overrides
            foreach (var override in loggingOptions.LogLevelOverrides)
            {
                loggerConfiguration.MinimumLevel.Override(override.Key, override.Value);
            }

            // Add enrichers based on configuration
            if (loggingOptions.EnrichWithThreadId)
                loggerConfiguration.Enrich.WithThreadId();

            if (loggingOptions.EnrichWithProcessInfo)
                loggerConfiguration.Enrich.WithProcessId().Enrich.WithProcessName();

            if (loggingOptions.EnrichWithMemoryUsage)
                loggerConfiguration.Enrich.WithMemoryUsage();

            if (loggingOptions.EnrichWithEnvironment)
                loggerConfiguration.Enrich.WithEnvironmentName().Enrich.WithEnvironmentUserName();

            if (loggingOptions.EnrichWithMachineName)
                loggerConfiguration.Enrich.WithMachineName();

            // Add custom enrichers
            loggerConfiguration.Enrich.WithProperty("Application", botName)
                              .Enrich.WithProperty("Environment", environment);

            // Configure console logging if enabled
            if (loggingOptions.EnableConsoleLogging)
            {
                if (loggingOptions.UseStructuredLogging)
                {
                    loggerConfiguration.WriteTo.Console(
                        outputTemplate: loggingOptions.ConsoleOutputTemplate);
                }
                else
                {
                    loggerConfiguration.WriteTo.Console();
                }
            }

            // Configure Application Insights if connection string is available
            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                var telemetryConfiguration = new TelemetryConfiguration
                {
                    ConnectionString = appInsightsConnectionString
                };

                // Add custom telemetry initializer
                telemetryConfiguration.TelemetryInitializers.Add(
                    new ElectricRaspberryTelemetryInitializer("1.0.0", botName, environment));

                // Add Application Insights sink
                loggerConfiguration.WriteTo.ApplicationInsights(
                    telemetryConfiguration, 
                    new TraceTelemetryConverter());
            }

            // Create and return the logger
            return loggerConfiguration.CreateLogger();
        }
    }
}
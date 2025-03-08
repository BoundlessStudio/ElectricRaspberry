namespace ElectricRaspberry.Configuration
{
    /// <summary>
    /// Configuration options for the tool registry
    /// </summary>
    public class ToolRegistryOptions
    {
        public const string ConfigSection = "ToolRegistry";

        /// <summary>
        /// Whether to enable tool execution
        /// </summary>
        public bool EnableTools { get; set; } = true;

        /// <summary>
        /// Default timeout for tool execution in milliseconds
        /// </summary>
        public int DefaultTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Default stamina cost multiplier for tools
        /// </summary>
        public double StaminaCostMultiplier { get; set; } = 1.0;

        /// <summary>
        /// Maximum number of tools that can be executed in a single request
        /// </summary>
        public int MaxToolsPerRequest { get; set; } = 5;

        /// <summary>
        /// Whether to log all tool executions for auditing
        /// </summary>
        public bool LogToolExecutions { get; set; } = true;

        /// <summary>
        /// Whether to validate tool parameters before execution
        /// </summary>
        public bool ValidateParameters { get; set; } = true;

        /// <summary>
        /// Whether to auto-discover tools when initializing
        /// </summary>
        public bool AutoDiscoverTools { get; set; } = true;

        /// <summary>
        /// List of tool categories that are disabled
        /// </summary>
        public List<string> DisabledCategories { get; set; } = new List<string>();

        /// <summary>
        /// List of specific tool names that are disabled
        /// </summary>
        public List<string> DisabledTools { get; set; } = new List<string>();

        /// <summary>
        /// Whether to enable tool concurrency
        /// </summary>
        public bool EnableConcurrency { get; set; } = true;

        /// <summary>
        /// Maximum concurrent tool executions allowed
        /// </summary>
        public int MaxConcurrentExecutions { get; set; } = 3;
    }
}
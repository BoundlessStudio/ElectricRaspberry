using ElectricRaspberry.Services.Tools;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Service responsible for registering, managing, and providing access to tools that the bot can use
    /// </summary>
    public interface IToolRegistry
    {
        /// <summary>
        /// Registers a tool with the registry
        /// </summary>
        /// <param name="tool">The tool to register</param>
        void RegisterTool(IBotTool tool);

        /// <summary>
        /// Gets a tool by name
        /// </summary>
        /// <param name="name">The name of the tool to retrieve</param>
        /// <returns>The requested tool, or null if not found</returns>
        IBotTool GetTool(string name);

        /// <summary>
        /// Gets all registered tools
        /// </summary>
        /// <returns>A collection of all registered tools</returns>
        IEnumerable<IBotTool> GetAllTools();

        /// <summary>
        /// Gets all tools matching a specific category
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>A collection of tools in the specified category</returns>
        IEnumerable<IBotTool> GetToolsByCategory(string category);

        /// <summary>
        /// Gets all tools that can respond to a given capability
        /// </summary>
        /// <param name="capability">The capability to check</param>
        /// <returns>A collection of tools that support the capability</returns>
        IEnumerable<IBotTool> GetToolsByCapability(string capability);

        /// <summary>
        /// Checks if a tool with the given name exists
        /// </summary>
        /// <param name="name">The name of the tool to check</param>
        /// <returns>True if the tool exists, false otherwise</returns>
        bool HasTool(string name);

        /// <summary>
        /// Gets the total number of registered tools
        /// </summary>
        int ToolCount { get; }
    }

    /// <summary>
    /// Base interface for all bot tools
    /// </summary>
    public interface IBotTool
    {
        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a description of what the tool does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the category this tool belongs to
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Gets the capabilities this tool provides
        /// </summary>
        IEnumerable<string> Capabilities { get; }

        /// <summary>
        /// Gets whether this tool requires admin permission to use
        /// </summary>
        bool RequiresAdmin { get; }

        /// <summary>
        /// Gets the stamina cost for using this tool
        /// </summary>
        double StaminaCost { get; }

        /// <summary>
        /// Executes the tool with the given parameters
        /// </summary>
        /// <param name="context">The execution context</param>
        /// <param name="parameters">The parameters for tool execution</param>
        /// <returns>The result of the tool execution</returns>
        Task<ToolResult> ExecuteAsync(ToolExecutionContext context, Dictionary<string, object> parameters);

        /// <summary>
        /// Gets the parameter schema for this tool
        /// </summary>
        /// <returns>A dictionary describing the parameter schema</returns>
        Dictionary<string, ParameterInfo> GetParameterSchema();
    }

    /// <summary>
    /// Context for tool execution
    /// </summary>
    public class ToolExecutionContext
    {
        /// <summary>
        /// ID of the conversation where the tool is being used
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// ID of the user who triggered the tool (if applicable)
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// ID of the channel where the tool is being used
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// ID of the message that triggered the tool (if applicable)
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// The thinking context available for this tool execution
        /// </summary>
        public ThinkingContext ThinkingContext { get; set; }

        /// <summary>
        /// Whether the context includes admin privileges
        /// </summary>
        public bool HasAdminPrivileges { get; set; }

        /// <summary>
        /// The timeout for tool execution in milliseconds
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Additional metadata for the execution context
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of a tool execution
    /// </summary>
    public class ToolResult
    {
        /// <summary>
        /// Whether the tool execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result data from the tool execution
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Error message if the execution failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// How much stamina was actually consumed by the tool
        /// </summary>
        public double StaminaConsumed { get; set; }

        /// <summary>
        /// How long the tool took to execute in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Additional metadata about the execution result
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful tool result
        /// </summary>
        /// <param name="result">The result data</param>
        /// <param name="staminaConsumed">How much stamina was consumed</param>
        /// <returns>A successful tool result</returns>
        public static ToolResult CreateSuccess(object result, double staminaConsumed = 0)
        {
            return new ToolResult
            {
                Success = true,
                Result = result,
                StaminaConsumed = staminaConsumed
            };
        }

        /// <summary>
        /// Creates a failed tool result
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="staminaConsumed">How much stamina was consumed even though it failed</param>
        /// <returns>A failed tool result</returns>
        public static ToolResult CreateFailure(string errorMessage, double staminaConsumed = 0)
        {
            return new ToolResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                StaminaConsumed = staminaConsumed
            };
        }
    }

    /// <summary>
    /// Information about a tool parameter
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// Name of the parameter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the parameter
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Type of the parameter
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Whether this parameter is required
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Default value for the parameter
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Allowed values for the parameter (if applicable)
        /// </summary>
        public IEnumerable<object> AllowedValues { get; set; }

        /// <summary>
        /// Creates a new parameter info
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="description">Description of the parameter</param>
        /// <param name="type">Type of the parameter</param>
        /// <param name="required">Whether this parameter is required</param>
        /// <param name="defaultValue">Default value for the parameter</param>
        /// <param name="allowedValues">Allowed values for the parameter</param>
        /// <returns>A parameter info object</returns>
        public static ParameterInfo Create(
            string name, 
            string description, 
            string type, 
            bool required = false, 
            object defaultValue = null, 
            IEnumerable<object> allowedValues = null)
        {
            return new ParameterInfo
            {
                Name = name,
                Description = description,
                Type = type,
                Required = required,
                DefaultValue = defaultValue,
                AllowedValues = allowedValues
            };
        }
    }
}
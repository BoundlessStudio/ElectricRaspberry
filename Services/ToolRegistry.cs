using ElectricRaspberry.Configuration;
using ElectricRaspberry.Services.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Implementation of IToolRegistry for managing bot tools
    /// </summary>
    public class ToolRegistry : IToolRegistry
    {
        private readonly ILogger<ToolRegistry> _logger;
        private readonly ToolRegistryOptions _options;
        private readonly IStaminaService _staminaService;
        private readonly ConcurrentDictionary<string, IBotTool> _tools = new();
        private readonly SemaphoreSlim _executionSemaphore;

        public int ToolCount => _tools.Count;

        public ToolRegistry(
            ILogger<ToolRegistry> logger,
            IOptions<ToolRegistryOptions> options,
            IStaminaService staminaService)
        {
            _logger = logger;
            _options = options.Value;
            _staminaService = staminaService;
            _executionSemaphore = new SemaphoreSlim(_options.MaxConcurrentExecutions, _options.MaxConcurrentExecutions);

            // Auto-discover tools if enabled
            if (_options.AutoDiscoverTools)
            {
                DiscoverToolsFromAssembly();
            }
        }

        /// <inheritdoc/>
        public void RegisterTool(IBotTool tool)
        {
            if (tool == null)
            {
                throw new ArgumentNullException(nameof(tool));
            }

            // Check if the tool is disabled by configuration
            if (_options.DisabledTools.Contains(tool.Name, StringComparer.OrdinalIgnoreCase) ||
                _options.DisabledCategories.Contains(tool.Category, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Tool {toolName} is disabled by configuration and will not be registered", tool.Name);
                return;
            }

            if (_tools.TryAdd(tool.Name.ToLowerInvariant(), tool))
            {
                _logger.LogInformation("Registered tool: {toolName} (Category: {category})", tool.Name, tool.Category);
            }
            else
            {
                _logger.LogWarning("Tool with name {toolName} is already registered", tool.Name);
            }
        }

        /// <inheritdoc/>
        public IBotTool GetTool(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            _tools.TryGetValue(name.ToLowerInvariant(), out var tool);
            return tool;
        }

        /// <inheritdoc/>
        public IEnumerable<IBotTool> GetAllTools()
        {
            return _tools.Values;
        }

        /// <inheritdoc/>
        public IEnumerable<IBotTool> GetToolsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return Enumerable.Empty<IBotTool>();
            }

            return _tools.Values.Where(t => 
                string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public IEnumerable<IBotTool> GetToolsByCapability(string capability)
        {
            if (string.IsNullOrEmpty(capability))
            {
                return Enumerable.Empty<IBotTool>();
            }

            return _tools.Values.Where(t => 
                t.Capabilities.Any(c => string.Equals(c, capability, StringComparison.OrdinalIgnoreCase)));
        }

        /// <inheritdoc/>
        public bool HasTool(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return _tools.ContainsKey(name.ToLowerInvariant());
        }

        /// <summary>
        /// Executes a tool with given parameters and context
        /// </summary>
        public async Task<ToolResult> ExecuteToolAsync(
            string toolName, 
            ToolExecutionContext context, 
            Dictionary<string, object> parameters)
        {
            if (!_options.EnableTools)
            {
                return ToolResult.CreateFailure("Tool execution is disabled");
            }

            var tool = GetTool(toolName);
            if (tool == null)
            {
                return ToolResult.CreateFailure($"Tool '{toolName}' not found");
            }

            // Check admin permission if required
            if (tool.RequiresAdmin && !context.HasAdminPrivileges)
            {
                return ToolResult.CreateFailure("This tool requires administrator privileges");
            }

            // Validate parameters if enabled
            if (_options.ValidateParameters)
            {
                var validationResult = ValidateParameters(tool, parameters);
                if (!string.IsNullOrEmpty(validationResult))
                {
                    return ToolResult.CreateFailure(validationResult);
                }
            }

            // Set default timeout if not specified
            if (context.TimeoutMs <= 0)
            {
                context.TimeoutMs = _options.DefaultTimeoutMs;
            }

            // Check if we can execute (based on concurrency limits)
            if (!await TryAcquireExecutionSlotAsync(context.TimeoutMs / 2))
            {
                return ToolResult.CreateFailure("Tool execution limit reached. Try again later.");
            }

            var stopwatch = Stopwatch.StartNew();
            var staminaCost = tool.StaminaCost * _options.StaminaCostMultiplier;
            
            try
            {
                // Consume stamina before execution
                await _staminaService.ConsumeStaminaAsync(staminaCost);

                // Execute the tool with timeout
                using var cts = new CancellationTokenSource(context.TimeoutMs);
                var task = tool.ExecuteAsync(context, parameters);
                
                if (await Task.WhenAny(task, Task.Delay(context.TimeoutMs, cts.Token)) != task)
                {
                    return ToolResult.CreateFailure("Tool execution timed out", staminaCost);
                }

                var result = await task;
                
                // Add execution time to result
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                
                if (_options.LogToolExecutions)
                {
                    _logger.LogInformation(
                        "Tool {toolName} executed by {userId} in {channelId}. Success: {success}, Execution time: {executionTime}ms",
                        toolName, context.UserId, context.ChannelId, result.Success, result.ExecutionTimeMs);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {toolName}", toolName);
                
                return ToolResult.CreateFailure($"Error executing tool: {ex.Message}", staminaCost);
            }
            finally
            {
                // Release execution slot
                _executionSemaphore.Release();
            }
        }

        /// <summary>
        /// Tries to acquire an execution slot for a tool
        /// </summary>
        private async Task<bool> TryAcquireExecutionSlotAsync(int timeoutMs)
        {
            if (!_options.EnableConcurrency)
            {
                return true; // Concurrency limits disabled
            }

            try
            {
                return await _executionSemaphore.WaitAsync(timeoutMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring execution slot");
                return false;
            }
        }

        /// <summary>
        /// Validates tool parameters against the schema
        /// </summary>
        private string ValidateParameters(IBotTool tool, Dictionary<string, object> parameters)
        {
            var schema = tool.GetParameterSchema();
            
            // Check for required parameters
            foreach (var param in schema.Where(p => p.Value.Required))
            {
                if (!parameters.ContainsKey(param.Key) || parameters[param.Key] == null)
                {
                    return $"Required parameter '{param.Key}' is missing";
                }
            }

            // Check parameter types and allowed values
            foreach (var param in parameters)
            {
                if (!schema.TryGetValue(param.Key, out var paramInfo))
                {
                    return $"Unknown parameter '{param.Key}'";
                }

                // Basic type checking
                if (param.Value != null)
                {
                    var valueType = param.Value.GetType().Name.ToLowerInvariant();
                    var expectedType = paramInfo.Type.ToLowerInvariant();
                    
                    // Very basic type checking - a real implementation would have more sophisticated type conversion
                    if (!expectedType.Contains(valueType) && 
                        !valueType.Contains(expectedType) &&
                        !(expectedType == "number" && (valueType == "int32" || valueType == "double" || valueType == "single")))
                    {
                        return $"Parameter '{param.Key}' has invalid type. Expected {paramInfo.Type}, got {valueType}";
                    }
                }

                // Check allowed values if specified
                if (paramInfo.AllowedValues != null && paramInfo.AllowedValues.Any() && param.Value != null)
                {
                    if (!paramInfo.AllowedValues.Contains(param.Value))
                    {
                        return $"Parameter '{param.Key}' has invalid value. Allowed values: {string.Join(", ", paramInfo.AllowedValues)}";
                    }
                }
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Auto-discovers tools from the assembly
        /// </summary>
        private void DiscoverToolsFromAssembly()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var toolTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IBotTool).IsAssignableFrom(t));

                foreach (var toolType in toolTypes)
                {
                    try
                    {
                        // Try to create an instance
                        if (Activator.CreateInstance(toolType) is IBotTool tool)
                        {
                            RegisterTool(tool);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to initialize tool type {toolType}", toolType.Name);
                    }
                }

                _logger.LogInformation("Auto-discovered {count} tools from assembly", _tools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-discovering tools from assembly");
            }
        }
    }
}
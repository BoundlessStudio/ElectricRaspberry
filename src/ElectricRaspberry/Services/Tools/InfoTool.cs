using Microsoft.Extensions.Logging;

namespace ElectricRaspberry.Services.Tools
{
    /// <summary>
    /// A simple tool that provides information about the bot and its environment
    /// </summary>
    public class InfoTool : IBotTool
    {
        public string Name => "Info";
        
        public string Description => "Provides information about the bot and its environment";
        
        public string Category => "Utility";
        
        public IEnumerable<string> Capabilities => new[] { "information", "help" };
        
        public bool RequiresAdmin => false;
        
        public double StaminaCost => 0.5;

        /// <inheritdoc/>
        public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, Dictionary<string, object> parameters)
        {
            try
            {
                // Get the requested information type
                var infoType = parameters.ContainsKey("type") 
                    ? parameters["type"].ToString().ToLowerInvariant() 
                    : "bot";

                // Get optional format parameter
                var format = parameters.ContainsKey("format")
                    ? parameters["format"].ToString().ToLowerInvariant()
                    : "text";

                // Get information based on requested type
                object result = infoType switch
                {
                    "bot" => await GetBotInfoAsync(context),
                    "conversation" => await GetConversationInfoAsync(context),
                    "channel" => await GetChannelInfoAsync(context),
                    "user" => await GetUserInfoAsync(context, 
                        parameters.ContainsKey("userId") ? parameters["userId"].ToString() : context.UserId),
                    "environment" => await GetEnvironmentInfoAsync(context),
                    _ => new Dictionary<string, string> { { "error", $"Unknown info type: {infoType}" } }
                };

                // Format the result based on requested format
                if (format == "json")
                {
                    // Result is already an object suitable for JSON
                    return ToolResult.CreateSuccess(result, StaminaCost);
                }
                else
                {
                    // Format as text
                    string textResult = FormatAsText(result, infoType);
                    return ToolResult.CreateSuccess(textResult, StaminaCost);
                }
            }
            catch (Exception ex)
            {
                return ToolResult.CreateFailure($"Error retrieving information: {ex.Message}", StaminaCost / 2);
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, ParameterInfo> GetParameterSchema()
        {
            return new Dictionary<string, ParameterInfo>
            {
                {
                    "type", 
                    ParameterInfo.Create(
                        "type", 
                        "Type of information to retrieve", 
                        "string", 
                        false, 
                        "bot", 
                        new[] { "bot", "conversation", "channel", "user", "environment" })
                },
                {
                    "format", 
                    ParameterInfo.Create(
                        "format", 
                        "Format of the returned information", 
                        "string", 
                        false, 
                        "text", 
                        new[] { "text", "json" })
                },
                {
                    "userId", 
                    ParameterInfo.Create(
                        "userId", 
                        "ID of the user to get information about (only for type=user)", 
                        "string", 
                        false)
                }
            };
        }

        private async Task<Dictionary<string, object>> GetBotInfoAsync(ToolExecutionContext context)
        {
            var result = new Dictionary<string, object>
            {
                { "name", "ElectricRaspberry" },
                { "version", "1.0.0" },
                { "uptime", DateTime.UtcNow.ToString("o") }, // Simulated uptime
                { "status", "online" }
            };

            if (context.ThinkingContext?.BotEmotionalState != null)
            {
                result["mood"] = context.ThinkingContext.BotEmotionalState.GetDominantEmotion().ToString();
                result["emotionalState"] = context.ThinkingContext.BotEmotionalState;
            }

            return result;
        }

        private async Task<Dictionary<string, object>> GetConversationInfoAsync(ToolExecutionContext context)
        {
            var result = new Dictionary<string, object>
            {
                { "id", context.ConversationId ?? "unknown" },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            if (context.ThinkingContext != null)
            {
                result["state"] = context.ThinkingContext.State.ToString();
                result["messageCount"] = context.ThinkingContext.RecentMessages?.Count ?? 0;
                result["userCount"] = context.ThinkingContext.Users?.Count ?? 0;
                
                if (context.ThinkingContext.RecentMessages?.Any() == true)
                {
                    result["lastMessageTime"] = context.ThinkingContext.RecentMessages[0].Timestamp.ToString("o");
                }
            }

            return result;
        }

        private async Task<Dictionary<string, object>> GetChannelInfoAsync(ToolExecutionContext context)
        {
            var result = new Dictionary<string, object>
            {
                { "id", context.ChannelId ?? "unknown" }
            };

            if (context.ThinkingContext?.Environment != null)
            {
                var env = context.ThinkingContext.Environment;
                result["name"] = env.ChannelName;
                result["type"] = env.ChannelType;
                result["userCount"] = env.UserCount;
                result["server"] = env.ServerName;
                result["serverId"] = env.ServerId;
                result["isPrivate"] = env.IsPrivate;
                result["lastActiveTime"] = env.LastActiveTime.ToString("o");
            }

            return result;
        }

        private async Task<Dictionary<string, object>> GetUserInfoAsync(ToolExecutionContext context, string userId)
        {
            var result = new Dictionary<string, object>
            {
                { "id", userId ?? "unknown" }
            };

            if (context.ThinkingContext?.Users != null && 
                !string.IsNullOrEmpty(userId) && 
                context.ThinkingContext.Users.TryGetValue(userId, out var userContext))
            {
                result["name"] = userContext.Username;
                result["relationshipStrength"] = userContext.RelationshipStrength;
                result["interactionCount"] = userContext.RecentInteractionCount;
                result["isAdmin"] = userContext.IsAdmin;
                
                // Top interests
                if (userContext.Interests?.Any() == true)
                {
                    result["interests"] = userContext.Interests
                        .OrderByDescending(i => i.Value)
                        .Take(5)
                        .ToDictionary(i => i.Key, i => i.Value);
                }
            }

            return result;
        }

        private async Task<Dictionary<string, object>> GetEnvironmentInfoAsync(ToolExecutionContext context)
        {
            return new Dictionary<string, object>
            {
                { "time", DateTime.UtcNow.ToString("o") },
                { "timeZone", "UTC" },
                { "platform", Environment.OSVersion.ToString() }
            };
        }

        private string FormatAsText(object result, string infoType)
        {
            // Simple formatting for text output
            if (result is Dictionary<string, object> dict)
            {
                var builder = new System.Text.StringBuilder();
                builder.AppendLine($"--- {infoType.ToUpperInvariant()} INFORMATION ---");
                
                foreach (var item in dict)
                {
                    // Skip complex objects in text format
                    if (item.Value is not Dictionary<string, object> && 
                        item.Value is not IEnumerable<object>)
                    {
                        builder.AppendLine($"{FormatPropertyName(item.Key)}: {item.Value}");
                    }
                    else if (item.Value is Dictionary<string, double> interests)
                    {
                        builder.AppendLine($"{FormatPropertyName(item.Key)}:");
                        foreach (var interest in interests)
                        {
                            builder.AppendLine($"  - {interest.Key}: {interest.Value:F2}");
                        }
                    }
                }
                
                return builder.ToString();
            }
            
            return result?.ToString() ?? "No information available";
        }

        private string FormatPropertyName(string propertyName)
        {
            // Convert camelCase or snake_case to Title Case
            if (string.IsNullOrEmpty(propertyName))
                return string.Empty;
                
            // Replace underscores with spaces
            var result = propertyName.Replace('_', ' ');
            
            // Insert spaces before capital letters
            result = System.Text.RegularExpressions.Regex.Replace(
                result, 
                "([a-z])([A-Z])", 
                "$1 $2");
                
            // Capitalize first letter of each word
            var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(result.ToLower());
        }
    }
}
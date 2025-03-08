using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Emotions;

namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Service responsible for AI-powered thinking and reasoning
    /// </summary>
    public interface IAIEngine
    {
        /// <summary>
        /// Analyzes a conversation and generates a response
        /// </summary>
        /// <param name="thinkingContext">The context for thinking</param>
        /// <param name="options">Options for the thinking process</param>
        /// <returns>The result of the thinking process</returns>
        Task<ThinkingResult> ThinkAsync(ThinkingContext thinkingContext, ThinkingOptions options = null);

        /// <summary>
        /// Analyzes a message for emotional impact
        /// </summary>
        /// <param name="message">The message to analyze</param>
        /// <param name="currentEmotionalState">The current emotional state</param>
        /// <returns>The emotional impact of the message</returns>
        Task<EmotionalImpact> AnalyzeEmotionalImpactAsync(string message, EmotionalState currentEmotionalState);

        /// <summary>
        /// Generates a response to a message
        /// </summary>
        /// <param name="thinkingContext">The context for generation</param>
        /// <param name="thinkingResult">The result of the thinking process</param>
        /// <param name="options">Options for the generation process</param>
        /// <returns>The generated response</returns>
        Task<GeneratedResponse> GenerateResponseAsync(
            ThinkingContext thinkingContext, 
            ThinkingResult thinkingResult, 
            ResponseGenerationOptions options = null);

        /// <summary>
        /// Extracts topics and entities from text
        /// </summary>
        /// <param name="text">The text to analyze</param>
        /// <returns>Extracted topics and entities</returns>
        Task<TopicExtractionResult> ExtractTopicsAsync(string text);

        /// <summary>
        /// Creates a memory entry from a conversation
        /// </summary>
        /// <param name="conversation">The conversation to create a memory from</param>
        /// <param name="importance">The importance of the memory (0.0-1.0)</param>
        /// <returns>The created memory entry</returns>
        Task<MemoryEntry> CreateMemoryAsync(Conversation conversation, double importance);

        /// <summary>
        /// Suggests actions based on the thinking result
        /// </summary>
        /// <param name="thinkingResult">The result of the thinking process</param>
        /// <param name="availableTools">The available tools</param>
        /// <returns>Suggested actions to take</returns>
        Task<ActionPlan> SuggestActionsAsync(ThinkingResult thinkingResult, IEnumerable<IBotTool> availableTools);
    }

    /// <summary>
    /// Options for the thinking process
    /// </summary>
    public class ThinkingOptions
    {
        /// <summary>
        /// The maximum thinking depth (number of recursive thinking steps)
        /// </summary>
        public int MaxThinkingDepth { get; set; } = 3;

        /// <summary>
        /// Whether to use tools during thinking
        /// </summary>
        public bool UseTools { get; set; } = true;

        /// <summary>
        /// The maximum number of tool calls during thinking
        /// </summary>
        public int MaxToolCalls { get; set; } = 3;

        /// <summary>
        /// The system prompt to use for thinking
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// The creativity level (0.0-1.0, where higher values mean more creative responses)
        /// </summary>
        public double CreativityLevel { get; set; } = 0.7;

        /// <summary>
        /// The maximum tokens to generate in the thinking output
        /// </summary>
        public int MaxTokens { get; set; } = 1000;

        /// <summary>
        /// Whether to include emotional analysis in thinking
        /// </summary>
        public bool IncludeEmotionalAnalysis { get; set; } = true;

        /// <summary>
        /// Whether to include reasoning steps in the output
        /// </summary>
        public bool IncludeReasoning { get; set; } = true;
    }

    /// <summary>
    /// Options for response generation
    /// </summary>
    public class ResponseGenerationOptions
    {
        /// <summary>
        /// The style of the response
        /// </summary>
        public Dictionary<string, object> StyleAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The maximum length of the response in characters
        /// </summary>
        public int MaxLength { get; set; } = 2000;

        /// <summary>
        /// The creativity level (0.0-1.0, where higher values mean more creative responses)
        /// </summary>
        public double CreativityLevel { get; set; } = 0.7;

        /// <summary>
        /// Whether to include emojis in the response
        /// </summary>
        public bool UseEmojis { get; set; } = true;

        /// <summary>
        /// Whether to format the response using Markdown
        /// </summary>
        public bool UseMarkdown { get; set; } = true;

        /// <summary>
        /// Whether to mention the user in the response
        /// </summary>
        public bool MentionUser { get; set; } = false;
    }

    /// <summary>
    /// The result of a thinking process
    /// </summary>
    public class ThinkingResult
    {
        /// <summary>
        /// The primary response message
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// The reasoning behind the response
        /// </summary>
        public string Reasoning { get; set; }

        /// <summary>
        /// The emotional state that influenced the response
        /// </summary>
        public EmotionalState EmotionalState { get; set; }

        /// <summary>
        /// Tools that were used during thinking
        /// </summary>
        public List<ToolUse> ToolsUsed { get; set; } = new List<ToolUse>();

        /// <summary>
        /// Topics detected in the conversation
        /// </summary>
        public List<string> DetectedTopics { get; set; } = new List<string>();

        /// <summary>
        /// Whether the response requires action beyond just replying
        /// </summary>
        public bool RequiresAction { get; set; }

        /// <summary>
        /// Whether the response should be sent
        /// </summary>
        public bool ShouldRespond { get; set; } = true;

        /// <summary>
        /// Priority of the response (0.0-1.0, where higher values mean higher priority)
        /// </summary>
        public double Priority { get; set; } = 0.5;

        /// <summary>
        /// Metadata about the thinking process
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a tool use during thinking
    /// </summary>
    public class ToolUse
    {
        /// <summary>
        /// The name of the tool
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// The parameters used with the tool
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// The result of the tool execution
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Whether the tool execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// How much stamina was consumed by this tool use
        /// </summary>
        public double StaminaConsumed { get; set; }
    }

    /// <summary>
    /// A generated response
    /// </summary>
    public class GeneratedResponse
    {
        /// <summary>
        /// The content of the response
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The emotional expression conveyed by the response
        /// </summary>
        public EmotionalExpression EmotionalExpression { get; set; }

        /// <summary>
        /// Whether the response should mention the user
        /// </summary>
        public bool MentionsUser { get; set; }

        /// <summary>
        /// The timestamp when the response was generated
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the response contains a question
        /// </summary>
        public bool ContainsQuestion { get; set; }

        /// <summary>
        /// Whether the response is a greeting
        /// </summary>
        public bool IsGreeting { get; set; }

        /// <summary>
        /// Whether the response is a farewell
        /// </summary>
        public bool IsFarewell { get; set; }

        /// <summary>
        /// The style attributes used to generate the response
        /// </summary>
        public Dictionary<string, object> StyleAttributes { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of topic extraction
    /// </summary>
    public class TopicExtractionResult
    {
        /// <summary>
        /// The primary topics extracted from the text
        /// </summary>
        public List<string> PrimaryTopics { get; set; } = new List<string>();

        /// <summary>
        /// Secondary or related topics
        /// </summary>
        public List<string> RelatedTopics { get; set; } = new List<string>();

        /// <summary>
        /// Entities mentioned in the text (people, places, organizations, etc.)
        /// </summary>
        public List<string> Entities { get; set; } = new List<string>();

        /// <summary>
        /// The sentiment of the text (positive, negative, neutral)
        /// </summary>
        public string Sentiment { get; set; }

        /// <summary>
        /// The confidence in the extraction (0.0-1.0)
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// A memory entry created from a conversation
    /// </summary>
    public class MemoryEntry
    {
        /// <summary>
        /// The unique identifier for this memory
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The content of the memory
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// When the memory was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The importance of the memory (0.0-1.0)
        /// </summary>
        public double Importance { get; set; } = 0.5;

        /// <summary>
        /// The topics related to this memory
        /// </summary>
        public List<string> Topics { get; set; } = new List<string>();

        /// <summary>
        /// The user IDs associated with this memory
        /// </summary>
        public List<string> UserIds { get; set; } = new List<string>();

        /// <summary>
        /// The conversation ID this memory is from
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Metadata about the memory
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// A plan for actions to take
    /// </summary>
    public class ActionPlan
    {
        /// <summary>
        /// The steps in the action plan
        /// </summary>
        public List<ActionStep> Steps { get; set; } = new List<ActionStep>();

        /// <summary>
        /// The goal of the action plan
        /// </summary>
        public string Goal { get; set; }

        /// <summary>
        /// The reasoning behind the action plan
        /// </summary>
        public string Reasoning { get; set; }

        /// <summary>
        /// The priority of the action plan (0.0-1.0, where higher values mean higher priority)
        /// </summary>
        public double Priority { get; set; } = 0.5;
    }

    /// <summary>
    /// A step in an action plan
    /// </summary>
    public class ActionStep
    {
        /// <summary>
        /// The type of action to take
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// The tool to use for this action
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// The parameters for the tool
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The expected result of this action
        /// </summary>
        public string ExpectedResult { get; set; }

        /// <summary>
        /// The fallback action if this action fails
        /// </summary>
        public ActionStep Fallback { get; set; }
    }
}
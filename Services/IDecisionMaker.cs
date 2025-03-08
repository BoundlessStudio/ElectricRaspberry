namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Service responsible for making decisions about how to respond to events
    /// </summary>
    public interface IDecisionMaker
    {
        /// <summary>
        /// Decides whether to respond to a message
        /// </summary>
        /// <param name="conversationId">The ID of the conversation</param>
        /// <returns>The decision result</returns>
        Task<DecisionResult> ShouldRespondAsync(string conversationId);

        /// <summary>
        /// Decides whether to initiate a conversation
        /// </summary>
        /// <param name="channelId">The ID of the channel</param>
        /// <returns>The decision result</returns>
        Task<DecisionResult> ShouldInitiateConversationAsync(string channelId);

        /// <summary>
        /// Decides whether to use a specific tool
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="conversationId">The ID of the conversation</param>
        /// <returns>The decision result</returns>
        Task<DecisionResult> ShouldUseToolAsync(string toolName, string conversationId);

        /// <summary>
        /// Decides whether to enter sleep mode
        /// </summary>
        /// <returns>The decision result</returns>
        Task<DecisionResult> ShouldEnterSleepModeAsync();

        /// <summary>
        /// Decides response style and parameters
        /// </summary>
        /// <param name="conversationId">The ID of the conversation</param>
        /// <returns>The style decision</returns>
        Task<StyleDecision> DecideResponseStyleAsync(string conversationId);

        /// <summary>
        /// Evaluates a thinking result and makes a decision about how to proceed
        /// </summary>
        /// <param name="thinkingResult">The thinking result to evaluate</param>
        /// <param name="conversationId">The ID of the conversation</param>
        /// <returns>The evaluation result</returns>
        Task<ThinkingEvaluation> EvaluateThinkingResultAsync(ThinkingResult thinkingResult, string conversationId);
    }

    /// <summary>
    /// The result of a decision
    /// </summary>
    public class DecisionResult
    {
        /// <summary>
        /// Whether the decision is affirmative
        /// </summary>
        public bool ShouldProceed { get; set; }

        /// <summary>
        /// The confidence in the decision (0.0-1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// The reasoning behind the decision
        /// </summary>
        public string Reasoning { get; set; }

        /// <summary>
        /// The delay to wait before proceeding, in milliseconds
        /// </summary>
        public int DelayMs { get; set; }

        /// <summary>
        /// Factors that influenced the decision
        /// </summary>
        public Dictionary<string, object> Factors { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates an affirmative decision result
        /// </summary>
        /// <param name="confidence">The confidence in the decision</param>
        /// <param name="reasoning">The reasoning behind the decision</param>
        /// <param name="delayMs">The delay to wait before proceeding</param>
        /// <returns>The decision result</returns>
        public static DecisionResult Yes(double confidence, string reasoning = null, int delayMs = 0)
        {
            return new DecisionResult
            {
                ShouldProceed = true,
                Confidence = confidence,
                Reasoning = reasoning,
                DelayMs = delayMs
            };
        }

        /// <summary>
        /// Creates a negative decision result
        /// </summary>
        /// <param name="confidence">The confidence in the decision</param>
        /// <param name="reasoning">The reasoning behind the decision</param>
        /// <returns>The decision result</returns>
        public static DecisionResult No(double confidence, string reasoning = null)
        {
            return new DecisionResult
            {
                ShouldProceed = false,
                Confidence = confidence,
                Reasoning = reasoning
            };
        }
    }

    /// <summary>
    /// Decision about the style of a response
    /// </summary>
    public class StyleDecision
    {
        /// <summary>
        /// The decided style attributes
        /// </summary>
        public Dictionary<string, object> StyleAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The confidence in the decision (0.0-1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Factors that influenced the decision
        /// </summary>
        public Dictionary<string, object> Factors { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Evaluation of a thinking result
    /// </summary>
    public class ThinkingEvaluation
    {
        /// <summary>
        /// Whether the thinking result is approved for use
        /// </summary>
        public bool Approved { get; set; }

        /// <summary>
        /// The confidence in the evaluation (0.0-1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// The reasoning behind the evaluation
        /// </summary>
        public string Reasoning { get; set; }

        /// <summary>
        /// Recommended actions to take based on the thinking result
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new List<string>();

        /// <summary>
        /// Factors that influenced the evaluation
        /// </summary>
        public Dictionary<string, object> Factors { get; set; } = new Dictionary<string, object>();
    }
}
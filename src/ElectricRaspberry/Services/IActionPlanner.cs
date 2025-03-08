namespace ElectricRaspberry.Services
{
    /// <summary>
    /// Service responsible for planning and executing actions based on thinking results
    /// </summary>
    public interface IActionPlanner
    {
        /// <summary>
        /// Creates an action plan based on a thinking result
        /// </summary>
        /// <param name="thinkingResult">The thinking result to create a plan from</param>
        /// <param name="thinkingContext">The context that was used for thinking</param>
        /// <returns>The created action plan</returns>
        Task<ActionPlan> CreateActionPlanAsync(ThinkingResult thinkingResult, ThinkingContext thinkingContext);

        /// <summary>
        /// Executes an action plan
        /// </summary>
        /// <param name="actionPlan">The action plan to execute</param>
        /// <param name="thinkingContext">The context for execution</param>
        /// <returns>The execution result</returns>
        Task<ActionExecutionResult> ExecuteActionPlanAsync(ActionPlan actionPlan, ThinkingContext thinkingContext);

        /// <summary>
        /// Creates and executes an action plan in one step
        /// </summary>
        /// <param name="thinkingResult">The thinking result to create a plan from</param>
        /// <param name="thinkingContext">The context for execution</param>
        /// <returns>The execution result</returns>
        Task<ActionExecutionResult> PlanAndExecuteAsync(ThinkingResult thinkingResult, ThinkingContext thinkingContext);

        /// <summary>
        /// Executes a specific action step
        /// </summary>
        /// <param name="actionStep">The action step to execute</param>
        /// <param name="thinkingContext">The context for execution</param>
        /// <returns>The execution result</returns>
        Task<StepExecutionResult> ExecuteStepAsync(ActionStep actionStep, ThinkingContext thinkingContext);

        /// <summary>
        /// Evaluates whether an action plan was successful
        /// </summary>
        /// <param name="actionPlan">The original action plan</param>
        /// <param name="executionResult">The execution result</param>
        /// <returns>The evaluation result</returns>
        Task<ActionEvaluation> EvaluateActionPlanAsync(ActionPlan actionPlan, ActionExecutionResult executionResult);
    }

    /// <summary>
    /// The result of executing an action plan
    /// </summary>
    public class ActionExecutionResult
    {
        /// <summary>
        /// Whether the execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The results of executing each step
        /// </summary>
        public List<StepExecutionResult> StepResults { get; set; } = new List<StepExecutionResult>();

        /// <summary>
        /// The overall message from the execution
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// How much stamina was consumed by the execution
        /// </summary>
        public double StaminaConsumed { get; set; }

        /// <summary>
        /// How long the execution took in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Whether the execution had side effects
        /// </summary>
        public bool HadSideEffects { get; set; }

        /// <summary>
        /// Whether the execution requires follow-up
        /// </summary>
        public bool RequiresFollowUp { get; set; }

        /// <summary>
        /// Creates a successful execution result
        /// </summary>
        /// <param name="message">The success message</param>
        /// <param name="staminaConsumed">How much stamina was consumed</param>
        /// <param name="executionTimeMs">How long the execution took</param>
        /// <returns>The execution result</returns>
        public static ActionExecutionResult CreateSuccess(string message, double staminaConsumed, long executionTimeMs)
        {
            return new ActionExecutionResult
            {
                Success = true,
                Message = message,
                StaminaConsumed = staminaConsumed,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Creates a failed execution result
        /// </summary>
        /// <param name="message">The failure message</param>
        /// <param name="staminaConsumed">How much stamina was consumed despite failure</param>
        /// <returns>The execution result</returns>
        public static ActionExecutionResult CreateFailure(string message, double staminaConsumed = 0)
        {
            return new ActionExecutionResult
            {
                Success = false,
                Message = message,
                StaminaConsumed = staminaConsumed
            };
        }
    }

    /// <summary>
    /// The result of executing a single action step
    /// </summary>
    public class StepExecutionResult
    {
        /// <summary>
        /// Whether the step execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result of the step execution
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// The error message if the execution failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// How much stamina was consumed by the step
        /// </summary>
        public double StaminaConsumed { get; set; }

        /// <summary>
        /// How long the step took to execute in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Whether a fallback was used
        /// </summary>
        public bool UsedFallback { get; set; }

        /// <summary>
        /// Creates a successful step execution result
        /// </summary>
        /// <param name="result">The execution result</param>
        /// <param name="staminaConsumed">How much stamina was consumed</param>
        /// <param name="executionTimeMs">How long the execution took</param>
        /// <returns>The step execution result</returns>
        public static StepExecutionResult CreateSuccess(object result, double staminaConsumed, long executionTimeMs)
        {
            return new StepExecutionResult
            {
                Success = true,
                Result = result,
                StaminaConsumed = staminaConsumed,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Creates a failed step execution result
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="staminaConsumed">How much stamina was consumed despite failure</param>
        /// <returns>The step execution result</returns>
        public static StepExecutionResult CreateFailure(string errorMessage, double staminaConsumed = 0)
        {
            return new StepExecutionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                StaminaConsumed = staminaConsumed
            };
        }
    }

    /// <summary>
    /// Evaluation of an action plan execution
    /// </summary>
    public class ActionEvaluation
    {
        /// <summary>
        /// Whether the action plan execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The confidence in the evaluation (0.0-1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Achievements of the action plan
        /// </summary>
        public List<string> Achievements { get; set; } = new List<string>();

        /// <summary>
        /// Issues with the action plan execution
        /// </summary>
        public List<string> Issues { get; set; } = new List<string>();

        /// <summary>
        /// Lessons learned from the execution
        /// </summary>
        public List<string> LessonsLearned { get; set; } = new List<string>();

        /// <summary>
        /// Recommended follow-up actions
        /// </summary>
        public List<ActionStep> RecommendedFollowUps { get; set; } = new List<ActionStep>();
    }
}
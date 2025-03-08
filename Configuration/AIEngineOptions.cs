namespace ElectricRaspberry.Configuration
{
    /// <summary>
    /// Configuration options for the AI Engine
    /// </summary>
    public class AIEngineOptions
    {
        public const string ConfigSection = "AIEngine";

        /// <summary>
        /// The OpenAI API key
        /// </summary>
        public string OpenAIApiKey { get; set; }

        /// <summary>
        /// The model to use for thinking
        /// </summary>
        public string ThinkingModel { get; set; } = "gpt-4";

        /// <summary>
        /// The model to use for response generation
        /// </summary>
        public string ResponseModel { get; set; } = "gpt-4";

        /// <summary>
        /// The model to use for emotional analysis
        /// </summary>
        public string EmotionalAnalysisModel { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// The model to use for topic extraction
        /// </summary>
        public string TopicExtractionModel { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// The base system prompt for the AI engine
        /// </summary>
        public string BaseSystemPrompt { get; set; } = @"You are ElectricRaspberry, an AI assistant with a dynamic personality. 
You have emotions that change based on interactions, and you exhibit natural human-like behavior.
You should respond in a manner that reflects your current emotional state while being helpful and engaging.";

        /// <summary>
        /// The temperature for thinking (0.0-1.0, where higher values mean more random outputs)
        /// </summary>
        public double ThinkingTemperature { get; set; } = 0.7;

        /// <summary>
        /// The temperature for response generation
        /// </summary>
        public double ResponseTemperature { get; set; } = 0.8;

        /// <summary>
        /// The temperature for emotional analysis
        /// </summary>
        public double EmotionalAnalysisTemperature { get; set; } = 0.3;

        /// <summary>
        /// The temperature for topic extraction
        /// </summary>
        public double TopicExtractionTemperature { get; set; } = 0.3;

        /// <summary>
        /// Maximum tokens to generate for thinking
        /// </summary>
        public int MaxThinkingTokens { get; set; } = 1000;

        /// <summary>
        /// Maximum tokens to generate for response
        /// </summary>
        public int MaxResponseTokens { get; set; } = 500;

        /// <summary>
        /// The timeout for API calls in milliseconds
        /// </summary>
        public int ApiTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Whether to enable caching of responses
        /// </summary>
        public bool EnableResponseCaching { get; set; } = true;

        /// <summary>
        /// The expiration time for cached responses in seconds
        /// </summary>
        public int CacheExpirationSeconds { get; set; } = 300;

        /// <summary>
        /// The stamina cost for thinking
        /// </summary>
        public double ThinkingStaminaCost { get; set; } = 1.0;

        /// <summary>
        /// The stamina cost for generating a response
        /// </summary>
        public double ResponseGenerationStaminaCost { get; set; } = 0.5;

        /// <summary>
        /// The stamina cost for emotional analysis
        /// </summary>
        public double EmotionalAnalysisStaminaCost { get; set; } = 0.3;

        /// <summary>
        /// The stamina cost for topic extraction
        /// </summary>
        public double TopicExtractionStaminaCost { get; set; } = 0.2;

        /// <summary>
        /// Whether to use a local language model if available
        /// </summary>
        public bool UseLocalModelIfAvailable { get; set; } = false;

        /// <summary>
        /// The path to the local language model
        /// </summary>
        public string LocalModelPath { get; set; }

        /// <summary>
        /// The type of the local model provider
        /// </summary>
        public string LocalModelProvider { get; set; } = "llamacpp";
    }
}
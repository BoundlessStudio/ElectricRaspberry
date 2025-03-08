namespace ElectricRaspberry.Configuration
{
    /// <summary>
    /// Configuration options for the bot's personality system
    /// </summary>
    public class PersonalityOptions
    {
        public const string ConfigSection = "Personality";

        /// <summary>
        /// Personality adaptation rate (0.0-1.0, higher values mean faster adaptation)
        /// </summary>
        public double AdaptationRate { get; set; } = 0.05;

        /// <summary>
        /// Minimum value for personality traits
        /// </summary>
        public double MinTraitValue { get; set; } = 0.1;

        /// <summary>
        /// Maximum value for personality traits
        /// </summary>
        public double MaxTraitValue { get; set; } = 1.0;

        /// <summary>
        /// Base probability of initiating conversation (0.0-1.0)
        /// </summary>
        public double BaseInitiationProbability { get; set; } = 0.2;

        /// <summary>
        /// Probability multiplier for each personality trait that affects initiation
        /// </summary>
        public Dictionary<string, double> InitiationTraitMultipliers { get; set; } = new Dictionary<string, double>
        {
            { "Extroverted", 1.5 },
            { "Friendly", 1.3 },
            { "Curious", 1.2 },
            { "Shy", 0.5 },
            { "Reserved", 0.7 }
        };

        /// <summary>
        /// Base probability of responding to a message (0.0-1.0)
        /// </summary>
        public double BaseResponseProbability { get; set; } = 0.8;

        /// <summary>
        /// Minimum time to wait before initiating conversation (in seconds)
        /// </summary>
        public int MinInitiationDelaySeconds { get; set; } = 60;

        /// <summary>
        /// Maximum time to wait before initiating conversation (in seconds)
        /// </summary>
        public int MaxInitiationDelaySeconds { get; set; } = 300;

        /// <summary>
        /// Minimum time to wait before responding to a message (in milliseconds)
        /// </summary>
        public int MinResponseDelayMs { get; set; } = 500;

        /// <summary>
        /// Maximum time to wait before responding to a message (in milliseconds)
        /// </summary>
        public int MaxResponseDelayMs { get; set; } = 3000;

        /// <summary>
        /// Threshold for considering a conversation dormant (in minutes)
        /// </summary>
        public int DormantConversationThresholdMinutes { get; set; } = 10;

        /// <summary>
        /// Emotional impact threshold for personality adaptation (0.0-1.0)
        /// </summary>
        public double EmotionalImpactThreshold { get; set; } = 0.3;

        /// <summary>
        /// Map of how emotional states affect personality traits
        /// </summary>
        public Dictionary<string, Dictionary<string, double>> EmotionalPersonalityImpacts { get; set; } = new Dictionary<string, Dictionary<string, double>>
        {
            { 
                "Joy", new Dictionary<string, double> 
                { 
                    { "Friendly", 0.1 }, 
                    { "Optimistic", 0.08 },
                    { "Reserved", -0.05 }
                } 
            },
            { 
                "Sadness", new Dictionary<string, double> 
                { 
                    { "Empathetic", 0.1 }, 
                    { "Reserved", 0.08 },
                    { "Extroverted", -0.05 }
                } 
            },
            { 
                "Anger", new Dictionary<string, double> 
                { 
                    { "Assertive", 0.1 }, 
                    { "Patient", -0.08 }
                } 
            },
            { 
                "Fear", new Dictionary<string, double> 
                { 
                    { "Cautious", 0.1 }, 
                    { "Bold", -0.08 }
                } 
            },
            { 
                "Surprise", new Dictionary<string, double> 
                { 
                    { "Curious", 0.1 }, 
                    { "Adaptable", 0.08 }
                } 
            }
        };
    }
}
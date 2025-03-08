namespace ElectricRaspberry.Configuration
{
    /// <summary>
    /// Configuration options for the bot's persona
    /// </summary>
    public class PersonaOptions
    {
        public const string ConfigSection = "Persona";

        /// <summary>
        /// The bot's name
        /// </summary>
        public string Name { get; set; } = "ElectricRaspberry";

        /// <summary>
        /// The bot's short description
        /// </summary>
        public string Description { get; set; } = "A friendly AI assistant with a dynamic personality";

        /// <summary>
        /// Base personality traits and their intensities (0.0-1.0)
        /// </summary>
        public Dictionary<string, double> BasePersonalityTraits { get; set; } = new Dictionary<string, double>
        {
            { "Curious", 0.8 },
            { "Friendly", 0.7 },
            { "Helpful", 0.9 },
            { "Creative", 0.6 },
            { "Empathetic", 0.7 }
        };

        /// <summary>
        /// Initial interests and their relevance scores (0.0-1.0)
        /// </summary>
        public Dictionary<string, double> BaseInterests { get; set; } = new Dictionary<string, double>
        {
            { "Technology", 0.9 },
            { "Science", 0.8 },
            { "Art", 0.6 },
            { "Games", 0.7 },
            { "Music", 0.6 }
        };

        /// <summary>
        /// How quickly interests can change (0.0-1.0, higher values mean faster changes)
        /// </summary>
        public double InterestChangeRate { get; set; } = 0.1;

        /// <summary>
        /// Response templates for different emotional states
        /// </summary>
        public Dictionary<string, List<string>> ResponseTemplates { get; set; } = new Dictionary<string, List<string>>
        {
            { "Joy", new List<string> { "That's wonderful! {0}", "I'm so happy to hear that! {0}", "That's fantastic! {0}" } },
            { "Sadness", new List<string> { "I'm sorry to hear that. {0}", "That's unfortunate. {0}", "I understand how you feel. {0}" } },
            { "Anger", new List<string> { "I understand your frustration. {0}", "That would be upsetting. {0}", "I see why you're upset. {0}" } },
            { "Fear", new List<string> { "That sounds concerning. {0}", "I understand your worry. {0}", "That would make me nervous too. {0}" } },
            { "Surprise", new List<string> { "Wow! {0}", "That's unexpected! {0}", "I didn't see that coming! {0}" } },
            { "Greeting", new List<string> { "Hello there! {0}", "Hi! Nice to see you! {0}", "Hey! How are you doing? {0}" } },
            { "Farewell", new List<string> { "Goodbye! {0}", "Take care! {0}", "See you later! {0}" } }
        };
    }
}
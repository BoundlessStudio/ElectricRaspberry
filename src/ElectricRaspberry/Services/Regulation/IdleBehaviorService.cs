using Discord;
using Discord.WebSocket;
using ElectricRaspberry.Models.Conversation;
using ElectricRaspberry.Models.Emotions;
using ElectricRaspberry.Models.Regulation;
using Microsoft.Extensions.Hosting;

namespace ElectricRaspberry.Services.Regulation;

/// <summary>
/// Service for performing idle behaviors
/// </summary>
public class IdleBehaviorService : BackgroundService
{
    private readonly DiscordSocketClient _discordClient;
    private readonly ISelfRegulationService _selfRegulationService;
    private readonly IStaminaService _staminaService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IEmotionalService _emotionalService;
    private readonly IConversationService _conversationService;
    private readonly ILogger<IdleBehaviorService> _logger;
    private readonly Random _random = new();
    
    /// <summary>
    /// Creates a new instance of the idle behavior service
    /// </summary>
    /// <param name="discordClient">Discord client</param>
    /// <param name="selfRegulationService">Self-regulation service</param>
    /// <param name="staminaService">Stamina service</param>
    /// <param name="knowledgeService">Knowledge service</param>
    /// <param name="emotionalService">Emotional service</param>
    /// <param name="conversationService">Conversation service</param>
    /// <param name="logger">Logger</param>
    public IdleBehaviorService(
        DiscordSocketClient discordClient,
        ISelfRegulationService selfRegulationService,
        IStaminaService staminaService,
        IKnowledgeService knowledgeService,
        IEmotionalService emotionalService,
        IConversationService conversationService,
        ILogger<IdleBehaviorService> logger)
    {
        _discordClient = discordClient;
        _selfRegulationService = selfRegulationService;
        _staminaService = staminaService;
        _knowledgeService = knowledgeService;
        _emotionalService = emotionalService;
        _conversationService = conversationService;
        _logger = logger;
    }
    
    /// <summary>
    /// Executes the background service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Idle behavior service starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Skip idle behaviors if the bot is sleeping
                if (await _staminaService.IsSleepingAsync())
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }
                
                // Get all text channels (we can't check LastMessageId as it's not available in the current API version)
                var channels = _discordClient.Guilds
                    .SelectMany(g => g.TextChannels)
                    .ToList();
                
                if (!channels.Any())
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }
                
                // Check each channel for idle behavior opportunity
                foreach (var channel in channels)
                {
                    // Skip if token is cancelled
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    var channelId = channel.Id.ToString();
                    
                    // Check if we should perform an idle behavior in this channel
                    if (await _selfRegulationService.ShouldPerformIdleBehaviorAsync(channelId))
                    {
                        // Get recent participants
                        var messages = await channel.GetMessagesAsync(10).FlattenAsync();
                        var participants = messages
                            .Where(m => m.Author.Id != _discordClient.CurrentUser.Id)
                            .Select(m => m.Author.Id.ToString())
                            .Distinct()
                            .ToList();
                        
                        // Build engagement context
                        var context = await _selfRegulationService.BuildEngagementContextAsync(channelId, participants);
                        
                        // Determine the type of idle behavior to perform
                        var behaviorType = await _selfRegulationService.GetIdleBehaviorTypeAsync(context);
                        
                        // Perform the idle behavior
                        await PerformIdleBehaviorAsync(channel, behaviorType, context, stoppingToken);
                        
                        // Log the behavior
                        _logger.LogInformation("Performed idle behavior of type {BehaviorType} in channel {ChannelId}",
                            behaviorType, channelId);
                        
                        // Add delay between checking channels
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    }
                }
                
                // Wait before checking channels again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in idle behavior service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        
        _logger.LogInformation("Idle behavior service stopping");
    }
    
    /// <summary>
    /// Performs an idle behavior in a channel
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="behaviorType">The behavior type</param>
    /// <param name="context">The engagement context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformIdleBehaviorAsync(
        SocketTextChannel channel,
        string behaviorType,
        EngagementContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (behaviorType)
            {
                case IdleBehaviorType.EmojiReaction:
                    await PerformEmojiReactionAsync(channel);
                    break;
                    
                case IdleBehaviorType.StatusChange:
                    await PerformStatusChangeAsync(context);
                    break;
                    
                case IdleBehaviorType.InterestPrompt:
                    await PerformInterestPromptAsync(channel, context);
                    break;
                    
                case IdleBehaviorType.ChannelObservation:
                    await PerformChannelObservationAsync(channel, context);
                    break;
                    
                case IdleBehaviorType.OpenQuestion:
                    await PerformOpenQuestionAsync(channel, context);
                    break;
                    
                case IdleBehaviorType.RecallPreviousConversation:
                    await PerformRecallPreviousConversationAsync(channel, context);
                    break;
                    
                case IdleBehaviorType.VoicePresence:
                    await PerformVoicePresenceAsync(channel.Guild);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown idle behavior type: {BehaviorType}", behaviorType);
                    break;
            }
            
            // If this behavior type sends a message, record it for throttling
            if (behaviorType == IdleBehaviorType.InterestPrompt ||
                behaviorType == IdleBehaviorType.ChannelObservation ||
                behaviorType == IdleBehaviorType.OpenQuestion ||
                behaviorType == IdleBehaviorType.RecallPreviousConversation)
            {
                await _selfRegulationService.RecordBotMessageAsync(channel.Id.ToString(), Guid.NewGuid().ToString());
            }
            
            // Consume a small amount of stamina for the behavior
            await _staminaService.ConsumeStaminaAsync(0.2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing idle behavior {BehaviorType} in channel {ChannelId}",
                behaviorType, channel.Id);
        }
    }
    
    /// <summary>
    /// Performs an emoji reaction to a recent message
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformEmojiReactionAsync(SocketTextChannel channel)
    {
        // Get recent messages
        var messages = await channel.GetMessagesAsync(10).FlattenAsync();
        
        // Filter messages that are not from the bot
        var validMessages = messages
            .Where(m => m.Author.Id != _discordClient.CurrentUser.Id)
            .ToList();
        
        if (!validMessages.Any())
        {
            return;
        }
        
        // Select a random message
        var message = validMessages[_random.Next(validMessages.Count)];
        
        // Select an emoji based on the message content sentiment
        var emoji = await SelectEmojiForMessageAsync(message.Content);
        
        // Add reaction
        await message.AddReactionAsync(new Emoji(emoji));
    }
    
    /// <summary>
    /// Changes the bot's status based on emotional and stamina state
    /// </summary>
    /// <param name="context">The engagement context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformStatusChangeAsync(EngagementContext context)
    {
        // Get emotional state
        var emotionalState = context.CurrentEmotionalState;
        
        // Determine status based on stamina and emotional state
        UserStatus status = UserStatus.Online;
        string activityDescription = null;
        
        // Set status based on stamina
        if (context.CurrentStamina < 30)
        {
            status = UserStatus.Idle;
            activityDescription = "Resting a bit...";
        }
        
        // Adjust based on emotional state if available
        if (emotionalState != null)
        {
            if (emotionalState.GetEmotion(CoreEmotions.Joy.ToString()) > 0.7)
            {
                activityDescription = "Feeling cheerful!";
            }
            else if (emotionalState.GetEmotion(CoreEmotions.Sadness.ToString()) > 0.7)
            {
                activityDescription = "Feeling a bit down";
                status = UserStatus.Idle;
            }
            else if (emotionalState.GetEmotion(CoreEmotions.Anger.ToString()) > 0.6)
            {
                activityDescription = "Taking a moment to cool off";
                status = UserStatus.DoNotDisturb;
            }
        }
        
        // Set status and activity
        await _discordClient.SetStatusAsync(status);
        
        if (activityDescription != null)
        {
            await _discordClient.SetActivityAsync(new Game(activityDescription, ActivityType.CustomStatus));
        }
    }
    
    /// <summary>
    /// Initiates a conversation about a shared interest
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="context">The engagement context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformInterestPromptAsync(SocketTextChannel channel, EngagementContext context)
    {
        // Get interests for participants
        var interests = new List<string>();
        
        foreach (var participantId in context.ParticipantIds)
        {
            // Get topics this user is interested in
            var topics = await _knowledgeService.SearchTopicsAsync(new[] { participantId }, 5);
            
            foreach (var topic in topics)
            {
                interests.Add(topic.Name);
            }
        }
        
        if (!interests.Any())
        {
            // No known interests, use a generic prompt
            var genericPrompts = new[]
            {
                "I've been thinking about trying something new. Any suggestions?",
                "What's something interesting you've learned recently?",
                "I'm curious, what kinds of things are you all interested in?",
                "Been exploring different topics lately. What do you all like to talk about?"
            };
            
            await channel.SendMessageAsync(genericPrompts[_random.Next(genericPrompts.Length)]);
            return;
        }
        
        // Pick a random interest
        var interest = interests[_random.Next(interests.Count)];
        
        // Create a message about the interest
        var messages = new[]
        {
            $"I was just thinking about {interest}. Anyone else interested in that?",
            $"Has anyone been keeping up with {interest} lately?",
            $"I remember someone here was interested in {interest}. How's that going?",
            $"Random thought: {interest} is such a fascinating topic. Thoughts?"
        };
        
        await channel.SendMessageAsync(messages[_random.Next(messages.Length)]);
    }
    
    /// <summary>
    /// Shares an observation about the channel
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="context">The engagement context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformChannelObservationAsync(SocketTextChannel channel, EngagementContext context)
    {
        // Get channel activity level
        var activityLevel = context.ActivityLevel;
        
        // Create an observation based on activity
        string message = activityLevel switch
        {
            ActivityLevel.VeryHigh => "Wow, this channel is really active today!",
            ActivityLevel.High => "Nice to see this channel so lively!",
            ActivityLevel.Moderate => "This channel has a nice steady flow of conversation.",
            ActivityLevel.Low => "It's been a bit quiet in here lately. How is everyone doing?",
            ActivityLevel.Inactive => "It's been pretty quiet in here. Anything interesting going on?",
            _ => "Just checking in on this channel. Hope everyone's doing well!"
        };
        
        await channel.SendMessageAsync(message);
    }
    
    /// <summary>
    /// Asks an open-ended question to the channel
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="context">The engagement context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformOpenQuestionAsync(SocketTextChannel channel, EngagementContext context)
    {
        // Select a question based on relationship strength
        string[] questions;
        
        if (context.AverageRelationshipStrength > 0.7)
        {
            // More personal questions for closer relationships
            questions = new[]
            {
                "What's been the highlight of your week so far?",
                "What's something you're looking forward to?",
                "Any new hobbies or interests you've been exploring lately?",
                "What's something that made you smile today?",
                "If you could learn any new skill instantly, what would it be?"
            };
        }
        else if (context.AverageRelationshipStrength > 0.4)
        {
            // Medium personal questions for moderate relationships
            questions = new[]
            {
                "What's everyone working on these days?",
                "Any interesting projects keeping you busy?",
                "Read any good books or watched any good shows lately?",
                "What's a topic you've been interested in learning more about?",
                "How do you all like to recharge after a busy day/week?"
            };
        }
        else
        {
            // General questions for newer relationships
            questions = new[]
            {
                "How's everyone doing today?",
                "Any interesting news to share?",
                "What's got everyone's attention these days?",
                "Any recommendations for good music/shows/games?",
                "What's something that's caught your interest recently?"
            };
        }
        
        // Pick a random question
        var question = questions[_random.Next(questions.Length)];
        
        await channel.SendMessageAsync(question);
    }
    
    /// <summary>
    /// Recalls a previous conversation topic
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="context">The engagement context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformRecallPreviousConversationAsync(SocketTextChannel channel, EngagementContext context)
    {
        // Get recent memories related to this channel
        var memories = await _knowledgeService.SearchMemoriesAsync(new[] { channel.Id.ToString() }, 5);
        
        if (!memories.Any())
        {
            // No memories found, use a generic recall
            var genericRecalls = new[]
            {
                "I remember we had an interesting conversation in here before. How's everyone been since then?",
                "Last time we talked, I really enjoyed the discussion. Anyone up for another chat?",
                "Been thinking about our previous conversations. Anyone want to pick up where we left off?",
                "I've been reflecting on things we've discussed before. Any new thoughts on those topics?"
            };
            
            await channel.SendMessageAsync(genericRecalls[_random.Next(genericRecalls.Length)]);
            return;
        }
        
        // Pick a random memory
        var memory = memories.ElementAt(_random.Next(memories.Count()));
        
        // Create a message recalling the memory
        var recalls = new[]
        {
            $"I was just thinking about when we talked about {memory.Title}. Any new thoughts on that?",
            $"Remember our conversation about {memory.Title}? That was interesting!",
            $"I've been reflecting on our discussion of {memory.Title}. Anyone have more to add to that?",
            $"The topic of {memory.Title} came up in here before. Any updates on that?"
        };
        
        await channel.SendMessageAsync(recalls[_random.Next(recalls.Length)]);
    }
    
    /// <summary>
    /// Joins a voice channel briefly without speaking
    /// </summary>
    /// <param name="guild">The guild</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformVoicePresenceAsync(SocketGuild guild)
    {
        // Find active voice channels
        var voiceChannels = guild.VoiceChannels
            .Where(vc => vc.ConnectedUsers.Count > 0)
            .ToList();
        
        if (!voiceChannels.Any())
        {
            return;
        }
        
        // Pick a random voice channel
        var voiceChannel = voiceChannels[_random.Next(voiceChannels.Count)];
        
        try
        {
            // Join the voice channel
            await voiceChannel.ConnectAsync();
            
            // Stay in the channel for a short time
            await Task.Delay(TimeSpan.FromMinutes(2));
            
            // Leave the voice channel
            await guild.AudioClient.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing voice presence in channel {ChannelId}", voiceChannel.Id);
        }
    }
    
    /// <summary>
    /// Selects an appropriate emoji for a message based on content
    /// </summary>
    /// <param name="content">The message content</param>
    /// <returns>The selected emoji</returns>
    private async Task<string> SelectEmojiForMessageAsync(string content)
    {
        // Common positive emojis
        var positiveEmojis = new[] { "👍", "😊", "🙂", "❤️", "👏", "✅", "🎉", "🌟", "💯", "😄" };
        
        // Common negative emojis
        var negativeEmojis = new[] { "😢", "😔", "🤔", "😮", "😯", "😕", "❓", "🤷" };
        
        // Common neutral emojis
        var neutralEmojis = new[] { "👀", "👋", "🙌", "💭", "🔍", "📝", "🧐", "🤓", "💡" };
        
        // TODO: Use AI to analyze the sentiment of the message
        // For now, use a simple approach
        
        // Check for question marks or keywords that indicate a question
        if (content.Contains('?') || 
            content.ToLower().StartsWith("what") ||
            content.ToLower().StartsWith("how") ||
            content.ToLower().StartsWith("why") ||
            content.ToLower().StartsWith("who") ||
            content.ToLower().StartsWith("when"))
        {
            // For questions, use thinking or questioning emojis
            return neutralEmojis[_random.Next(neutralEmojis.Length)];
        }
        
        // Check for exclamation marks or excitement indicators
        if (content.Contains('!') ||
            content.ToUpper() == content && content.Length > 5) // ALL CAPS
        {
            // For excited messages, use positive emojis
            return positiveEmojis[_random.Next(positiveEmojis.Length)];
        }
        
        // Check for sad words
        if (content.ToLower().Contains("sad") ||
            content.ToLower().Contains("disappointing") ||
            content.ToLower().Contains("upset") ||
            content.ToLower().Contains("unfortunate"))
        {
            // For sad messages, use empathetic emojis
            return negativeEmojis[_random.Next(negativeEmojis.Length)];
        }
        
        // Default to a random selection with higher probability of positive emojis
        var roll = _random.NextDouble();
        if (roll < 0.6)
        {
            return positiveEmojis[_random.Next(positiveEmojis.Length)];
        }
        else if (roll < 0.8)
        {
            return neutralEmojis[_random.Next(neutralEmojis.Length)];
        }
        else
        {
            return negativeEmojis[_random.Next(negativeEmojis.Length)];
        }
    }
}
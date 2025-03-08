using ElectricRaspberry.Models.Conversation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ElectricRaspberry.Services;

public class ConversationService : IConversationService
{
    private readonly ILogger<ConversationService> _logger;
    private readonly SemaphoreSlim _conversationLock = new(1, 1);
    private readonly ConcurrentDictionary<string, Conversation> _conversations = new();
    
    // Thresholds for conversation management
    private readonly TimeSpan _idleThreshold = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _completionThreshold = TimeSpan.FromHours(1);
    private readonly int _maxActiveConversations = 50;
    
    public ConversationService(ILogger<ConversationService> logger)
    {
        _logger = logger;
    }
    
    public async Task<Conversation> ProcessMessageAsync(MessageEvent messageEvent, string channelId = null, bool highPriority = false)
    {
        await _conversationLock.WaitAsync();
        try
        {
            // First, determine which conversation this message belongs to
            var conversation = await DetermineConversationForMessageAsync(messageEvent);
            
            // Add the message to the conversation
            conversation.AddMessage(messageEvent);
            
            // Ensure conversation is marked as active or urgent based on priority
            if (highPriority)
            {
                conversation.State = ConversationState.Urgent;
            }
            else if (conversation.State != ConversationState.Active && conversation.State != ConversationState.Urgent)
            {
                conversation.State = ConversationState.Active;
            }
            
            _logger.LogDebug("Added message {MessageId} to conversation {ConversationId}", 
                messageEvent.Message.Id, conversation.Id);
            
            return conversation;
        }
        finally
        {
            _conversationLock.Release();
        }
    }
    
    public async Task<Conversation?> GetConversationAsync(string conversationId)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            return conversation;
        }
        
        return null;
    }
    
    public async Task<IEnumerable<Conversation>> GetActiveConversationsAsync()
    {
        await _conversationLock.WaitAsync();
        try
        {
            return _conversations.Values
                .Where(c => c.State == ConversationState.Active || c.State == ConversationState.Urgent)
                .OrderByDescending(c => c.LastActiveAt)
                .ToList();
        }
        finally
        {
            _conversationLock.Release();
        }
    }
    
    public async Task<IEnumerable<Conversation>> GetChannelConversationsAsync(ulong channelId)
    {
        await _conversationLock.WaitAsync();
        try
        {
            return _conversations.Values
                .Where(c => c.ChannelId == channelId && 
                          (c.State == ConversationState.Active || c.State == ConversationState.Urgent))
                .OrderByDescending(c => c.LastActiveAt)
                .ToList();
        }
        finally
        {
            _conversationLock.Release();
        }
    }
    
    public async Task<bool> CompleteConversationAsync(string conversationId)
    {
        await _conversationLock.WaitAsync();
        try
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation.State = ConversationState.Completed;
                _logger.LogInformation("Conversation {ConversationId} marked as completed", conversationId);
                return true;
            }
            
            return false;
        }
        finally
        {
            _conversationLock.Release();
        }
    }
    
    public async Task<bool> UpdateConversationTopicAsync(string conversationId, string topic)
    {
        await _conversationLock.WaitAsync();
        try
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation.Topic = topic;
                _logger.LogDebug("Updated topic for conversation {ConversationId}: {Topic}", 
                    conversationId, topic);
                return true;
            }
            
            return false;
        }
        finally
        {
            _conversationLock.Release();
        }
    }
    
    public async Task<bool> SetConversationImportanceAsync(string conversationId, double importance)
    {
        await _conversationLock.WaitAsync();
        try
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation.Importance = Math.Clamp(importance, 0, 1);
                
                // If importance is very high, mark as urgent
                if (importance > 0.8 && conversation.State == ConversationState.Active)
                {
                    conversation.State = ConversationState.Urgent;
                }
                
                _logger.LogDebug("Set importance for conversation {ConversationId} to {Importance}", 
                    conversationId, importance);
                return true;
            }
            
            return false;
        }
        finally
        {
            _conversationLock.Release();
        }
    }
    
    public async Task<IEnumerable<MessageReference>> GetRecentMessagesAsync(string conversationId, int count = 10)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            return conversation.GetRecentMessages(count);
        }
        
        return Enumerable.Empty<MessageReference>();
    }
    
    public async Task<string> CreateConversationContextAsync(string conversationId)
    {
        if (!_conversations.TryGetValue(conversationId, out var conversation))
        {
            return "No conversation found.";
        }
        
        var messages = conversation.GetRecentMessages(10);
        var contextBuilder = new System.Text.StringBuilder();
        
        // Add conversation metadata
        contextBuilder.AppendLine($"Conversation in {conversation.ChannelName}");
        contextBuilder.AppendLine($"Topic: {(string.IsNullOrEmpty(conversation.Topic) ? "Unspecified" : conversation.Topic)}");
        contextBuilder.AppendLine($"Participants: {conversation.ParticipantIds.Count}");
        contextBuilder.AppendLine($"Started: {conversation.StartedAt}");
        contextBuilder.AppendLine();
        
        // Add recent messages
        contextBuilder.AppendLine("Recent messages:");
        foreach (var message in messages)
        {
            string sender = message.IsFromBot ? "Bot" : $"User {message.AuthorId}";
            contextBuilder.AppendLine($"[{message.Timestamp.ToShortTimeString()}] {sender}: {message.Content}");
        }
        
        return contextBuilder.ToString();
    }
    
    public async Task PerformMaintenanceAsync()
    {
        await _conversationLock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var keysToRemove = new List<string>();
            
            // Update conversation states based on activity
            foreach (var conversation in _conversations.Values)
            {
                // Check for idle conversations
                if (conversation.State == ConversationState.Active && 
                    now - conversation.LastActiveAt > _idleThreshold)
                {
                    conversation.State = ConversationState.Idle;
                    _logger.LogDebug("Conversation {ConversationId} marked as idle", conversation.Id);
                }
                
                // Check for old completed conversations to remove
                if (conversation.State == ConversationState.Completed && 
                    now - conversation.LastActiveAt > _completionThreshold)
                {
                    keysToRemove.Add(conversation.Id);
                }
            }
            
            // Remove old conversations
            foreach (var key in keysToRemove)
            {
                _conversations.TryRemove(key, out _);
                _logger.LogInformation("Removed old conversation {ConversationId}", key);
            }
            
            // Limit total number of active conversations if needed
            if (_conversations.Count > _maxActiveConversations)
            {
                // Mark oldest idle conversations as completed to be removed in next maintenance
                var oldestIdle = _conversations.Values
                    .Where(c => c.State == ConversationState.Idle)
                    .OrderBy(c => c.LastActiveAt)
                    .Take(_conversations.Count - _maxActiveConversations)
                    .ToList();
                
                foreach (var conversation in oldestIdle)
                {
                    conversation.State = ConversationState.Completed;
                    _logger.LogInformation("Auto-completed old idle conversation {ConversationId}", conversation.Id);
                }
            }
            
            _logger.LogDebug("Conversation maintenance completed. Active: {Active}, Idle: {Idle}, Completed: {Completed}",
                _conversations.Values.Count(c => c.State == ConversationState.Active),
                _conversations.Values.Count(c => c.State == ConversationState.Idle),
                _conversations.Values.Count(c => c.State == ConversationState.Completed));
        }
        finally
        {
            _conversationLock.Release();
        }
    }
    
    public async Task ResetAllConversationsAsync()
    {
        await _conversationLock.WaitAsync();
        try
        {
            // Mark all conversations as completed
            foreach (var conversation in _conversations.Values)
            {
                conversation.State = ConversationState.Completed;
            }
            
            _logger.LogInformation("Reset all conversations to completed state");
        }
        finally
        {
            _conversationLock.Release();
        }
    }
    
    // Helper methods
    private async Task<Conversation> DetermineConversationForMessageAsync(MessageEvent messageEvent)
    {
        // Direct messages always create their own conversation if no active one exists
        if (messageEvent.IsDirectMessage)
        {
            var dmConversations = _conversations.Values
                .Where(c => c.IsDirectMessage && 
                          c.ChannelId == messageEvent.Channel.Id && 
                          c.State != ConversationState.Completed)
                .OrderByDescending(c => c.LastActiveAt)
                .ToList();
            
            // Use the most recent active DM conversation if it exists and is recent
            if (dmConversations.Any() && 
                DateTime.UtcNow - dmConversations.First().LastActiveAt < _idleThreshold)
            {
                return dmConversations.First();
            }
            
            // Otherwise create a new DM conversation
            var newDmConversation = new Conversation(messageEvent);
            _conversations[newDmConversation.Id] = newDmConversation;
            _logger.LogInformation("Created new DM conversation {ConversationId} with {User}", 
                newDmConversation.Id, messageEvent.Message.Author.Username);
            return newDmConversation;
        }
        
        // For channel messages, try to join an existing conversation if it's active
        var channelConversations = _conversations.Values
            .Where(c => !c.IsDirectMessage && 
                      c.ChannelId == messageEvent.Channel.Id && 
                      c.State != ConversationState.Completed)
            .OrderByDescending(c => c.LastActiveAt)
            .ToList();
        
        // Use the most recent active channel conversation if it exists and is recent
        if (channelConversations.Any() && 
            DateTime.UtcNow - channelConversations.First().LastActiveAt < _idleThreshold)
        {
            return channelConversations.First();
        }
        
        // Otherwise create a new channel conversation
        var newChannelConversation = new Conversation(messageEvent);
        _conversations[newChannelConversation.Id] = newChannelConversation;
        _logger.LogInformation("Created new channel conversation {ConversationId} in channel {ChannelId}", 
            newChannelConversation.Id, messageEvent.Channel.Id);
        return newChannelConversation;
    }
}
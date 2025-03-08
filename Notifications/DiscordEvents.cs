using Discord;
using Discord.WebSocket;
using MediatR;

namespace ElectricRaspberry.Notifications;

// Base notification for all Discord events
public abstract record DiscordEventNotification : INotification
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

#region Gateway Events

// Ready event
public record ReadyNotification : DiscordEventNotification;

// Connected event
public record ConnectedNotification : DiscordEventNotification;

// Disconnected event
public record DisconnectedNotification(Exception? Exception) : DiscordEventNotification;

// Log event
public record LogNotification(LogSeverity Severity, string Source, string Message, Exception? Exception) : DiscordEventNotification;

#endregion

#region Guild Events

// Guild available event
public record GuildAvailableNotification(SocketGuild Guild) : DiscordEventNotification;

// Guild unavailable event
public record GuildUnavailableNotification(SocketGuild Guild) : DiscordEventNotification;

// Guild updated event
public record GuildUpdatedNotification(SocketGuild OldGuild, SocketGuild NewGuild) : DiscordEventNotification;

// Guild member updated event
public record GuildMemberUpdatedNotification(Cacheable<SocketGuildUser, ulong> OldUser, SocketGuildUser NewUser) : DiscordEventNotification;

// User joined guild event
public record UserJoinedNotification(SocketGuildUser User) : DiscordEventNotification;

// User left guild event
public record UserLeftNotification(SocketGuild Guild, SocketUser User) : DiscordEventNotification;

#endregion

#region Message Events

// Message received event
public record MessageReceivedNotification(SocketMessage Message) : DiscordEventNotification;

// Message updated event
public record MessageUpdatedNotification(Cacheable<IMessage, ulong> OldMessage, SocketMessage NewMessage, ISocketMessageChannel Channel) : DiscordEventNotification;

// Message deleted event
public record MessageDeletedNotification(Cacheable<IMessage, ulong> Message, Cacheable<IMessageChannel, ulong> Channel) : DiscordEventNotification;

// Reaction added event
public record ReactionAddedNotification(Cacheable<IUserMessage, ulong> Message, Cacheable<IMessageChannel, ulong> Channel, SocketReaction Reaction) : DiscordEventNotification;

// Reaction removed event
public record ReactionRemovedNotification(Cacheable<IUserMessage, ulong> Message, Cacheable<IMessageChannel, ulong> Channel, SocketReaction Reaction) : DiscordEventNotification;

// Reactions cleared event
public record ReactionsAllClearedNotification(Cacheable<IUserMessage, ulong> Message, Cacheable<IMessageChannel, ulong> Channel) : DiscordEventNotification;

// Reaction emoji cleared event
public record ReactionsRemovedForEmoteNotification(Cacheable<IUserMessage, ulong> Message, Cacheable<IMessageChannel, ulong> Channel, IEmote Emote) : DiscordEventNotification;

#endregion

#region Channel Events

// Channel created event
public record ChannelCreatedNotification(SocketChannel Channel) : DiscordEventNotification;

// Channel updated event
public record ChannelUpdatedNotification(SocketChannel OldChannel, SocketChannel NewChannel) : DiscordEventNotification;

// Channel destroyed event
public record ChannelDestroyedNotification(SocketChannel Channel) : DiscordEventNotification;

#endregion

#region Role Events

// Role created event
public record RoleCreatedNotification(SocketRole Role) : DiscordEventNotification;

// Role updated event
public record RoleUpdatedNotification(SocketRole OldRole, SocketRole NewRole) : DiscordEventNotification;

// Role deleted event
public record RoleDeletedNotification(SocketRole Role) : DiscordEventNotification;

#endregion

#region User/Presence Events

// User updated event
public record UserUpdatedNotification(SocketUser OldUser, SocketUser NewUser) : DiscordEventNotification;

// Current user updated event
public record CurrentUserUpdatedNotification(SocketSelfUser OldUser, SocketSelfUser NewUser) : DiscordEventNotification;

// User banned event
public record UserBannedNotification(SocketUser User, SocketGuild Guild) : DiscordEventNotification;

// User unbanned event
public record UserUnbannedNotification(SocketUser User, SocketGuild Guild) : DiscordEventNotification;

// User joined voice channel event
public record UserVoiceStateUpdatedNotification(SocketUser User, SocketVoiceState OldState, SocketVoiceState NewState) : DiscordEventNotification;

// User voice server updated event
public record UserVoiceServerUpdatedNotification(SocketUser User, SocketVoiceServer Server) : DiscordEventNotification;

// Presence updated event
public record PresenceUpdatedNotification(SocketUser User, SocketPresence OldPresence, SocketPresence NewPresence) : DiscordEventNotification;

#endregion

#region Other Events

// Typing started event
public record TypingStartedNotification(Cacheable<IUser, ulong> User, Cacheable<IMessageChannel, ulong> Channel) : DiscordEventNotification;

// Latency updated event
public record LatencyUpdatedNotification(int OldLatency, int NewLatency) : DiscordEventNotification;

// Button executed event
public record ButtonExecutedNotification(SocketMessageComponent Component) : DiscordEventNotification;

// Select menu executed event
public record SelectMenuExecutedNotification(SocketMessageComponent Component) : DiscordEventNotification;

// Modal submitted event
public record ModalSubmittedNotification(SocketModal Modal) : DiscordEventNotification;

// Slash command executed event
public record SlashCommandExecutedNotification(SocketSlashCommand Command) : DiscordEventNotification;

// User command executed event
public record UserCommandExecutedNotification(SocketUserCommand Command) : DiscordEventNotification;

// Message command executed event
public record MessageCommandExecutedNotification(SocketMessageCommand Command) : DiscordEventNotification;

// Auto moderation rule created event
public record AutoModerationRuleCreatedNotification(SocketAutoModerationRule Rule) : DiscordEventNotification;

// Auto moderation rule updated event
public record AutoModerationRuleUpdatedNotification(SocketAutoModerationRule OldRule, SocketAutoModerationRule NewRule) : DiscordEventNotification;

// Auto moderation rule deleted event
public record AutoModerationRuleDeletedNotification(SocketAutoModerationRule Rule) : DiscordEventNotification;

// Auto moderation action executed event
public record AutoModerationActionExecutedNotification(SocketAutoModerationActionExecutedData Data) : DiscordEventNotification;

#endregion
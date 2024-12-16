using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class BotJaneSmith : DiscordBackgroundService
{
    public BotJaneSmith(ILogger<BotJaneSmith> logger, IOptions<DiscordOptions> options) : base(logger, options.Value.JaneSmith)
    {
        this.client.MessageReceived += OnMessageReceived;
    }
    private async Task OnMessageReceived(SocketMessage message)
    {
        if(message.Author.Id == this.client.CurrentUser.Id)
            return;

        if(message.MentionedUsers.Any(_ => _.Id == this.client.CurrentUser.Id))
        {
            var user = message.Author as SocketGuildUser;
            if(user is null)
                return;

            var channel = await this.client.GetChannelAsync(message.Channel.Id) as ISocketMessageChannel;
            if(channel is null)
                return;

            var stickers = this.client.Guilds.SelectMany(_ => _.Stickers).ToArray();

            await channel.SendMessageAsync(
                text: "Hello!",
                allowedMentions: AllowedMentions.All,
                // messageReference: new MessageReference(message.Id),
                stickers: stickers,
                components: new ComponentBuilder().Build(),
                embed: new EmbedBuilder().Build(),
                poll: new PollProperties()
                {
                    LayoutType = PollLayout.Default,
                    Duration = 24,
                    AllowMultiselect = false,
                    Question = new PollMediaProperties()
                    {
                        Text = "Do you like this bot?",
                    },
                    Answers = new List<PollMediaProperties>()
                    {
                        new PollMediaProperties()
                        {
                            Text = "Yes",
                            Emoji = new Emoji("👍"),
                        },
                        new PollMediaProperties()
                        {
                            Text = "No",
                            Emoji = new Emoji("👎"),
                        }
                    },
                }
            );
        }
    }
}
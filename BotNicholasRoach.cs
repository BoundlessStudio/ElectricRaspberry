using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class BotNicholasRoach : DiscordBackgroundService
{
    public BotNicholasRoach(ILogger<BotNicholasRoach> logger, IOptions<DiscordOptions> options) : base(logger, options.Value.NicholasRoach)
    {
        this.client.MessageReceived += OnMessageReceived;
    }
    private async Task OnMessageReceived(SocketMessage message)
    {
        if(message.Author.Id == this.client.CurrentUser.Id)
        {
            // Self reflection
            return;
        }

        if(message.MentionedUsers.Any(_ => _.Id == this.client.CurrentUser.Id))
        {
            await message.Channel.SendMessageAsync($"Echo: {message.Content}");
        }
    }
}
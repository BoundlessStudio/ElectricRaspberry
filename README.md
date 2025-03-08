# ElectricRaspberry Discord Bot

A .NET Web API application that implements a Discord.Net background service using MediatR for handling Discord events.

## Features

- ASP.NET Core Web API
- Discord.Net integration as a background service
- MediatR for event handling
- Complete implementation of Discord.Net gateway client events
- Scalable architecture for handling Discord events

## Requirements

- .NET 8.0 SDK
- Discord Bot Token

## Configuration

1. Update the `appsettings.json` file with your Discord bot token:

```json
{
  "Discord": {
    "Token": "YOUR_DISCORD_BOT_TOKEN",
    "GuildIds": []
  }
}
```

## Running the Application

```bash
dotnet restore
dotnet build
dotnet run
```

The API will be available at:
- https://localhost:5001
- http://localhost:5000

## Architecture

### Components

1. **DiscordBotService**: Background service that connects to Discord and publishes events
2. **MediatR Notifications**: Records defined for each Discord event
3. **MediatR Handlers**: Classes that process Discord events

### Discord Events

All Discord.Net gateway client events are implemented and published as MediatR notifications:

- Gateway Events (Ready, Connected, Disconnected, Log)
- Guild Events
- Message Events
- Channel Events
- Role Events
- User/Presence Events
- Interaction Events (Buttons, Select Menus, Modals, Slash Commands)
- Auto Moderation Events

### Adding Custom Event Handlers

To handle a specific Discord event, implement a new handler class:

```csharp
public class YourCustomEventHandler : INotificationHandler<EventNotification>
{
    private readonly ILogger<YourCustomEventHandler> _logger;

    public YourCustomEventHandler(ILogger<YourCustomEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventNotification notification, CancellationToken cancellationToken)
    {
        // Your custom logic here
        return Task.CompletedTask;
    }
}
```

## API Endpoints

- `GET /api/discord/status`: Returns the bot's status and configured guilds

## License

MIT

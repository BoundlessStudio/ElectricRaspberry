namespace ElectricRaspberry.Configuration;

public class DiscordOptions
{
    public const string Discord = "Discord";
    
    public string Token { get; set; } = string.Empty;
    public ulong[] GuildIds { get; set; } = Array.Empty<ulong>();
}
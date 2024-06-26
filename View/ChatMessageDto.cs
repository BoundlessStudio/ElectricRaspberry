using System.Text.Json.Serialization;

namespace ElectricRaspberry.View;

public class ChatMessageDto
{
  [JsonPropertyName("instructions")]
  public string? Instructions { get; set; }

  [JsonPropertyName("prompt")]
  public string? Prompt { get; set; }

  [JsonPropertyName("history")]
  public List<MessageDto> History { get; set; } = new List<MessageDto>();
}

public class MessageDto
{
  [JsonConstructor]
  public MessageDto(string role, string content)
  {
    this.Role = role;
    this.Content = content;
  }

  [JsonPropertyName("role")]
  public string Role { get; set; }

  [JsonPropertyName("content")]
  public string Content { get; set; }
}
using System.Text.Json.Serialization;
using OpenAI.Files;

public class FileInfoDto
{
  public FileInfoDto(OpenAIFileInfo info)
  {
    this.Id = info.Id;
    this.Filename = info.Filename;
    this.Size = info.SizeInBytes ?? 0;
    this.CreatedAt = info.CreatedAt;
    this.Purpose = info.Purpose.ToString();
    this.Status = info.Status.ToString();
    this.Details = info.StatusDetails;
  }

  [JsonConstructor]
  public FileInfoDto(string id, string filename, long? size, DateTimeOffset createdAt, string purpose, string status, string details)
  {
    this.Id = id;
    this.Filename = filename;
    this.Size = size;
    this.CreatedAt = createdAt;
    this.Purpose = purpose;
    this.Status = status;
    this.Details = details;
  }

  [JsonPropertyName("id")]
  public string Id { get; set; }

  [JsonPropertyName("filename")]
  public string Filename { get; set;  }

  [JsonPropertyName("size")]
  public long? Size { get; set;  }

  [JsonPropertyName("created_at")]
  public DateTimeOffset CreatedAt { get; set; }

  [JsonPropertyName("purpose")]
  public string Purpose { get; set;  }

  [JsonPropertyName("status")]
  public string Status  { get; set;  }

  [JsonPropertyName("details")]
  public string Details { get; set;  }

}
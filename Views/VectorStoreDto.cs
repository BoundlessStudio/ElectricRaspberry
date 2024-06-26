using System.Text.Json.Serialization;
using OpenAI.VectorStores;

public class VectorStoreDto
{
  [JsonConstructor]
  public VectorStoreDto(string id, string name, DateTimeOffset? created, DateTimeOffset? expires, DateTimeOffset? last_active, int usage, string status, FileCountsDto counts)
  {
    this.Id = id;
    this.Name = name;
    this.CreatedAt = created;
    this.ExpiresAt = expires;
    this.LastActiveAt = last_active;
    this.Status = status;
    this.UsageBytes = usage;
    this.FileCounts = counts;
  }
  
  public VectorStoreDto(VectorStore store)
  {
    this.Id = store.Id;
    this.Name = store.Name;
    this.ExpiresAt = store.ExpiresAt;
    this.CreatedAt = store.CreatedAt;
    this.LastActiveAt = store.LastActiveAt;
    this.Status = store.Status.ToString();
    this.UsageBytes = store.UsageBytes;
    this.FileCounts = new FileCountsDto()
    {
      Total = store.FileCounts.Total,
      Cancelled = store.FileCounts.Cancelled,
      Failed = store.FileCounts.Failed,
      Completed = store.FileCounts.Completed,
      InProgress = store.FileCounts.InProgress,
    };
  }

  [JsonPropertyName("id")]
  public string Id { get; set; }

  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("created_at")]
  public DateTimeOffset? CreatedAt { get; set; }

  [JsonPropertyName("expires_at")]
  public DateTimeOffset? ExpiresAt { get; set; }

  [JsonPropertyName("last_active_at")]
  public DateTimeOffset? LastActiveAt { get; set; }

  [JsonPropertyName("usage")]
  public int UsageBytes { get; set; }

  [JsonPropertyName("status")]
  public string Status { get; set; }

  [JsonPropertyName("counts")]
  public FileCountsDto FileCounts { get; set; }
}


public class FileCountsDto
{
  public int InProgress { get; set; }

  public int Completed { get; set; }

  public int Failed { get; set; }

  public int Cancelled { get; set; }

  public int Total { get; set; }

}
using System.Text.Json.Serialization;
using OpenAI.VectorStores;
public class FileAssociationDto
{
  [JsonConstructor]
  public FileAssociationDto(string fileId, int size, DateTimeOffset createdAt, string vectorStoreId, string? lastError = null)
  {
    this.FileId = fileId;
    this.Size = size;
    this.CreatedAt = createdAt;
    this.VectorStoreId = vectorStoreId;
    this.LastError = lastError;
  }

  public FileAssociationDto(VectorStoreFileAssociation association)
  {
    this.FileId = association.FileId;
    this.Size = association.Size;
    this.CreatedAt = association.CreatedAt;
    this.VectorStoreId = association.VectorStoreId;
    this.LastError = association.LastError?.Message ?? null;
  }


  [JsonPropertyName("field_id")]
  public string FileId { get; set; }

  [JsonPropertyName("size")]
  public int Size { get; set; }

  [JsonPropertyName("created")]
  public DateTimeOffset CreatedAt { get; set; }

  [JsonPropertyName("store_id")]
  public string VectorStoreId { get; set; }

  [JsonPropertyName("last_error")]
  public string? LastError { get; set; }
}
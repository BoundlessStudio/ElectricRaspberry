using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

public interface IFeedStorageService
{
  Task EnsureContainer(string containerName, CancellationToken cancellationToken = default);
  Task RemoveContainer(string containerName, CancellationToken cancellationToken = default);
  Task<string> CopyFrom(string url, string containerName, CancellationToken cancellationToken = default);
  Task<string> Upload(string containerName, IFormFile File, CancellationToken cancellationToken = default);
}

public class FeedStorageService : IFeedStorageService
{
  private readonly string storageConnectionString;

  public FeedStorageService(IOptions<StorageOptions> storageOptions)
  {
    this.storageConnectionString = storageOptions.Value.ConnectionString;
  }

  public async Task EnsureContainer(string containerName, CancellationToken cancellationToken = default)
  {
    var container = new BlobContainerClient(storageConnectionString, containerName);
    await container.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob, cancellationToken: cancellationToken);
  }

  public async Task RemoveContainer(string containerName, CancellationToken cancellationToken = default)
  {
    var container = new BlobContainerClient(storageConnectionString, containerName);
    await container.DeleteIfExistsAsync(cancellationToken: cancellationToken);
  }

  public async Task<string> CopyFrom(string url, string containerName, CancellationToken cancellationToken = default)
  {
    var blobName = Guid.NewGuid().ToString("N") + ".png";
    var client = new BlobClient(storageConnectionString, containerName, blobName);
    await client.SyncCopyFromUriAsync(new Uri(url), cancellationToken: cancellationToken);
    return client.Uri.AbsoluteUri;
  }

  public async Task<string> Upload(string containerName, IFormFile file, CancellationToken cancellationToken = default)
  {
    var info = new FileInfo(file.FileName);
    var contentType = ElectricRaspberry.MimeTypes.GetMimeType(file.FileName);
    var blobName = Guid.NewGuid().ToString("N") + info.Extension;
    var client = new BlobClient(storageConnectionString, containerName, blobName);
    var options = new BlobUploadOptions
    {
        HttpHeaders = new BlobHttpHeaders()
        {
          ContentType = contentType
        }
    };
    await client.UploadAsync(file.OpenReadStream(), options, cancellationToken);
    return client.Uri.AbsoluteUri;
  }
}
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using ElectricRaspberry.Models;
using ElectricRaspberry.Models.TinyUrl;

namespace ElectricRaspberry.Services
{
  public interface IStorageService
  {
    Task<string> CopyFrom(string url, string container, CancellationToken cancellationToken = default);
    Task<string> UploadBase64(string input, string container, string name, CancellationToken cancellationToken = default);
    Task<string> Upload(BinaryData data, string container, string name, CancellationToken cancellationToken = default);
    Task<string> Upload(IFormFile file, string container, CancellationToken cancellationToken = default);
  }

  public class StorageService : IStorageService
  {
    private readonly string storageConnectionString;

    public StorageService(IOptions<StorageOptions> storageOptions)
    {
      this.storageConnectionString = storageOptions.Value.ConnectionString;
    }

    private async Task EnsureContainer(string container, CancellationToken cancellationToken = default)
    {
      var client = new BlobContainerClient(storageConnectionString, container);
      await client.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
    }

    private Task<string> GetTinyUrl(string url, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(url);
    }

    public async Task<string> CopyFrom(string url, string container, CancellationToken cancellationToken = default)
    {
      await this.EnsureContainer(container, cancellationToken);

      var name = Path.GetFileName(new Uri(url).LocalPath);
      var client = new BlobClient(storageConnectionString, container, name);
      await client.SyncCopyFromUriAsync(new Uri(url), cancellationToken: cancellationToken);
      var uri = client.Uri.AbsoluteUri;
      return await GetTinyUrl(uri);
    }

    public async Task<string> UploadBase64(string input, string container, string name, CancellationToken cancellationToken = default)
    {
      var bytes = Convert.FromBase64String(input);
      var data = BinaryData.FromBytes(bytes);
      return await Upload(data, container, name, cancellationToken);
    }

    public async Task<string> Upload(BinaryData data, string container, string name, CancellationToken cancellationToken = default)
    {
      await this.EnsureContainer(container, cancellationToken);

      var client = new BlobClient(storageConnectionString, container, name);
      var reponse = await client.UploadAsync(data, true);
      var uri = client.Uri.AbsoluteUri;
      return await GetTinyUrl(uri);
    }


    public async Task<string> Upload(IFormFile file, string container, CancellationToken cancellationToken = default)
    {
      await this.EnsureContainer(container, cancellationToken);

      var client = new BlobClient(storageConnectionString, container, file.FileName);
      var options = new BlobUploadOptions
      {
        HttpHeaders = new BlobHttpHeaders()
        {
          ContentType = file.ContentType
        }
      };
      var stream = file.OpenReadStream();
      await client.UploadAsync(stream, options, cancellationToken);
      var uri = client.Uri.AbsoluteUri;
      return await GetTinyUrl(uri);
    }

    public async Task<string> Upload( byte[] data, string name, string contentType, string container, CancellationToken cancellationToken = default)
    {
      await this.EnsureContainer(container, cancellationToken);

      var client = new BlobClient(storageConnectionString, container, name);
      var options = new BlobUploadOptions
      {
        HttpHeaders = new BlobHttpHeaders()
        {
          ContentType = contentType
        }
      };
      await client.UploadAsync(BinaryData.FromBytes(data), options, cancellationToken);
      var uri = client.Uri.AbsoluteUri;
      return await GetTinyUrl(uri);
    }
  }
}

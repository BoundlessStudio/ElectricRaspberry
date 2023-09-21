using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using ElectricRaspberry.Models;
using ElectricRaspberry.Models.TinyUrl;

namespace ElectricRaspberry.Services
{
  public interface IStorageService
  {
    Task<string> CopyFrom(string url, string container, CancellationToken cancellationToken = default);
    Task<string> Upload(byte[] data, string name, string contentType, string container, CancellationToken cancellationToken = default);
    Task<string> Upload(IFormFile file, string container, CancellationToken cancellationToken = default);
  }

  public class StorageService : IStorageService
  {
    private readonly string storageConnectionString;
    private readonly HttpClient restClient;

    public StorageService(IOptions<StorageOptions> storageOptions, IOptions<TinyUrlOptions> tinyUrlOptions, IHttpClientFactory factory)
    {
      this.storageConnectionString = storageOptions.Value.ConnectionString;
      this.restClient = factory.CreateClient();
      this.restClient.BaseAddress = new Uri($"https://api.tinyurl.com");
      this.restClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
      this.restClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tinyUrlOptions.Value.ApiKey);
    }

    private async Task EnsureContainer(string container, CancellationToken cancellationToken = default)
    {
      var client = new BlobContainerClient(storageConnectionString, container);
      await client.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
    }

    private Task<string> GetTinyUrl(string url, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(url);
      //var data = new TinyUrlRequest () { Url = url };
      //var response = await this.restClient.PostAsJsonAsync("/create", data);
      //response.EnsureSuccessStatusCode();
      //var result = await response.Content.ReadFromJsonAsync<TinyUrlResponse>();
      //return result?.Data?.Url ?? url;
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

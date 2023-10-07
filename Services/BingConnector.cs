using ElectricRaspberry.Models;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElectricRaspberry.Services;

public interface IWebSearchConnector
{
  Task<IEnumerable<WebPage>> SearchAsync(string query, int count = 1, int offset = 0, CancellationToken cancellationToken = default);
}

public sealed class BingConnector : IWebSearchConnector
{
  private readonly ILogger _logger;
  private readonly HttpClient _httpClient;
  private readonly string? _apiKey;

  public BingConnector(string apiKey, IHttpClientFactory httpFactory, ILogger? logger = null)
  {
    _apiKey = apiKey;
    _logger = logger ?? NullLogger.Instance;
    _httpClient = httpFactory.CreateClient();
  }

  /// <inheritdoc/>
  public async Task<IEnumerable<WebPage>> SearchAsync(string query, int count = 1, int offset = 0, CancellationToken cancellationToken = default)
  {
    if (count <= 0) { throw new ArgumentOutOfRangeException(nameof(count)); }

    if (count >= 50) { throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} value must be less than 50."); }

    if (offset < 0) { throw new ArgumentOutOfRangeException(nameof(offset)); }

    Uri uri = new($"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count={count}&offset={offset}");

    _logger.LogDebug("Sending request: {Uri}", uri);

    using HttpResponseMessage response = await SendGetRequestAsync(uri, cancellationToken).ConfigureAwait(false);

    _logger.LogDebug("Response received: {StatusCode}", response.StatusCode);

    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

    // Sensitive data, logging as trace, disabled by default
    _logger.LogTrace("Response content received: {Data}", json);

    BingSearchResponse? data = JsonSerializer.Deserialize<BingSearchResponse>(json);

    WebPage[]? results = data?.WebPages?.Value;

    return results == null ? Enumerable.Empty<WebPage>() : results;
  }

  /// <summary>
  /// Sends a GET request to the specified URI.
  /// </summary>
  /// <param name="uri">The URI to send the request to.</param>
  /// <param name="cancellationToken">A cancellation token to cancel the request.</param>
  /// <returns>A <see cref="HttpResponseMessage"/> representing the response from the request.</returns>
  private async Task<HttpResponseMessage> SendGetRequestAsync(Uri uri, CancellationToken cancellationToken = default)
  {
    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

    if (!string.IsNullOrEmpty(_apiKey))
    {
      httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
    }

    return await _httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
  }

  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated", Justification = "Class is instantiated through deserialization.")]
  private sealed class BingSearchResponse
  {
    [JsonPropertyName("webPages")]
    public WebPages? WebPages { get; set; }
  }

  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated", Justification = "Class is instantiated through deserialization.")]
  private sealed class WebPages
  {
    [JsonPropertyName("value")]
    public WebPage[]? Value { get; set; }
  }
}

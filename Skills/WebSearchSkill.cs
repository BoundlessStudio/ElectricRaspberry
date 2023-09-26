using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElectricRaspberry.Skills;

/// <summary>
/// Web search engine plugin (e.g. Bing).
/// </summary>
public sealed class WebSearchSkill
{
  /// <summary>
  /// The count parameter name.
  /// </summary>
  public const string CountParam = "count";

  /// <summary>
  /// The offset parameter name.
  /// </summary>
  public const string OffsetParam = "offset";

  private readonly IWebSearchConnector _connector;

  /// <summary>
  /// Initializes a new instance of the <see cref="WebSearchSkill"/> class.
  /// </summary>
  /// <param name="connector">The web search engine connector.</param>
  public WebSearchSkill(IWebSearchConnector connector)
  {
    _connector = connector;
  }

  /// <summary>
  /// Performs a web search using the provided query, count, and offset.
  /// </summary>
  /// <param name="query">The text to search for.</param>
  /// <param name="count">The number of results to return. Default is 3.</param>
  /// <param name="offset">The number of results to skip. Default is 0.</param>
  /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
  /// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains the search results as a string.</returns>
  [SKFunction, Description("Perform a web search.")]
  public async Task<string> SearchAsync(
    [Description("Search query")] string query,
    [Description("Number of results")] int count = 3,
    [Description("Number of results to skip")] int offset = 0
  )
  {
    var results = await _connector.SearchAsync(query, count < 1 ? 1 : count, offset).ConfigureAwait(false);
    if (!results.Any())
      throw new InvalidOperationException("Failed to get a response from the web search engine.");

    return count == 1 ? results.FirstOrDefault() ?? string.Empty : JsonSerializer.Serialize(results);
  }
}



/// <summary>
/// Web search engine connector interface.
/// </summary>
public interface IWebSearchConnector
{
  /// <summary>
  /// Execute a web search engine search.
  /// </summary>
  /// <param name="query">Query to search.</param>
  /// <param name="count">Number of results.</param>
  /// <param name="offset">Number of results to skip.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
  /// <returns>First snippet returned from search.</returns>
  Task<IEnumerable<string>> SearchAsync(string query, int count = 1, int offset = 0, CancellationToken cancellationToken = default);
}


/// <summary>
/// Bing API connector.
/// </summary>
public sealed class BingConnector : IWebSearchConnector
{
  private readonly ILogger _logger;
  private readonly HttpClient _httpClient;
  private readonly string? _apiKey;


  /// <summary>
  /// Initializes a new instance of the <see cref="BingConnector"/> class.
  /// </summary>
  /// <param name="apiKey">The API key to authenticate the connector.</param>
  /// <param name="httpClient">The HTTP client to use for making requests.</param>
  /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
  public BingConnector(string apiKey, IHttpClientFactory httpFactory, ILogger? logger = null)
  {
    _apiKey = apiKey;
    _logger = logger ?? NullLogger.Instance;
    _httpClient = httpFactory.CreateClient();
  }

  /// <inheritdoc/>
  public async Task<IEnumerable<string>> SearchAsync(string query, int count = 1, int offset = 0, CancellationToken cancellationToken = default)
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

    return results == null ? Enumerable.Empty<string>() : results.Select(x => x.Snippet);
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

  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
      Justification = "Class is instantiated through deserialization.")]
  private sealed class BingSearchResponse
  {
    [JsonPropertyName("webPages")]
    public WebPages? WebPages { get; set; }
  }

  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
      Justification = "Class is instantiated through deserialization.")]
  private sealed class WebPages
  {
    [JsonPropertyName("value")]
    public WebPage[]? Value { get; set; }
  }

  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
      Justification = "Class is instantiated through deserialization.")]
  private sealed class WebPage
  {
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;
  }
}


using ElectricRaspberry.Models;
using ElectricRaspberry.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.SkillDefinition;
using PuppeteerSharp;
using PuppeteerSharp.Dom;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace ElectricRaspberry.Skills;

//public sealed class WebSearchSkill
//{
//  private readonly string goal;
//  private readonly IUser user;
//  private readonly IWebSearchConnector connector;
//  private readonly ConnectOptions connectOptions;
//  private readonly IChatCompletion chatCompletion;
//  private readonly IStorageService storageService;

//  // Add Goal for LLM
//  public WebSearchSkill(string goal, IUser user, IOptions<BingOptions> bingOptions, IOptions<BrowserlessOptions> browserlessOptions, IKernel kernel, IStorageService storageService, IHttpClientFactory httpFactory)
//  {
//    this.goal = goal;
//    this.user = user;
//    this.connector = new BingConnector(bingOptions.Value.ApiKey, httpFactory);
//    this.connectOptions = new ConnectOptions() { BrowserWSEndpoint = $"wss://chrome.browserless.io?token={browserlessOptions.Value.ApiKey}" };
//    this.chatCompletion = kernel.GetService<IChatCompletion>("gpt4-32k");
//    this.storageService = storageService;
//  }

//  [SKFunction, Description("Perform a search engine query")]
//  public async Task<string> SearchAsync([Description("Search query")] string query)
//  {
//    var results = await connector.SearchAsync(query, 3, 0).ConfigureAwait(false);
//    if (!results.Any())
//      throw new InvalidOperationException("Failed to get a response from the web search engine.");

//    var chat = chatCompletion.CreateNewChat();
//    chat.AddSystemMessage("Instructions: Using the goal as your focus summarize the json results of this web search into a single paragraph. Goal: " + goal);
//    foreach (var item in results)
//    {
//      var json = JsonSerializer.Serialize(item);
//      chat.AddUserMessage(json);
//    }
    
//    return await chatCompletion.GenerateMessageAsync(chat);
//  }

//  //[SKFunction, Description("Read a web page")]
//  //public async Task<string> ReadAsync([Description("Url for page to load")] string url)
//  //{
//  //  await using var browser = await Puppeteer.ConnectAsync(connectOptions);
//  //  await using var page = await browser.NewPageAsync();
//  //  await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 768 });
//  //  var response = await page.GoToAsync(url);

//  //  // Prioritize 'article' tags
//  //  var articles = await page.QuerySelectorAllAsync<HtmlElement>("article");
//  //  if (articles.Any())
//  //  {
//  //    return await GetSummary(articles);
//  //  }

//  //  // Fallback to 'main' tag
//  //  var main = await page.QuerySelectorAsync("main");
//  //  if (main is not null)
//  //  {
//  //    var text = await main.ToDomHandle<HtmlElement>().GetInnerHtmlAsync();
//  //    return await GetSummary(articles);
//  //  }

//  //  // Fallback to 'body' tag
//  //  var body = await page.QuerySelectorAsync<HtmlElement>("body");
//  //  return await GetSummary(body);
//  //}

//  private async Task<string> GetSummary(HtmlElement element)
//  {
//    var text = await element.GetInnerTextAsync();
//    var chat = chatCompletion.CreateNewChat();
//    chat.AddSystemMessage("Instructions: Using the goal as your focus summarize the web page into a single paragraph. Goal: " + goal);
//    chat.AddUserMessage(text);
//    return await chatCompletion.GenerateMessageAsync(chat);
//  }

//  private async Task<string> GetSummary(HtmlElement[] elements)
//  {
//    var chat = chatCompletion.CreateNewChat();
//    chat.AddSystemMessage("Instructions: Using the goal as your focus summarize the web page into a single paragraph. Goal: " + goal);
//    foreach (var element in elements)
//    {
//      var text = await element.GetInnerTextAsync();
//      chat.AddUserMessage(text);
//    }
//    return await chatCompletion.GenerateMessageAsync(chat);
//  }
//}


//public interface IWebSearchConnector
//{
//  Task<IEnumerable<WebPage>> SearchAsync(string query, int count = 1, int offset = 0, CancellationToken cancellationToken = default);
//}

//public sealed class BingConnector : IWebSearchConnector
//{
//  private readonly ILogger _logger;
//  private readonly HttpClient _httpClient;
//  private readonly string? _apiKey;

//  public BingConnector(string apiKey, IHttpClientFactory httpFactory, ILogger? logger = null)
//  {
//    _apiKey = apiKey;
//    _logger = logger ?? NullLogger.Instance;
//    _httpClient = httpFactory.CreateClient();
//  }

//  /// <inheritdoc/>
//  public async Task<IEnumerable<WebPage>> SearchAsync(string query, int count = 1, int offset = 0, CancellationToken cancellationToken = default)
//  {
//    if (count <= 0) { throw new ArgumentOutOfRangeException(nameof(count)); }

//    if (count >= 50) { throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} value must be less than 50."); }

//    if (offset < 0) { throw new ArgumentOutOfRangeException(nameof(offset)); }

//    Uri uri = new($"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count={count}&offset={offset}");

//    _logger.LogDebug("Sending request: {Uri}", uri);

//    using HttpResponseMessage response = await SendGetRequestAsync(uri, cancellationToken).ConfigureAwait(false);

//    _logger.LogDebug("Response received: {StatusCode}", response.StatusCode);

//    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

//    // Sensitive data, logging as trace, disabled by default
//    _logger.LogTrace("Response content received: {Data}", json);

//    BingSearchResponse? data = JsonSerializer.Deserialize<BingSearchResponse>(json);

//    WebPage[]? results = data?.WebPages?.Value;

//    return results == null ? Enumerable.Empty<WebPage>() : results;
//  }

//  /// <summary>
//  /// Sends a GET request to the specified URI.
//  /// </summary>
//  /// <param name="uri">The URI to send the request to.</param>
//  /// <param name="cancellationToken">A cancellation token to cancel the request.</param>
//  /// <returns>A <see cref="HttpResponseMessage"/> representing the response from the request.</returns>
//  private async Task<HttpResponseMessage> SendGetRequestAsync(Uri uri, CancellationToken cancellationToken = default)
//  {
//    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

//    if (!string.IsNullOrEmpty(_apiKey))
//    {
//      httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
//    }

//    return await _httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
//  }

//  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated", Justification = "Class is instantiated through deserialization.")]
//  private sealed class BingSearchResponse
//  {
//    [JsonPropertyName("webPages")]
//    public WebPages? WebPages { get; set; }
//  }

//  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated", Justification = "Class is instantiated through deserialization.")]
//  private sealed class WebPages
//  {
//    [JsonPropertyName("value")]
//    public WebPage[]? Value { get; set; }
//  }
//}

//public sealed class WebPage
//{
//  [JsonPropertyName("name")]
//  public string Name { get; set; } = string.Empty;

//  [JsonPropertyName("url")]
//  public string Url { get; set; } = string.Empty;

//  [JsonPropertyName("snippet")]
//  public string Snippet { get; set; } = string.Empty;
//}
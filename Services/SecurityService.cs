using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneOf;
using Polly;
using Polly.Caching;
using Polly.RateLimit;
using Polly.Timeout;
using RestSharp;

public interface ISecurityService
{
  IAuthorizedUser GetAuthorizedUser(ClaimsPrincipal principal);
  Task<OneOf<TimeoutRejectedError, RateLimitRejectedError, RetryLimitExceededError, Auth0.ManagementApi.Models.User>> GetAuthenticatedUser(IAuthorizedUser user);
  Task<GraphServiceClient> GetGraphClient(Auth0.ManagementApi.Models.User user);
}

public class SecurityService : ISecurityService
{
  private readonly AuthOptions authOptions;
  private readonly MsGraphOptions msGraphOptions;
  private readonly IAsyncCacheProvider cacheProvider;

  public SecurityService(IOptions<AuthOptions> authOptions, IOptions<MsGraphOptions> msGraphOptions, IAsyncCacheProvider cacheProvider)
  {
    this.authOptions = authOptions.Value;
    this.msGraphOptions = msGraphOptions.Value;
    this.cacheProvider = cacheProvider;
  }
  
  public IAuthorizedUser GetAuthorizedUser(ClaimsPrincipal principal) 
  {
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new NullReferenceException("Claims Principal missing claims for sub");
    var name = principal.FindFirstValue("https://electric-raspberry.ngrok.app/name") ?? throw new NullReferenceException("Claims Principal missing claims for name");
    var picture = principal.FindFirstValue("https://electric-raspberry.ngrok.app/picture") ?? throw new NullReferenceException("Claims Principal missing claims for picture");
    var nickname = principal.FindFirstValue("https://electric-raspberry.ngrok.app/nickname") ?? throw new NullReferenceException("Claims Principal missing claims for nickname");
    var given_name = principal.FindFirstValue("https://electric-raspberry.ngrok.app/given_name")?.ToLowerInvariant();
    var family_name = principal.FindFirstValue("https://electric-raspberry.ngrok.app/family_name")?.ToLowerInvariant();
    var mention = (given_name, family_name) switch
    {
        (null, not null) => $"@{family_name}",
        (not null, null) => $"@{given_name}",
        (not null, not null) => $"@{given_name}.{family_name}",
        _ => $"@{nickname}",
    };
    return new AuthorizedUser(userId, name, mention, picture);
  }

  public async Task<OneOf<TimeoutRejectedError, RateLimitRejectedError, RetryLimitExceededError, Auth0.ManagementApi.Models.User>> GetAuthenticatedUser(IAuthorizedUser user)
  {
    try
    {
      var cxt = new Context(user.UserId);
      var cache = Policy.CacheAsync(cacheProvider, TimeSpan.FromMinutes(60));
      var limit = Policy.RateLimitAsync(20, TimeSpan.FromSeconds(1));
      var timeout = Policy.TimeoutAsync(30, TimeoutStrategy.Pessimistic);
      var retry = Policy.Handle<ErrorApiException>().RetryAsync(3);

      var policyWrap = Policy.WrapAsync(cache, retry, timeout, limit);
      return await policyWrap.ExecuteAsync(async (context) => {
        return await InternalGetAuthenticatedUser(user);
      }, cxt);
    }
    catch (RetryLimitExceededException)
    {
      return new RetryLimitExceededError();
    }
    catch (TimeoutRejectedException)
    {
      return new TimeoutRejectedError();
    }
    catch (RateLimitRejectedException ex)
    {
      return new RateLimitRejectedError(ex.RetryAfter);
    }
  }

  private async Task<Auth0.ManagementApi.Models.User> InternalGetAuthenticatedUser(IAuthorizedUser user)
  {
    var restClient = new RestClient();
    var request = new RestRequest($"{this.authOptions.Domain}/oauth/token", Method.Post);
    request.AddHeader("content-type", "application/json");
    var data = new {
      client_id = this.authOptions.ClientId,
      client_secret = this.authOptions.ClientSecret,
      audience = $"{this.authOptions.Domain}/api/v2/",
      grant_type = "client_credentials"
    };
    var json = JsonConvert.SerializeObject(data);
    request.AddParameter("application/json", json, ParameterType.RequestBody);
    var response = await restClient.ExecuteAsync(request);
    var result = JObject.Parse(response.Content ?? string.Empty);
    string access_token = result["access_token"]?.Value<string>() ?? string.Empty;
    var authClient = new ManagementApiClient(access_token, new Uri($"{this.authOptions.Domain}/api/v2"));
    return await authClient.Users.GetAsync(user.UserId);
  }

  public async Task<GraphServiceClient> GetGraphClient(Auth0.ManagementApi.Models.User user) 
  {
    var client_id = this.msGraphOptions.ClientId;
    var client_secret = this.msGraphOptions.ClientSecret;

    var identity = user.Identities.FirstOrDefault(i => i.Provider == "windowslive");
    var refresh_token = identity?.RefreshToken ?? string.Empty;
    var client = new RestClient();
    var request = new RestRequest("https://login.microsoftonline.com/consumers/oauth2/v2.0/token", Method.Post);
    request.AddHeader("content-type", "application/x-www-form-urlencoded");
    request.AddParameter("application/x-www-form-urlencoded", $"grant_type=refresh_token&client_id={client_id}&client_secret={client_secret}&refresh_token={refresh_token}", ParameterType.RequestBody);
    var response = await client.ExecuteAsync(request);
    var result = JObject.Parse(response.Content ?? string.Empty);
    string access_token = result["access_token"]?.Value<string>() ?? string.Empty;

    var handlers = GraphClientFactory.CreateDefaultHandlers(new DelegateAuthenticationProvider((req) => 
    {
      req.Headers.Authorization = new AuthenticationHeaderValue(scheme: "bearer", parameter: access_token);
      return Task.CompletedTask;
    }));

    return new GraphServiceClient(GraphClientFactory.Create(handlers));
  }
}

public partial class RetryLimitExceededError
{
}

public partial class TimeoutRejectedError
{
}

public partial class RateLimitRejectedError
{
  public string retryAfter { get; }

  public RateLimitRejectedError(TimeSpan retry)
  {
    this.retryAfter = DateTimeOffset.UtcNow.Add(retry).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
  }
}
using System.Net.Http.Headers;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Extensions.Logging;

class MSALHelper 
{
  private MSALHelper() {}
  static public async Task<GraphServiceClient> CreateGraphServiceClientAsync(MsGraphConfiguration graphApiConfiguration, ILogger logger) 
  {
    var appAuth = PublicClientApplicationBuilder.Create(graphApiConfiguration.ClientId)
      .WithRedirectUri(graphApiConfiguration.RedirectUri.ToString())
      .WithAuthority(AzureCloudInstance.AzurePublic, graphApiConfiguration.TenantId)
      .Build();

    var storageProperties = new StorageCreationPropertiesBuilder(".msalcache.bin", "./").Build();
    var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
    cacheHelper.RegisterCache(appAuth.UserTokenCache);

    // Add authentication handler.
    IList<DelegatingHandler> handlers = GraphClientFactory.CreateDefaultHandlers(new DelegateAuthenticationProvider(async (req) =>
      {
        var scopes = graphApiConfiguration.Scopes.ToArray();
        try
        {
            var accounts = await appAuth.GetAccountsAsync();
            var authResult = await appAuth.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
            req.Headers.Authorization = new AuthenticationHeaderValue(scheme: "bearer", parameter: authResult.AccessToken);
        }
        catch (MsalUiRequiredException) 
        {
            // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
            var authResult = await appAuth.AcquireTokenInteractive(scopes).ExecuteAsync();
            req.Headers.Authorization = new AuthenticationHeaderValue(scheme: "bearer", parameter: authResult.AccessToken);
        }
        catch (Exception)
        {
        }
      }));

    // Add logging handler to log Graph API requests and responses request IDs.
    handlers.Add(new MsGraphClientLoggingHandler(logger));

    // Create the Graph client.
    return new GraphServiceClient(GraphClientFactory.Create(handlers));
  }
}
using System.Security.Claims;
using Microsoft.Graph;

public interface ISecurityService
{
  IAuthorizedUser GetUser(ClaimsPrincipal principal);
}

public class SecurityService : ISecurityService
{
  public SecurityService()
  {
  }
  
  public IAuthorizedUser GetUser(ClaimsPrincipal principal) 
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
}
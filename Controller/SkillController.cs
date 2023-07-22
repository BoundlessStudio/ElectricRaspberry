using System.Security.Claims;
using Auth0.ManagementApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

public class SkillController
{
  public async Task<Results<Ok<IEnumerable<SkillDocument>>, NotFound, ProblemHttpResult>> List(ClaimsPrincipal principal, ISecurityService security, FeedsContext context)
  {
    var user = security.GetAuthorizedUser(principal);
    var result = await security.GetAuthenticatedUser(user);
    return result.Value switch
    {
      User details => GetUserSkills(context, details),
      TimeoutRejectedError _ => TypedResults.Problem(statusCode: 408),
      RateLimitRejectedError _ => TypedResults.Problem(statusCode: 429),
      RetryLimitExceededError _ => TypedResults.Problem(statusCode: 429),
      _ => TypedResults.Problem(statusCode: 501)
    };
  }

  private static Ok<IEnumerable<SkillDocument>> GetUserSkills(FeedsContext context, User details)
  {
    var identities = details.Identities.Where(_ => _.IsSocial == true).Select(_ => _.Connection).ToList();
    var validOwners = new List<string>(identities) { SkillRecord.SystemOwner, details.UserId };
    IEnumerable<SkillDocument> collection = context.Skills.Where(_ => validOwners.Contains(_.Owner)).Select(_ => _.ToDocument()).ToList();
    return TypedResults.Ok(collection);
  }
}
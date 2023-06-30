using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// TODO: Link OpenAPI spec To Web Client

public class FeedController
{
  
  public async Task<Results<Ok<IEnumerable<FeedDocument>>, NoContent>> List(ClaimsPrincipal principal, ISecurityService security, FeedsContext context)
  {
    var user = security.GetUser(principal);
    var query = await context.Feeds.Where(_ => _.Access == FeedAccess.Private && _.OwnerId == user.UserId).ToListAsync();
    if(query.Any() == false) return TypedResults.NoContent();
    var collection = query.Select(_ => _.ToDocument()).AsEnumerable();
    return TypedResults.Ok(collection);
  }

  public async Task<Results<Ok<FeedDocument>, NotFound>> Get(ClaimsPrincipal principal, ISecurityService security, FeedsContext context, [FromRoute]string feed_id)
  {
    var user = security.GetUser(principal);
    var model = await context.Feeds.FindAsync(feed_id);
    if(model is null) return TypedResults.NotFound();
    if(model.Access == FeedAccess.Private && model.OwnerId != user.UserId) return TypedResults.NotFound();
    var document = model.ToDocument();
    return TypedResults.Ok(document);
  }
 

  public async Task<Results<Created<FeedDocument>, BadRequest, NotFound>> Create(ClaimsPrincipal principal, ISecurityService security, FeedsContext context, [FromBody]FeedCreateDocument dto)
  {
    var user = security.GetUser(principal);

    var model = new FeedRecord()
    { 
      FeedId = Guid.NewGuid().ToString(), 
      OwnerId = user.UserId,
      Name = dto.Name, 
      Description = string.Empty,
      Access = FeedAccess.Private,
    };
    context.Feeds.Add(model);
    await context.SaveChangesAsync();

    // TODO: Deal with dto.Template and copy comments from a existing template.

    var document = model.ToDocument();
    return TypedResults.Created($"/api/feed/{model.FeedId}", document);
  }

  public async Task<Results<NoContent, NotFound>> Edit(ClaimsPrincipal principal, ISecurityService security, FeedsContext context, [FromBody]FeedEditDocument dto)
  {
    var user = security.GetUser(principal);
    var model = await context.Feeds.FindAsync(dto.FeedId);
    if(model is null) return TypedResults.NotFound();
    if(model.OwnerId != user.UserId) return TypedResults.NotFound();
    model.Name = dto.Name;
    model.Description = dto.Description;
    model.Access = dto.Access;
    await context.SaveChangesAsync();

    return TypedResults.NoContent();
  }

  public async Task<Results<NoContent, NotFound>> Delete(ClaimsPrincipal principal, ISecurityService security, FeedsContext context, [FromRoute]string feed_id)
  {
    var user = security.GetUser(principal);
    var model = await context.Feeds.FindAsync(feed_id);
    if(model is null) return TypedResults.NotFound();
    if(model.OwnerId != user.UserId) return TypedResults.NotFound();
    var comments = context.Comments.Where(_ => _.FeedId == feed_id);
    context.Comments.RemoveRange(comments);
    context.Feeds.Remove(model);
    await context.SaveChangesAsync();
    return TypedResults.NoContent();
  }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class FeedHub : Hub
{
  // this.Context
  // this.Context.Features
  // this.Context.ConnectionId
  // this.Context.UserIdentifier
  // this.Context.Items
  // this.Context.User
  // this.Clients
  // this.Clients.All
  // this.Clients.Caller
  // this.Clients.Others
  // this.Groups

  public async Task JoinFeed(string feedId, FeedsContext dbContext, ISecurityService security)
  {
    var principal = this.Context.User ?? throw new HubException("Not able to join feed because because you are not authorized user.");
    var user = security.GetAuthorizedUser(principal);
    var feed = await dbContext.Feeds.FindAsync(feedId);
    if(feed is null) throw new HubException("Not able to join feed because because it dose not exist.");
    if(feed.Access == FeedAccess.Private && feed.OwnerId != user.UserId) throw new HubException("Not able to join feed because because it is private.");
    await Groups.AddToGroupAsync(Context.ConnectionId, feedId);
  }
}
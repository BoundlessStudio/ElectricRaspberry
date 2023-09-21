using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class ClientHub : Hub
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


  public ClientHub()
  {
  }

  public override Task OnConnectedAsync()
  {
    return base.OnConnectedAsync();
  }

  public override Task OnDisconnectedAsync(Exception? exception)
  {
    return base.OnDisconnectedAsync(exception);
  }
}
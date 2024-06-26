using Microsoft.AspNetCore.Mvc;

namespace ElectricRaspberry.Extensions;

public static class ExtensionsRouting
{
  internal static RouteGroupBuilder AddAuthorization(this RouteGroupBuilder group)
  {
    group.RequireAuthorization();
    group.WithMetadata(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized));
    return group;
  }
}
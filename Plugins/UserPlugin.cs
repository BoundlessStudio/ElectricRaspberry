using ElectricRaspberry.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;

namespace ElectricRaspberry.Plugins
{
  public class UserPlugin
  {
    private readonly IUser user;

    public UserPlugin(IUser user)
    {
      this.user = user;
    }

    [SKFunction, Description("Get user details. Use this function to get the user infomation including: name, region, location, and timezone")]
    public Task<string> GetUserDetails()
    {
      var details = new StringBuilder();
      details.AppendLine($"Name: {user.Name}");
      details.AppendLine($"Region: {user.City}, {user.Country}");
      details.AppendLine($"Location: ({user.Latitude},{user.Longitude})");
      details.AppendLine($"Timezone: {user.Timezone}");
      var results = details.ToString();
      return Task.FromResult(results);
    }
  }
}

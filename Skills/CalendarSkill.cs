using ElectricRaspberry.Models;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;

namespace ElectricRaspberry.Skills
{
  public class CalendarSkill
  {
    private readonly IUser user;
    public CalendarSkill(IUser user)
    {
      this.user = user;
    }

    [SKFunction, Description("Get the current date time in the user's timezone.")]
    public string Now()
    {
      var time = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, this.user.Timezone);
      return time.ToString("D");
    }

  }
}
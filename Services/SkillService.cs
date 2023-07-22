using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.Web;

public interface ISkillService
{
}

public class SkillService : ISkillService
{
  public SkillService()
  {    
  }

  public Dictionary<string, string> GetAvailableSkills()
  {
    return new Dictionary<string, string>()
    {
      {nameof(TimeSkill), "System.Time"},
      {nameof(TextMemorySkill), "System.Memory"},
      {nameof(LanguageCalculatorSkill), "System.Math"},
      {nameof(WebSearchEngineSkill), "Bing.Search"},
      {nameof(CalendarSkill), "Microsoft.Calendar"},
      {nameof(CloudDriveSkill), "Microsoft.Drive"},
      {nameof(EmailSkill), "Microsoft.Email"},
      {nameof(TaskListSkill), "Microsoft.Tasks"},
      {nameof(WaitSkill), "System.Wait"},
    };
  }
}
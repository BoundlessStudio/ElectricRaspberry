using Auth0.ManagementApi.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;

namespace ElectricRaspberry.Skills
{
  public class UserFeedbackSkill
  {
    private readonly IHubContext<ClientHub> hub;
    private readonly string connectionId;

    public UserFeedbackSkill(IHubContext<ClientHub> hub, string connectionId)
    {
      this.hub = hub;
      this.connectionId = connectionId;
    }

    [SKFunction, Description("Get user feedback in the form a simple text question and answer.")]
    public async Task<string> AskQuestionAsync([Description("A question for the user to answer.")] string input, SKContext context)
    {

      return await this.hub.Clients.Client(this.connectionId).InvokeAsync<string>("AskQuestion", input, default);
    }
  }
}

// [ ] User Feedback [Question => Answer]
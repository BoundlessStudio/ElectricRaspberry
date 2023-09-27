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
    public async Task<string> AskQuestionAsync([Description("A question for the user to answer.")] string question)
    {
      var result = await InvokePrompt(question);
      return result;
    }

    private async Task<string> InvokePrompt(string question)
    {
      using var cts = new CancellationTokenSource();
      try
      {
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var result = await this.hub.Clients.Client(this.connectionId).InvokeAsync<string>("AskQuestion", question, cts.Token);
        return "The user reponded with: " + result;
      }
      catch (OperationCanceledException)
      {
        return "The prompt timed out after 10 seconds.";
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }
  }
}

// [ ] User Feedback [Question => Answer]
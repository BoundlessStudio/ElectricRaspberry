using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectricRaspberry.Skills;

public class JavascriptBrowserSkill
{
  private readonly IChatCompletion chatCompletion;

  public JavascriptBrowserSkill(IKernel kernel)
  {
    this.chatCompletion = kernel.GetService<IChatCompletion>("gpt4-32k");
  }

  [SKFunction, Description("Create a table based on a text prompt and url to data.")]
  public async Task<string> CodeTableAsync([Description("Text prompt"), Required] string input, [Description("Url to data source"), Required] string url)
  {
    var chat = chatCompletion.CreateNewChat();
    //chat.AddSystemMessage();
    //chat.AddUserMessage();
    return await chatCompletion.GenerateMessageAsync(chat);
  }

  [SKFunction, Description("Create a timeline based on a text prompt and url to data.")]
  public async Task<string> CodeTimelimeAsync([Description("Text prompt"), Required] string input, [Description("Url to data source"), Required] string url)
  {
    var chat = chatCompletion.CreateNewChat();
    //chat.AddSystemMessage();
    //chat.AddUserMessage();
    return await chatCompletion.GenerateMessageAsync(chat);
  }

  [SKFunction, Description("Create a map based on a text prompt and optional url to data.")]
  public async Task<string> CodeMapAsync([Description("Text prompt")] string input, [Description("Url to data source")] string url)
  {
    var chat = chatCompletion.CreateNewChat();
    //chat.AddSystemMessage();
    //chat.AddUserMessage();
    return await chatCompletion.GenerateMessageAsync(chat);
  }

}

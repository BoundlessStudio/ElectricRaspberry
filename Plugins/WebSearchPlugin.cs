using ElectricRaspberry.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using System.ComponentModel;
using System.Text.Json;

public sealed class WebSearchPlugin
{
  private readonly IWebSearchConnector connector;
  private readonly IChatCompletion chatCompletion;


  public WebSearchPlugin(IKernel kernel, IWebSearchConnector connector)
  {
    this.chatCompletion = kernel.GetService<IChatCompletion>();
    this.connector = connector;
  }

  [SKFunction, Description("Search the web. Use this function to perform a bing search of the web based on the query.")]
  public async Task<string> SearchWebAsync(
    [Description("The search query")] string query
  )
  {
    var results = await connector.SearchAsync(query, 3, 0).ConfigureAwait(false);
    if (!results.Any())
      throw new InvalidOperationException("Failed to get a response from the web search engine.");

    var chat = chatCompletion.CreateNewChat();
    chat.AddSystemMessage("Instructions: Using the goal as your focus summarize the json results of this web search into a single paragraph. Goal: " + goal);
    foreach (var item in results)
    {
      var json = JsonSerializer.Serialize(item);
      chat.AddUserMessage(json);
    }

    return await chatCompletion.GenerateMessageAsync(chat);
  }
}
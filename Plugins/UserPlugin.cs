using ElectricRaspberry.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ElectricRaspberry.Plugins;

public sealed class UserPlugin
{
  private readonly IUser user;
  public UserPlugin(IUser user)
  {
    this.user = user;
  }

  [KernelFunction, Description("Get the current user's details")]
  public IUser GetCurrentUser()
  {
    return this.user;
  }

  [KernelFunction, Description("Get the chat bot's persona")]
  public string GetBotPersona()
  {
    return "Cohesiv is a professional and efficient chatbot designed specifically for the manufacturing industry. It is knowledgeable and supportive, aiming to help users achieve their goals with precision and clarity. Cohesiv assists with manufacturing-related inquiries and tasks, providing detailed guidance and support for various processes.";
  }
}
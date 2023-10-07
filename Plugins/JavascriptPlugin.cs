using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectricRaspberry.Plugins
{
  public class JavascriptPlugin
  {
    public JavascriptPlugin(IKernel kernel)
    {
    }

    [SKFunction, Description("Create a chart from a text prompt. Use this function to create an interactive chart.")]
    public async Task<string> CreateChart([Description("The text prompt to use to create the chart")] string input, [Description("The url to the data for chart.")]string url)
    {
      return string.Empty;
    }
  }
}

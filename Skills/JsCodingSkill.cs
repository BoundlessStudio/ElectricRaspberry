using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ElectricRaspberry.Skills
{
  public class JsCodingSkill
  {
    const string CODE_PROMPT =
@"
Run JavaScript in the user's browser to compute results and visualize data in various ways.
The environment and constraints are designed to capture both the results and any errors that occur during execution.

Constraints
Browser Execution: All code will be run in the browser, not on Node.js.
Pre-loaded Libraries: Libraries are pre-loaded in the environment; there's no need to import or require them again.
Sandbox Environment: This sandbox can access the user's device via browser APIs and contains pre-loaded libraries you will need.
Sandbox Element: You have access to a DIV element with the ID of 'sandbox', which is displayed to the user alongside the final result.
Loader: The code you create is loaded via a asynchronously Function constructor and this function will be invoked asynchronously.
Result Limitations: The code you create MUST return a result. Any results MUST be a string.
Async: You MUST use await/async.
Environment Reset: The environment is reset between calls, requiring the full script for each invocation.
Post-Execution Validation: Use the results to correct any issues and try again until you achieve a successful outcome.
Timeout: The Environment has a timeout of 30s while running the code you create.

Libraries
Danfo.js: Accessible as dfd, for handling CSV and JSON data. Functions include loading, manipulating, and analyzing data.
NumJS: Provides mathematical and statistical operations on array-like structures.
Grid.js: Accessible as gridjs.Grid, for creating interactive tables. You must include the options 'pagination: true', 'search: true', and 'sort: true'.
Google Maps: for the creation and manipulation of interactive maps. Use global varaible 'google.maps'. Wrap the functions in promises to use with await/async to return a result.
Day.js: For handling and manipulating dates and times.
vis-timeline: Available as vis.Timeline and vis.DataSet, for creating interactive timelines.
Plotly: Used for creating various charts and graphs.
JsBarcode: Generates barcode images from data strings. use global variable JsBarcode. You MUST add a svg element with the id of 'barcode' as a child of the 'sandbox' element. You MUST return that the barcode was created. 
";


    private readonly IHubContext<ClientHub> hub;
    private readonly string connectionId;

    public JsCodingSkill(IKernel kernel, IHubContext<ClientHub> hub, string connectionId)
    {
      this.hub = hub;
      this.connectionId = connectionId;
    }

    [SKFunction, Description(CODE_PROMPT)]
    public async Task<string> CodeAsync([Description("javascript code")] string input)
    {
      var match = Regex.Match(input, @"```javascript\s*(.*?)```", RegexOptions.Singleline);
      string code = match.Success ? match.Groups[1].Value : input;
      var result = await InvokeCode(code);
      return result;
    }

    private async Task<string> InvokeCode(string code)
    {
      using var cts = new CancellationTokenSource();
      try
      {
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        var result = await this.hub.Clients.Client(this.connectionId).InvokeAsync<string>("EvalCode", code, cts.Token);
        if(string.IsNullOrWhiteSpace(result))
          return "The environment return no result.";
        if (result.Length > 1000)
          return "The response from the environment is too large to include.";
        else
          return "The environment return:" + result;
      }
      catch (OperationCanceledException)
      {
        return "The environment timed out after 10 seconds.";
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }
  }
}




using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ElectricRaspberry.Skills
{
  public class JsCodingSkill
  {
    const string CODE_PROMPT =
    "Use this skill to create javascript code and run it in the users browser (NOT NODE). " +
    "The environment is reset between calls so you will need to supply the full script. " +
    "Use 'window.navigator' to acess the users device. " +
    "You have access to DIV element with the id of 'sandbox'. This sandbox is displayed to the user along side the final result." +
    "The code you create is loaded via a Function constructor. The function will be invoked asynchronously. " +
    "The code you write must use 'await' instead of '.then()' if invoking a promise. " +
    "The function may return a result that is captured and the 'console.log' is also captured both are return along with any errors. " +
    "Any results returned should be kept short. Do not return Base64 encoded images, complete data sets, or long blocks of text. " +
    "After The function executed successfully you are done you do not need to valid the results. " +
    "The following librarys have been pre-loaded into the browser (DO NOT IMPORT OR REQUIRE THEM AGAIN). " +
    "1. Danfo.js for loading and manipulation of CSV and Json data. Use global varaible 'dfd'. to_html dose not exist in Danfo stop trying to use it. " +
    "2. Chart.js for charting. Use global varaible 'Chart'. You need to add a canvas element to sandbox before use. Do not return Base64 encoded images as there are too large. " +
    "3. Grid.js for Advanced Table Plugin. Use global varaible 'gridjs.Grid'. You must include the following options when creating a Grid: 'pagination: true', 'search: true', and 'sort: true'. " +
    "4. vis.js (vis-timeline) for an interactive visualization chart to visualize data in time. Use global varaibles 'vis.Timeline' and 'vis.DataSet'." +
    "5. Google (Maps JavaScript API) for an interactive map. Use global varaible 'google.maps'. " +
    
    "";


    private readonly IHubContext<ClientHub> hub;
    private readonly string connectionId;

    public JsCodingSkill(IKernel kernel, IHubContext<ClientHub> hub, string connectionId)
    {
      this.hub = hub;
      this.connectionId = connectionId;
    }

    [SKFunction, Description(CODE_PROMPT)]
    public async Task<string> CodeAsync([Description("The javascript code to used with new Function(code)")] string input)
    {
      var match = Regex.Match(input, @"```javascript\s*(.*?)```", RegexOptions.Singleline);
      string code = match.Success ? match.Groups[1].Value : input;

      string reponse = string.Empty;
      try
      {
        reponse = await this.hub.Clients.Client(this.connectionId).InvokeAsync<string>("EvalCode", code, default);
      }
      catch (Exception ex)
      {
        reponse = ex.Message;
      }

      return reponse;
    }
  }
}


//"danfojs (for manipulating, processing, and plotting structured data) has pre-loaded into the browser's javascript sandbox under goal varaible 'dfd'. plotly is also included as a dependency to danfojs plot function. In a recent update the library switched to Camel Case for their function names..\r\n" +
//"Chart.js (for charting) has pre-loaded into the browser's javascript sandbox under goal varaible 'Chart'. If used you will need to add a canvas element to #sandbox.\r\n" +

// ToDo:
// - D3.js
// - chromajs
// - currency.js
// - cheetah-grid
// - jsLPSolver
// - Grid.js
// vnext?
// - GoogleMaps OR OpenStreetMap
// - recursive-diff
// - fluidplayer
// - Drawflow
// - taffydb
// - formkit
// - bulksearch


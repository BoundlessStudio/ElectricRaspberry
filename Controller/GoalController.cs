using ElectricRaspberry.Plugins;
using ElectricRaspberry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using Microsoft.SemanticKernel.Orchestration;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

public class GoalController
{
  public async Task<MessageDocument> Plan(
    ClaimsPrincipal principal,
    //IOptions<OpenAIOptions> openAiOptions,
    IOptions<AzureAIOptions> azureAiOptions,
    IOptions<BingOptions> bingOptions,
    IOptions<LeonardoOptions> leonardoOptions,
    IOptions<BrowserlessOptions> browserlessOptions,
    IOptions<PythonInterpreterOptions> pyOptions,
    IStorageService storageService,
    IHttpClientFactory httpFactory,
    IMemoryCache cache,
    ILoggerFactory logFactory,
    [FromBody]GoalDocument dto)
  {
    var user = principal.GetUser();

    var kernel = new KernelBuilder()
      .WithAzureChatCompletionService("gpt-4-32k", azureAiOptions.Value.Endpoint, azureAiOptions.Value.ApiKey)
      .Build();

    var bing = new BingConnector(bingOptions.Value.ApiKey, httpFactory);

    kernel.ImportFunctions(new JavascriptPlugin(kernel));
    kernel.ImportFunctions(new ScratchpadPlugin(cache));
    kernel.ImportFunctions(new WebSearchPlugin(kernel, bing));
    kernel.ImportFunctions(new UserPlugin(user));
    kernel.ImportFunctions(new ArtistPlugin(user, leonardoOptions, storageService, httpFactory));

    // TODO: Drawing Plugin
    // leonardo vs dalle

    // TOOD: Read Web Page Plugin
    // Use browserless to render web page and get the text.

    // TOOD: Javascript Plugin
    // Return JS code for the followin:
    // 1. Table -> https://gridjs.io/
    // 2. Chart -> https://www.chartjs.org/
    // 3. Diagram -> https://mermaid.js.org/
    // 4. Barcodes -> https://barcode.tec-it.com/barcode.ashx?data=cb95a36b73fef&type=Code128&width=400&height=100&format=png
    // 5. QRCodes -> https://api.qrserver.com/v1/create-qr-code/?data=cb95a36b73fef&size=220x220&format=png
    // 6. Map -> https://developers.google.com/maps/documentation/javascript/overview
    // 7. Timeline -> https://visjs.github.io/vis-timeline/docs/timeline/
    // 8. Calendar -> ?

    // TOOD: Javascript Interpreter
    // Run code

    var instructions = new StringBuilder();
    instructions.AppendLine("[INSTRUCTION]");
    instructions.AppendLine("Use markdown formating for links, images, tables and code blocks.");
    instructions.AppendLine("You are a helpful assistant.");

    var chat = kernel.GetService<IChatCompletion>();
    var history = chat.CreateNewChat();
    history.AddSystemMessage(instructions.ToString());

    foreach (var item in dto.History)
    {
      switch (item.Role)
      {
        case Role.Assistant:
          history.AddAssistantMessage(item.Content);
          break;
        case Role.User:
          history.AddUserMessage(item.Content);
          break;
        case Role.System:
        default:
          break;
      }
    }
    history.AddUserMessage(dto.Goal);

    var functions = kernel.Functions.GetFunctionViews().Select(f => f.ToOpenAIFunction()).ToList();
    var settings = new OpenAIRequestSettings()
    {
      Functions = functions,
      FunctionCall = functions.Count > 0 ? OpenAIRequestSettings.FunctionCallAuto : OpenAIRequestSettings.FunctionCallNone,
    };

    var completions = await chat.GetChatCompletionsAsync(history, settings);
    var completion = completions.FirstOrDefault() ?? throw new InvalidOperationException("Excepted Completion >= 1");
    var message = await completion.GetChatMessageAsync();
    var content = message.Content ?? string.Empty;

    var match = Regex.Match(content, @"```markdown\s*(.*?)```", RegexOptions.Singleline);
    string markdown = match.Success ? match.Groups[1].Value : content;

    var functionResponse = completion.GetFunctionResponse();
    if(functionResponse is null)
      return new MessageDocument() { Role = Role.Assistant, Content = markdown };

    kernel.Functions.TryGetFunctionAndContext(functionResponse, out ISKFunction? fn, out ContextVariables? cxt);
    if (fn is null)
      return new MessageDocument() { Role = Role.Assistant, Content = markdown };

    var result = await kernel.RunAsync(fn, cxt);
    var value = result.GetValue<string>();
    return new MessageDocument() { Role = Role.Assistant, Content = value ?? markdown };
  }
}

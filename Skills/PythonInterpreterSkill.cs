using ElectricRaspberry.Models;
using ElectricRaspberry.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ElectricRaspberry.Skills
{
  public class PythonInterpreterSkill
  {
    private readonly string url;
    private readonly HttpClient client;
    private readonly IUser user;
    private readonly IStorageService storageService;

    public PythonInterpreterSkill(IOptions<PythonInterpreterOptions> pyOptions, IUser user, IStorageService storageService, IHttpClientFactory httpFactory)
    {
      this.url = pyOptions.Value.Domain;
      this.client = httpFactory.CreateClient();
      this.user = user;
      this.storageService = storageService;
    }

    [SKFunction, Description("Excute code in a Python Interpreter. Can download files.")]
    public async Task<string> InvokeAsync([Description("python code")] string input)
    {
      if(string.IsNullOrWhiteSpace(input))
        return "Python code is Requiered.";

      var source = new CancellationTokenSource();
      source.CancelAfter(TimeSpan.FromSeconds(300));

      try
      {
        var match = Regex.Match(input, @"```python\s*(.*?)```", RegexOptions.Singleline);
        string code = match.Success ? match.Groups[1].Value : input;

        // Get Session
        var connectionId = await InitializeKernel(source.Token);
        // Warmup Kernel
        await WaitForReady(connectionId, source.Token);
        // Submit Code
        await InvokeCode(connectionId, code, source.Token);
        // Wait for Results
        var results = await GetResults(connectionId, source.Token);
        return results;
      }
      catch (OperationCanceledException)
      {
        return "The Interpreter timed out.";
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }


    private async ValueTask<string> InitializeKernel(CancellationToken cancellationToken = default)
    {
      var response = await client.PostAsync($"{url}/init", null);
      var result = await response.Content.ReadFromJsonAsync<InitializeResponse>(cancellationToken: cancellationToken);
      var connectionId = result?.connectionId ?? throw new InvalidOperationException("Python interpreter session failed to initialize");
      return connectionId;
    }

    private async ValueTask WaitForReady(string connectionId, CancellationToken cancellationToken = default)
    {
      for (int i = 0; i < 6; i++)
      {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        var initialized = await client.GetStringAsync($"{url}/api/{connectionId}", cancellationToken: cancellationToken);
        if (initialized.Contains("Kernel is ready"))
          return;
      }

      throw new InvalidOperationException("Python interpreter session failed to initialize");
    }

    private async ValueTask InvokeCode(string connectionId, string code, CancellationToken cancellationToken = default)
    {
      var msg = new Message { Type = "Execute", command = code };
      await client.PostAsJsonAsync($"{url}/api/{connectionId}", msg, cancellationToken: cancellationToken);
      await Task.Delay(TimeSpan.FromSeconds(5));
    }

    private async ValueTask<string> GetResults(string connectionId, CancellationToken cancellationToken = default)
    {
      for (int i = 0; i < 12; i++)
      {
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

        var json = await client.GetStringAsync($"{url}/api/{connectionId}", cancellationToken: cancellationToken);
        Debug.WriteLine(json);
        var item = JsonSerializer.Deserialize<GetResult>(json);
        if(item is null)
          return "Failed to get results.";

        if (item.results.Any() == false)
          continue;

        var output = new StringBuilder();
        foreach (var result in item.results)
        {
          switch (result.type)
          {
            case "message" when (result.value.Length > 2000):
            case "message_raw" when (result.value.Length > 2000):
              output.AppendLine("The output is too large to include the full results so it has been truncated to the follow:");
              output.AppendLine(result.value.Substring(0, 2000));
              break;
            case "message" when (result.value.Length < 5):
            case "message_raw" when (result.value.Length < 5):
              output.AppendLine($"The result from the interpreter: {result.value}.");
              break;
            case "message":
            case "message_raw":
              output.AppendLine(result.value);
              break;
            case "image/png":
              var id = Guid.NewGuid().ToString("N").ToLower();
              var url = await storageService.UploadBase64(result.value, user.Container, $"{id}.png");
              output.AppendLine(url);
              break;
            case "error":
              throw new InvalidOperationException(result.value);
            default:
              throw new InvalidOperationException("Unknown Result Type from the Python interpreter.");
          }
        }

        return output.ToString();
      }

      throw new OperationCanceledException("The Interpreter timed out.");
    }
  }

  public class InitializeResponse
  {
    public string connectionId { get; set; }
  }

  public class Message
  {
    public string Type { get; set; }
    public string command { get; set; }
  }

  public class GetResult
  {
    public CodeExecutionResult[] results { get; set; }
  }

  public class CodeExecutionResult
  {
    public string type { get; set; }
    public string value { get; set; }
  }

}
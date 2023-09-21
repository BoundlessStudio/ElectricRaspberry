using ElectricRaspberry.Models;
using ElectricRaspberry.Models.Converter;
using ElectricRaspberry.Services;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SkillDefinition;
using Polly;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ElectricRaspberry.Skills
{
  public class ConverterSkill
  {
    private readonly IUser user;
    private readonly HttpClient restClient;
    private readonly IStorageService storageService;
    private string apiKey;

    public ConverterSkill(IKernel kernel, IUser user, IOptions<ConvertioOptions> options, IStorageService storageService, IHttpClientFactory factory)
    {
      this.user = user;
      this.restClient = factory.CreateClient();
      this.storageService = storageService;
      this.apiKey = options.Value.ApiKey;
    }

    [SKFunction, Description("This funtion takes the url of the file, converts it to the output format and returns a url to the new file.")]
    public async Task<string> ConvertAsync([Description("The url of the file to convert.")] string url, [Description("the format to covert the file to.")] string format)
    {
      var data = new RequestConvertDocument(this.apiKey, url, format);
      var repsonse = await this.restClient.PostAsJsonAsync("https://api.convertio.co/convert", data);
      repsonse.EnsureSuccessStatusCode();
      var result = await repsonse.Content.ReadFromJsonAsync<ReponseConvertDocument>();
      var conversionId = result?.Data?.Id;

      await Task.Delay(TimeSpan.FromSeconds(1));

      var retry = Policy<ReponseStatusOutput?>
        .Handle<Exception>()
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

      var output = await retry.ExecuteAsync(async () => {
        var root = await this.restClient.GetFromJsonAsync<ReponseStatusDocument>($"https://api.convertio.co/convert/{conversionId}/status");
        return root?.Data?.Output;
      });

      var temporary = output?.Url ?? throw new InvalidOperationException("Failed to convert file.");
      var permanent = await this.storageService.CopyFrom(temporary, user.Container);

      return permanent;
    }
  }
}
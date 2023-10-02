using ElectricRaspberry.Models;
using ElectricRaspberry.Services;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.SkillDefinition;
using Polly;
using System.ComponentModel;

namespace ElectricRaspberry.Skills
{
  public class DrawImageSkill
  {
    private readonly IUser user;
    private readonly IStorageService storageService;
    private readonly HttpClient restClient;

    public DrawImageSkill(IUser user, IOptions<LeonardoOptions> options, IStorageService storageService, IHttpClientFactory factory)
    {
      this.user = user;
      this.storageService = storageService;
      this.restClient = factory.CreateClient();
      this.restClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
      this.restClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
    }

    [SKFunction, Description("Generate an image from a text prompt and returns a URL")]
    public async Task<string> DrawImageAsync(
      [Description("The prompt for the image generation")] string prompt, 
      [Description("The height of the image. Defaults to 768. Must be between 32 and 1024 and be a multiple of 8.")] int height = 768, 
      [Description("The width of the image. Defaults to 512. Must be between 32 and 1024 and be a multiple of 8.")] int width = 512
    )
    {
      var request = new ImageGenerationRequest()
      {
        NegativePrompt = "bad anatomy, bad draw face, low quality body, worst quality body, bad draw body, bad draw anatomy, low quality face, bad art, low quality anatomy, bad proportions, gross proportions, flowers, blurry, crossed eyes, ugly, bizarre, poorly drawn, poorly drawn face, poorly drawn hands, poorly drawn limbs, poorly drawn fingers, out of frame, body out of frame, deformed, disfigured, mutation, mutated hands, mutated limbs. mutated face, malformed, malformed limbs, extra fingers, children, kid",
        Prompt = prompt,
        Width = width,
      };

      var response = await this.restClient.PostAsJsonAsync("https://cloud.leonardo.ai/api/rest/v1/generations", request) ?? throw new InvalidOperationException("Error posting generation job");
      response.EnsureSuccessStatusCode();
      var json = await response.Content.ReadAsStringAsync();
      var generation = System.Text.Json.JsonSerializer.Deserialize<GenerationJob>(json) ?? throw new InvalidOperationException("Could not prase the GenerationJob object");

      await Task.Delay(TimeSpan.FromSeconds(5));

      var retry = Policy<GeneratedImage?>
        .Handle<Exception>()
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

      var image = await retry.ExecuteAsync(async () => {
        var root = await this.restClient.GetFromJsonAsync<GenerationJobStatus>("https://cloud.leonardo.ai/api/rest/v1/generations/" + generation.Job.GenerationId);
        if (root is null)
          throw new InvalidOperationException("failed to get status of job.");
        else if (root.Generations.Status != "COMPLETE")
          throw new InvalidOperationException("incomplete job.");
        else
          return root?.Generations?.GeneratedImages?.FirstOrDefault();
      });

      var temporary = image?.Url ?? throw new InvalidOperationException("Failed to generate an image.");
      var permanent = await this.storageService.CopyFrom(temporary, user.Container);

      var name = prompt.Substring(0, 10);
      return $"![{name}]({permanent})";
    }
  }
}
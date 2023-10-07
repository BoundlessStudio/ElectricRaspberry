using ElectricRaspberry.Models;
using ElectricRaspberry.Services;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectricRaspberry.Plugins
{
  public class ArtistPlugin
  {
    private readonly IUser user;
    private readonly IStorageService storageService;
    private readonly HttpClient restClient;

    public ArtistPlugin(IUser user, IOptions<LeonardoOptions> options, IStorageService storageService, IHttpClientFactory factory)
    {
      this.user = user;
      this.storageService = storageService;
      this.restClient = factory.CreateClient();
      this.restClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
      this.restClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
    }

    [SKFunction, Description("Image generation. Use this function to create a image from a text prompt.")]
    public async Task<string> GenerationImageAsync(
      [Description("The prompt for the image generation.")] string prompt,
      [Description("The height of the image."), Required, DefaultValue(768), Range(512, 1024)] int height = 768,
      [Description("The width of the image."), Required, DefaultValue(512), Range(512, 1024)] int width = 512
    )
    {
      try
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

        GeneratedImage? image = null;
        for (int i = 0; i < 12; i++)
        {
          await Task.Delay(TimeSpan.FromSeconds(10));

          var root = await this.restClient.GetFromJsonAsync<GenerationJobStatus>("https://cloud.leonardo.ai/api/rest/v1/generations/" + generation.Job.GenerationId);
          if (root is null)
            continue;

          if (root.Generations.Status != "COMPLETE")
            continue;

          image = root?.Generations?.GeneratedImages?.FirstOrDefault();
          break;
        }
        if (image is null) return "Failed to generate an image.";

        return await this.storageService.CopyFrom(image.Url, user.Container);
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }
  }
}

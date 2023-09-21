using ElectricRaspberry.Services;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Audio;
using System.Security.Claims;

public class FileController
{
  public async Task<string> Whisper(ClaimsPrincipal principal, IOptions<OpenAIOptions> options, IFormFile file)
  {
    var api = new OpenAIClient(new OpenAIAuthentication(options.Value.ApiKey, options.Value.OrgId));
    var request = new AudioTranscriptionRequest(file.OpenReadStream(), file.FileName, responseFormat: AudioResponseFormat.Text, language: "en");
    var result = await api.AudioEndpoint.CreateTranscriptionAsync(request);
    return result;
  }

  public async Task<List<FileDocument>> Upload(ClaimsPrincipal principal, IStorageService storage, IFormFileCollection files)
  {
    var user = principal.GetUser();

    var collection = new List<FileDocument>();
    foreach (var file in files)
    {
        var url = await storage.Upload(file, user.Container);
        var doc = new FileDocument()
        {
            Name = file.FileName,
            Size = file.Length,
            Type = file.ContentType,
            Url = url,
        };
        collection.Add(doc);
    }
    return collection;
  }
}
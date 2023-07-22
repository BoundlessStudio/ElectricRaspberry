using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Audio;

public class FileController
{
  public async Task<Results<Ok<string>, NoContent, NotFound>> Whisper(ClaimsPrincipal principal, IOptions<OpenAIOptions> options, IFormFile file)
  {
    var api = new OpenAIClient(new OpenAIAuthentication(options.Value.ApiKey, options.Value.OrgId));
    var request = new AudioTranscriptionRequest(file.OpenReadStream(), file.FileName, responseFormat: AudioResponseFormat.Text, language: "en");
    var result = await api.AudioEndpoint.CreateTranscriptionAsync(request);
    return TypedResults.Ok(result);
  }
  
  public async Task<Ok<List<FileDocument>>> Upload(ClaimsPrincipal principal, IFeedStorageService storage, IFormFileCollection files, [FromRoute]string feed_id)
  {
    var collection = new List<FileDocument>();
    foreach (var file in files)
    {
      var url = await storage.Upload(feed_id, file);
      var doc = new FileDocument()
      {
        Name = file.FileName,
        Size = file.Length,
        Type = file.ContentType,
        Url = url,
      };
      collection.Add(doc);
    }
    return TypedResults.Ok(collection);
  }
}
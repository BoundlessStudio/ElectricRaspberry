using ElectricRaspberry.Views;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Files;

namespace ElectricRaspberry.Controllers;

public class FileController 
{
  private readonly FileClient client;

  public FileController(FileClient client)
  {
    this.client = client;
  }

  /// <summary> 
  /// Purpose = assistants, vision
  /// </summary>

  public async Task<Results<Ok<List<FileInfoDto>>, BadRequest, ProblemHttpResult>> Upload(IFormFileCollection files)
  {
    try
    {
      var p = FileUploadPurpose.Assistants;
      var collection = new List<FileInfoDto>();

      foreach (var file in files)
      {
        var s = file.OpenReadStream();
        var f = file.FileName;
        OpenAIFileInfo info = await client.UploadFileAsync(s, f, p);
        var dto = new FileInfoDto(info);
        collection.Add(dto);
      }

      return TypedResults.Ok(collection);
    }
    catch (Exception ex)
    {
      var problem = new ProblemDetails();
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<FileContentHttpResult, ProblemHttpResult>> Download(string id)
  {
    try
    {
      OpenAIFileInfo info = await client.GetFileAsync(id);
      MimeTypes.TryGetMimeType(info.Filename, out var mimeType);
      BinaryData file = await client.DownloadFileAsync(info);
      return TypedResults.File(file.ToArray(), contentType: mimeType, fileDownloadName: info.Filename);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<FileInfoDto>, ProblemHttpResult>> Get(string id)  
  {
    try
    {
      OpenAIFileInfo info = await client.GetFileAsync(id);
      var dto = new FileInfoDto(info);
      return TypedResults.Ok(dto);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok, ProblemHttpResult>> Delete(string id)
  {
    try
    {
      bool result = await client.DeleteFileAsync(id);
      if(!result)
        return TypedResults.Problem("Failed to delete the file", statusCode: 500);
      else
        return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }
}
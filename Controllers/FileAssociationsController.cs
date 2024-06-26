using ElectricRaspberry.Views;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using OpenAI;
using OpenAI.Files;
using OpenAI.VectorStores;

namespace ElectricRaspberry.Controllers;

public class FileAssociationsController 
{
  private readonly VectorStoreClient vClient;
  private readonly FileClient fClient;

  public FileAssociationsController(VectorStoreClient vClient, FileClient fClient)
  {
    this.vClient = vClient;
    this.fClient = fClient;
  }


  public async Task<Results<Ok<List<FileAssociationDto>>, ProblemHttpResult>> Create(string id, [FromBody]VectorStoreFileAssociationDto dto) 
  {
    try
    {
      VectorStore store = await vClient.GetVectorStoreAsync(id);

      var collection = new List<FileAssociationDto>();
      foreach (var file in dto.Files)
      {
        OpenAIFileInfo info = await fClient.GetFileAsync(file);
        VectorStoreFileAssociation association = await vClient.AddFileToVectorStoreAsync(store, info);
        collection.Add(new FileAssociationDto(association));
      }

      return TypedResults.Ok(collection);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  
  public async Task<Results<Ok<FileAssociationDto>, ProblemHttpResult>> Get(string storeId, string fileId)  
  {
    try
    {
      VectorStore store = await vClient.GetVectorStoreAsync(storeId);
      OpenAIFileInfo info = await fClient.GetFileAsync(fileId);
      VectorStoreFileAssociation association = await vClient.GetFileAssociationAsync(store, info);
      var dto = new FileAssociationDto(association);
      return TypedResults.Ok(dto);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<List<FileAssociationDto>>, ProblemHttpResult>> List(string id)  
  {
    try
    {
      VectorStore store = await vClient.GetVectorStoreAsync(id);
      var enumerable = vClient.GetFileAssociationsAsync(store, ListOrder.NewestFirst);
      var collection = new List<FileAssociationDto>();
      await foreach (var item in enumerable)
      {
        collection.Add(new FileAssociationDto(item));
      }

      return TypedResults.Ok(collection);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }


  public async Task<Results<Ok, ProblemHttpResult>> Delete(string id, [FromBody]VectorStoreFileAssociationDto dto)
  {
    try
    {
      VectorStore store = await vClient.GetVectorStoreAsync(id);
      foreach (var file in dto.Files)
      {
        OpenAIFileInfo info = await fClient.GetFileAsync(file);
        bool result = await vClient.RemoveFileFromStoreAsync(store, info);
      }

      return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

}
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Files;
using OpenAI.VectorStores;

namespace ElectricRaspberry.Controllers;

public class VectorStoreController 
{
  private readonly VectorStoreClient vClient;
  private readonly FileClient fClient;

  public VectorStoreController(VectorStoreClient vClient, FileClient fClient)
  {
    this.vClient = vClient;
    this.fClient = fClient;
  }

  public async Task<Results<Ok, ProblemHttpResult>> Create([FromBody]CreateVectorStoreDto dto)
  {
    try
    {
      var options = new VectorStoreCreationOptions()
      {
        Name = dto.Name,
        ExpirationPolicy = dto.Expiration == null ? null : 
          new VectorStoreExpirationPolicy(VectorStoreExpirationAnchor.LastActiveAt, dto.Expiration.Value),
      };

      VectorStore store = await vClient.CreateVectorStoreAsync(options);

      return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<VectorStoreDto>, ProblemHttpResult>> Get(string id)  
  {
    try
    {
      VectorStore store = await vClient.GetVectorStoreAsync(id);
      var dto = new VectorStoreDto(store);
      return TypedResults.Ok(dto);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok, ProblemHttpResult>> Modify(string id, [FromBody]ModifyVectorStoreDto dto)  
  {
    try
    {
      var option = new VectorStoreModificationOptions()
      {
        Name = dto.Name,
        ExpirationPolicy = dto.Expiration == null ? null : 
          new VectorStoreExpirationPolicy(VectorStoreExpirationAnchor.LastActiveAt, dto.Expiration.Value),
      };

      // client.AddFileToVectorStoreAsync();

      await vClient.ModifyVectorStoreAsync(id, option);

      return TypedResults.Ok();
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
      VectorStore store = await vClient.GetVectorStoreAsync(id);
      if(store is null)
        return TypedResults.Problem("Failed to find the vector store", statusCode: 500);

      bool result = await vClient.DeleteVectorStoreAsync(store);
      if(!result)
        return TypedResults.Problem("Failed to delete the vector store", statusCode: 500);

      return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }
}
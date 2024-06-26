

using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ElectricRaspberry.Controllers;

public class TableDatabaseController 
{
  private readonly CosmosTableService service;

  public TableDatabaseController(CosmosTableService service)
  {
    this.service = service;
  }

  public async Task<Results<Ok, ProblemHttpResult>> Create(string id, [FromBody]CreateDatabaseContainerDto dto)
  {
    try
    {
      await this.service.CreateContainerAsync(id, dto.Container);
      return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<List<string>>, ProblemHttpResult>> List(string id)
  {
    try
    {
      var collection = await this.service.ListContainers(id);
      return TypedResults.Ok(collection);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok, ProblemHttpResult>> Upsert(string databaseId, string collectionId, [FromBody]UpsertRecordDto dto)
  {
    try
    {
      await this.service.UpsertItemAsync(databaseId, collectionId, dto);
      return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<IEnumerable<Dictionary<string,object>>>, BadRequest, ProblemHttpResult>> Query(string databaseId, string collectionId, [FromQuery]string q)
  {
    if(string.IsNullOrWhiteSpace(q))
      return TypedResults.BadRequest();

    try
    {
      var results = await this.service.SubmitQuery(databaseId, collectionId, q);
      var json = JsonSerializer.Serialize(results);
      var data = new List<Dictionary<string,object>>().AsEnumerable();
      return TypedResults.Ok(results);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<Dictionary<string,object>>, NotFound, ProblemHttpResult>> Get(string databaseId, string collectionId, string id)
  {
    try
    {
      var results = await this.service.GetItemAsync(databaseId, collectionId, id);
      if(results is null)
        return TypedResults.NotFound();
      else 
        return TypedResults.Ok(results);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok, ProblemHttpResult>> Remove(string databaseId, string collectionId, string id)
  {
    try
    {
      await this.service.DeleteItemAsync(databaseId, collectionId, id);
      return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok, ProblemHttpResult>> Delete(string databaseId, string collectionId)
  {
    try
    {
      await this.service.DeleteContainerAsync(databaseId, collectionId);
      return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

}


using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ElectricRaspberry.Controllers;

public class GraphDatabaseController 
{
  private readonly CosmosGraphService service;

  public GraphDatabaseController(CosmosGraphService service)
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

  public async Task<Results<Ok, ProblemHttpResult>> Statements(string databaseId, string collectionId, [FromBody]GraphStatementsDto dto)
  {
    try
    {
      await this.service.SubmitStatements(databaseId, collectionId, dto.Statements);
      return TypedResults.Ok();
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<IEnumerable<Dictionary<string,object>>>, ProblemHttpResult>> Vertices(string databaseId, string collectionId)
  {
    try
    {
      var results = await this.service.SubmitQuery(databaseId, collectionId, "g.V().valueMap(true)");
      return TypedResults.Ok(results);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<IEnumerable<Dictionary<string,object>>>, ProblemHttpResult>> Edges(string databaseId, string collectionId)
  {
    try
    {
      var results = await this.service.SubmitQuery(databaseId, collectionId, "g.E().valueMap(true)");
      return TypedResults.Ok(results);
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


using ElectricRaspberry.Views;
using Json.Schema;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Assistants;
using Json.Schema.Generation;
using ElectricRaspberry.Extensions;
using OpenAI.Files;
using OpenAI.VectorStores;
using System.Text.Json;

namespace ElectricRaspberry.Controllers;

public class AssistantController 
{
  private readonly AssistantClient assistantClient;
  private readonly FileClient fileClient;
  private readonly VectorStoreClient vectorStoreClient;
  private readonly CosmosGraphService service;
  private readonly Dictionary<string, ToolDefinition> tools;

  public AssistantController(AssistantClient assistantClient, FileClient fileClient, VectorStoreClient vectorStoreClient, CosmosGraphService service)
  {
    this.assistantClient = assistantClient;
    this.fileClient = fileClient;
    this.vectorStoreClient = vectorStoreClient;
    this.service = service;
    
    var code_interpreter = ToolDefinition.CreateCodeInterpreter();
    var file_search = ToolDefinition.CreateFileSearch();
    var get_weather = ToolDefinition.CreateFunction("get_weather", "Gets the hourly forecast from Open-Meteo for a latitude & longitude.", new JsonSchemaBuilder().FromType<Location>().Build().ToBinaryData());
    var web_search = ToolDefinition.CreateFunction("web_search", "Search the web for a query via google.", BinaryData.FromString("{}"));
    var gremlin_query = ToolDefinition.CreateFunction("gremlin_query", "Gremlin query to interact with persistent graphs in Cosmos DB.", BinaryData.FromString("{}"));
    var get_user_location = ToolDefinition.CreateFunction("get_user_location", "Gets the current user's location; returns a latitude and longitude", BinaryData.FromString("{}"));

    this.tools = new Dictionary<string, ToolDefinition>()
    {
      {"code_interpreter", code_interpreter},
      {"file_search", file_search},
      {"web_search", web_search},
      {"gremlin_query", gremlin_query},
      {"get_weather", get_weather},
      {"get_user_location", get_user_location},
    };
  }

  // [FromServices]CosmosService cosmosService
  public async Task<Results<Created, ProblemHttpResult>> Create([FromBody]CreateAssistantDto dto)
  {
    try
    {
      var options = new AssistantCreationOptions() 
      {
        Name = dto.Name,
        Description = dto.Description,
        Instructions = string.Empty,
        Temperature = 0.3f,
        ResponseFormat = AssistantResponseFormat.Text,
      };

      options.Metadata.Add("version", "1");
      options.Metadata.Add("graphs", JsonSerializer.Serialize(new List<string>()));
      options.Metadata.Add("tables", JsonSerializer.Serialize(new List<string>()));

      foreach (var tool in this.tools)
      {
        options.Tools.Add(tool.Value);
      }
      
      Assistant assistant = await assistantClient.CreateAssistantAsync("gpt-4o", options);

      await this.service.CreateDatabaseAsync(assistant.Id);
      
      return TypedResults.Created($"/api/assistants/{assistant.Id}");
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok<AssistantDto>, ProblemHttpResult>> Get(string id)  
  {
    try
    {
      Assistant assistant = await assistantClient.GetAssistantAsync(id);
      var dto = new AssistantDto(assistant);
      return TypedResults.Ok(dto);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

  public async Task<Results<Ok, Conflict, ProblemHttpResult>> Modify(string id, [FromBody]ModifyAssistantDto dto)  
  {
    try
    {
      var options = new AssistantModificationOptions()
      {
        Name = dto.Name,
        Description = dto.Description,
        Instructions = dto.Instructions,
        Temperature = 0.3f,
        ResponseFormat = AssistantResponseFormat.Text,
        ToolResources = new ToolResources()
        {
          CodeInterpreter = new CodeInterpreterToolResources(),
          FileSearch = new FileSearchToolResources()
        }
      };

      options.Metadata.Add("version", "1");
      options.Metadata.Add("graphs", JsonSerializer.Serialize(dto.Graphs));
      options.Metadata.Add("tables", JsonSerializer.Serialize(dto.Tables));

      foreach (var item in this.tools)
      {
        options.DefaultTools.Add(item.Value);
      }

      foreach (var item in dto.InterpreterFiles)
      {
        options.ToolResources.CodeInterpreter.FileIds.Add(item);
      }

      foreach (var item in dto.VectorStores)
      {
        options.ToolResources.FileSearch.VectorStoreIds.Add(item);
      }

      Assistant assistant = await assistantClient.ModifyAssistantAsync(id, options);

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
      Assistant assistant = await assistantClient.GetAssistantAsync(id);

      var files = assistant.ToolResources.CodeInterpreter.FileIds;
      foreach (var fileId in files)
      {
        await fileClient.DeleteFileAsync(fileId);
      }

      var stores =  assistant.ToolResources.FileSearch.VectorStoreIds;
      foreach (var storeId in stores)
      {
        await vectorStoreClient.DeleteVectorStoreAsync(storeId);
      }

      await this.service.DeleteDatabaseAsync(id);

      bool result = await assistantClient.DeleteAssistantAsync(id);
      if(result)
        return TypedResults.Ok();
      else
        return TypedResults.Problem("Failed to delete the assistant", statusCode: 500);
    }
    catch (Exception ex)
    {
      return TypedResults.Problem(ex.Message, statusCode: 500);
    }
  }

}
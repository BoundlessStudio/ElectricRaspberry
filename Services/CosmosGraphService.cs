using ElectricRaspberry.Models;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

//using RequestMessage = Gremlin.Net.Driver.Messages.RequestMessage;

public class CosmosGraphService
{
  private GraphSettingsModel settings;
  private CosmosClient cosmosClient;

  public CosmosGraphService(IOptions<GraphSettingsModel> options)
  {
    this.settings = options.Value;

    var cs = $"AccountEndpoint=https://{settings.Host}.documents.azure.com:443/;AccountKey={settings.Key}; ApiKind=Gremlin;";
    this.cosmosClient = new CosmosClient(cs);
  }

  private IGremlinClient GetGremlinClient(string database, string container)
  {
    var server = new GremlinServer(
      $"{settings.Host}.gremlin.cosmos.azure.com",
      port: 443, 
      enableSsl: true, 
      username: $"/dbs/{database}/colls/{container}", 
      password: settings.Key
    );

    return new GremlinClient(
      server, 
      new GraphSON2Reader(), 
      new GraphSON2Writer(), 
      GremlinClient.GraphSON2MimeType
    );
  }

  public async Task CreateDatabaseAsync(string database)
  {
    await cosmosClient.CreateDatabaseAsync(database);
  }

  public async Task CreateContainerAsync(string database, string container)
  {
    Database db = cosmosClient.GetDatabase(database);
    var properties = new ContainerProperties(container, "/key");
    await db.CreateContainerIfNotExistsAsync(properties);
  }

  public async Task DeleteContainerAsync(string database, string container)
  {
     Database db = cosmosClient.GetDatabase(database);
    Container c = db.GetContainer(container);
    await c.DeleteContainerAsync();
  }

  public async Task DeleteDatabaseAsync(string database)
  {
    Database db = cosmosClient.GetDatabase(database);
    await db.DeleteAsync();
  }

  public async Task<List<string>> ListContainers(string database)
  {
    Database db = cosmosClient.GetDatabase(database);
    var iterator = db.GetContainerQueryIterator<ContainerProperties>();

    var collection = new List<string>();
    while (iterator.HasMoreResults)
    {
        foreach (var container in await iterator.ReadNextAsync())
        {
          collection.Add(container.Id);
        }
    }

    return collection;
  }

  public async Task<IEnumerable<Dictionary<string,object>>> SubmitQuery(string database, string container, string query)
  {
    using var client = this.GetGremlinClient(database, container);
    try
    {
      var result = await client.SubmitAsync<Dictionary<string,object>>(query);
      return result.AsEnumerable();
    }
    finally
    {
      client.Dispose();
    }
  }

  public async Task SubmitStatements(string database, string container, List<string> statements)
  {
    using var client = this.GetGremlinClient(database, container);
    try
    {
      foreach (var statement in statements)
      {
        await client.SubmitAsync(statement);
      }
    }
    finally
    {
      client.Dispose();
    }
  }

}
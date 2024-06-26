using System.Text.Json;
using System.Text.Json.Serialization;
using ElectricRaspberry.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

public class CosmosTableService
{
  private CosmosClient cosmosClient;

  public CosmosTableService(IOptions<TableSettingsModel> settings)
  {
    var options = new CosmosClientOptions()
    {
      Serializer = new SystemTextJsonSerializer(new JsonSerializerOptions
      {
        Converters = { new DictionaryJsonConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      })
    };

    this.cosmosClient = new CosmosClient(settings.Value.ConnectionString, options);
  }

  public async Task CreateDatabaseAsync(string database)
  {
    await cosmosClient.CreateDatabaseAsync(database);
  }

  public async Task CreateContainerAsync(string database, string container)
  {
    Database db = cosmosClient.GetDatabase(database);
    var properties = new ContainerProperties(container, "/id");
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
      var result = await iterator.ReadNextAsync();
      collection.AddRange(result.Select(_ => _.Id));
    }

    return collection;
  }

  public async Task UpsertItemAsync(string database, string container, Dictionary<string, object> item)
  {
    Database db = cosmosClient.GetDatabase(database);
    Container c = db.GetContainer(container);
    //await c.CreateItemAsync(item);
    await c.UpsertItemAsync(item);
  }

  public async Task<Dictionary<string, object>?> GetItemAsync(string database, string container, string id)
  {
    Database db = cosmosClient.GetDatabase(database);
    Container c = db.GetContainer(container);
    var response = await c.ReadItemAsync<Dictionary<string, object>>(id, partitionKey: new PartitionKey(id));
    if (response.StatusCode == System.Net.HttpStatusCode.OK)
      return response.Resource;
    else
      return null;
  }

  public async Task DeleteItemAsync(string database, string container, string id)
  {
    Database db = cosmosClient.GetDatabase(database);
    Container c = db.GetContainer(container);
    await c.DeleteItemAsync<Dictionary<string, object>>(id, partitionKey: new PartitionKey(id));
  }

  public async Task<IEnumerable<Dictionary<string, object>>> SubmitQuery(string database, string container, string query)
  {
    Database db = cosmosClient.GetDatabase(database);
    Container c = db.GetContainer(container);
    var iterator = c.GetItemQueryIterator<Dictionary<string, object>>(queryText: query);
    var collection = new List<Dictionary<string, object>>();
    while (iterator.HasMoreResults)
    {
      var result = await iterator.ReadNextAsync();
      collection.AddRange(result);
    }
    return collection;
  }

}

public class SystemTextJsonSerializer : CosmosSerializer
{
  private readonly JsonSerializerOptions jsonSerializerOptions;

  public SystemTextJsonSerializer(JsonSerializerOptions jsonSerializerOptions)
  {
    this.jsonSerializerOptions = jsonSerializerOptions;
  }

  public override T FromStream<T>(Stream stream)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    if (typeof(Stream).IsAssignableFrom(typeof(T)))
    {
      return (T)(object)stream;
    }

    using (stream)
    {
      #pragma warning disable CS8603 // Possible null reference return.
      return JsonSerializer.Deserialize<T>(stream, this.jsonSerializerOptions);
      #pragma warning restore CS8603 // Possible null reference return.
    }
  }

  public override Stream ToStream<T>(T input)
  {
    if (input == null)
    {
      throw new ArgumentNullException(nameof(input));
    }

    MemoryStream stream = new MemoryStream();
    JsonSerializer.SerializeAsync(stream, input, this.jsonSerializerOptions).GetAwaiter().GetResult();
    stream.Position = 0;
    return stream;
  }
}

public class DictionaryJsonConverter : JsonConverter<Dictionary<string, object>>
{
  public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  { 
    if (reader.TokenType != JsonTokenType.StartObject)
    {
      throw new JsonException();
    }

    var dictionary = new Dictionary<string, object>();

    while (reader.Read())
    {
      if (reader.TokenType == JsonTokenType.EndObject)
      {
        return dictionary;
      }

      if (reader.TokenType != JsonTokenType.PropertyName)
      {
        throw new JsonException();
      }

      string propertyName = reader.GetString() ?? string.Empty;
      
      reader.Read();

      object? value = JsonSerializer.Deserialize<object>(ref reader, options);

      #pragma warning disable CS8604 // Possible null reference argument.
      dictionary.Add(propertyName, value);
      #pragma warning restore CS8604 // Possible null reference argument.
    }

    throw new JsonException();
  }

  public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();

    foreach (var kvp in value)
    {
      writer.WritePropertyName(kvp.Key);
      JsonSerializer.Serialize(writer, kvp.Value, options);
    }

    writer.WriteEndObject();
  }
}
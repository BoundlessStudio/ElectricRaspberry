namespace ElectricRaspberry.Configuration;

public class CosmosDbOptions
{
    public const string CosmosDB = "CosmosDB";
    
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
}
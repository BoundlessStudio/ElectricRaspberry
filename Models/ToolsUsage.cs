
using System.Text.Json.Serialization;

public class ToolsUsage
{
  [JsonPropertyName("code_interpreter")]
  public CodeInterpreterTool CodeInterpreter { get; set; }

  [JsonPropertyName("file_search")]
  public FileSearchTool FileSearch { get; set; }

  [JsonPropertyName("web_search")]
  public WebSearchTool WebSearch { get; set; }

  [JsonPropertyName("graph")]
  public GraphTool Graph { get; set; }

  public ToolsUsage()
  {
    this.CodeInterpreter = new CodeInterpreterTool();
    this.FileSearch = new FileSearchTool();
    this.WebSearch = new WebSearchTool();
    this.Graph = new GraphTool(); 
  }
}

public class CodeInterpreterTool
  {
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("files")]
    public List<string> Files { get; set; } = new List<string>();
  }

  public class FileSearchTool
  {
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("stores")]
    public List<string> Stores { get; set; } = new List<string>();
  }

  public class WebSearchTool
  {
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
  }

  public class GraphTool
  {
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("collections")]
    public List<string> Collections { get; set; } = new List<string>();
  }
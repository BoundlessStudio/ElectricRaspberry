using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace ElectricRaspberry.Plugins;

public sealed class SqlDatabasePlugin
{
  const string PROMPT = @"
Generate the TSQL to complete the following goal:
<goal>
{{$goal}}
</goal>
Use table following table schemas to ground the SQL statements:
<schemas>
{{$schemas}}
</schemas>
<instructions>
The Database is MS SQL server.
Do not sql reserved keywords as column alias.
Do not display 'table_schema' name when displaying tables to the user.
YOU MUST ONLY RETURN THE SQL CODE. 
</instructions>
";

  const string SQL_FOR_SCHEMA = @"
SELECT 
    t.table_name, 
    c.column_name, 
    c.data_type
FROM 
    information_schema.tables t
JOIN 
    information_schema.columns c 
    ON t.table_schema = c.table_schema 
    AND t.table_name = c.table_name
WHERE 
    t.table_type = 'BASE TABLE'
ORDER BY 
    t.table_schema, 
    t.table_name, 
    c.ordinal_position;
";

  const string CONNECTION_STRING = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ElectricRaspberry;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

  const string CODE_BLOCK_PATTERN = @"```[a-zA-Z]*\n(.*?)```";

  [KernelFunction, Description("Generate TSQL for interactions with the database using natural lanague.")]
  public static async Task<string?> GenerateSQL([Description("The instructions in natural lanaguge.")] string instructions)
  {
    var apikey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new ArgumentNullException("OPENAI_API_KEY");
    var orgId = Environment.GetEnvironmentVariable("OPENAI_ORGANIZATION");

    var builder = Kernel.CreateBuilder();
    builder.AddOpenAIChatCompletion("gpt-4o", apikey, orgId);
    var kernel = builder.Build();

    var executionSettings = new OpenAIPromptExecutionSettings
    {
      MaxTokens = 4096,
      Temperature = 0.7,
    };

    var chatFunction = kernel.CreateFunctionFromPrompt(PROMPT, executionSettings);

    var schema = await ExecuteSQL(SQL_FOR_SCHEMA);

    var arguments = new KernelArguments()
    {
      ["schemas"] = schema,
      ["goal"] = instructions
    };
    var result = await chatFunction.InvokeAsync(kernel, arguments);

    var markdown = result.GetValue<string>() ?? string.Empty;
    
    var match = Regex.Match(markdown, CODE_BLOCK_PATTERN, RegexOptions.Singleline);
    var sql = match.Success ? match.Groups[1].Value.Trim() : markdown;
    var results = await ExecuteSQL(sql);

    return results;
  }

  private static async Task<string?> ExecuteSQL(string sql)
  {
    
    using SqlConnection connection = new SqlConnection(CONNECTION_STRING);
    connection.Open();

    using SqlCommand command = new SqlCommand(sql, connection);
    using SqlDataReader reader = await command.ExecuteReaderAsync();

    var result = new StringBuilder();
    while (await reader.ReadAsync())
    {
      var row = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i).ToString());
      result.AppendLine(string.Join(",", row));
    }

    return result.ToString();
  }
}
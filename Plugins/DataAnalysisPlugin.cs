using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ElectricRaspberry.Plugins;

public sealed class DataAnalysisPlugin
{
  const string PROMPT = @"
Generate a Python script to complete following goal:
<goal>
{{$goal}}
</goal>
The following packages/librarys have been installed in the enviroment.
<packages>
requests>=2,<3
pandas>=1.3,<2
numpy>=1.24,<1.25
dateparser>=1.1,<1.2
Pillow<=9.5.0
geopandas>=0.13,<0.14
tabulate>=0.9.0,<1.0
PyPDF2>=3.0,<3.1
pdfminer.six>=20221105,<20221106
pdfplumber>=0.9,<0.10
matplotlib>=3.7,<3.8
qrcode>=6,<7
python-barcode>=0.14,<0.15
zxing>=0.9,<1.0
PuLP>=2,<3
ortools>=8,<9
urllib3>=1.25,<1.26
python-docx>=0.8,<0.9
openpyxl>=3.0,<3.1
python-pptx>=0.6,<0.7
pytesseract>=0.3,<0.4
pyocr>=0.8,<0.9
folium>=0.12,<0.13
geopy>=2,<3
gmplot>=1,<2
azure-storage-blob>=12,<13
faker>=9,<10
seaborn>=0.11,<0.12
pyodbc>=4,<5
sqlalchemy>=1.4,<1.5
MarkupSafe>=2.0.1,<2.0.2
BeautifulSoup4>=4.12.3,<4.12.4
pytesseract>=0.3.10,<0.3.11
</packages>
Those packages delivery the following features:
<features>
Data Interpretation, Statistical Analysis, Data Visualization, Predictive Analysis, Web Browser, Image OCR, Geocoding
</features>
<instructions>
Results must be printed to the StandardOutput.
YOU MUST ONLY RETURN THE PYHTON CODE.
</instructions>
";

  const string CODE_BLOCK_PATTERN = @"```[a-zA-Z]*\n(.*?)```";

  private readonly Kernel kernel;
  private readonly KernelFunction chatFunction;

  public DataAnalysisPlugin()
  {
    var apikey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new ArgumentNullException("OPENAI_API_KEY");
    var orgId = Environment.GetEnvironmentVariable("OPENAI_ORGANIZATION");

    var builder = Kernel.CreateBuilder();
    builder.AddOpenAIChatCompletion("gpt-4o", apikey, orgId);
    this.kernel = builder.Build();

    var settings = new OpenAIPromptExecutionSettings
    {
      MaxTokens = 4096,
      Temperature = 0.7,
    };

    this.chatFunction = this.kernel.CreateFunctionFromPrompt(PROMPT, settings);
  }

  [KernelFunction, Description("Generate and execute python in an interactive environment with access to the web.")]
  public async Task<string?> GeneratePython([Description("The instructions in natural lanaguge.")] string instructions)
  {
    var arguments = new KernelArguments()
    {
      ["goal"] = instructions,
    };
    var result = await chatFunction.InvokeAsync(this.kernel, arguments);

    var markdown = result.GetValue<string>() ?? string.Empty;

    var match = Regex.Match(markdown, CODE_BLOCK_PATTERN, RegexOptions.Singleline);
    var python = match.Success ? match.Groups[1].Value.Trim() : markdown;
    var results = await ExecutePython(python);

    return results;
  }

  public static async Task<string> ExecutePython(string script)
  {
    // Create a temporary directory
    string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDirectory);

    try
    {
      // Path to the script file
      string scriptFilePath = Path.Combine(tempDirectory, "main.py");
      File.WriteAllText(scriptFilePath, script);

      // Create a new process start info for the script
      ProcessStartInfo scriptProcess = new ProcessStartInfo();
      scriptProcess.WorkingDirectory = tempDirectory;
      scriptProcess.FileName = "python.exe";
      scriptProcess.Arguments = scriptFilePath;
      scriptProcess.UseShellExecute = false;
      scriptProcess.RedirectStandardOutput = true;
      scriptProcess.RedirectStandardError = true;
      scriptProcess.CreateNoWindow = true;

      var files = TrackFiles(tempDirectory);

      using var process = Process.Start(scriptProcess);
      if (process == null)
        throw new InvalidOperationException();

      await process.WaitForExitAsync();

      string output = process.StandardOutput.ReadToEnd();
      string error = process.StandardError.ReadToEnd();
      var std = string.IsNullOrWhiteSpace(output) ? "No Output" : output;
      var ste = string.IsNullOrWhiteSpace(error) ? "No Error" : error;
      var wdf = files.Count == 0 ? "No Files" : string.Join("\n", files);

      return $"<StandardOutput>{std}</StandardOutput>\n<StandardError>{ste}</StandardError>"; // \n<WorkingDirectory>{wdf}</WorkingDirectory>
    }
    finally
    {
      // Clean up the temporary directory
      //Directory.Delete(tempDirectory, true);
    }
  }

  private static HashSet<string> TrackFiles(string tempDirectory)
  {
    var files = new HashSet<string>();
    var watcher = new FileSystemWatcher(tempDirectory, "*.*");
    watcher.Created += (sender, e) =>
    {
      files.Add(e.FullPath);
    };
    watcher.Deleted += (sender, e) =>
    {
      files.Remove(e.FullPath);
    };
    watcher.Renamed += (sender, e) =>
    {
      if (e.OldName is not null)
        files.Remove(e.OldName);
      if (e.Name is not null)
        files.Add(e.Name);
    };
    watcher.EnableRaisingEvents = true;
    return files;
  }

  private static void OnChanged(object source, FileSystemEventArgs e)
  {
    Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
  }

  private static void OnRenamed(object source, RenamedEventArgs e)
  {
    Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
  }
}
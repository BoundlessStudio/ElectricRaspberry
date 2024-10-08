using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElectricRaspberry;

public class Application : BackgroundService
{
  private readonly SessionsPythonPlugin plugin;
  private readonly ILogger<Application> logger;

  public Application(IHttpClientFactory httpFactory, ILoggerFactory loggerFactory)
  {
    var endpoint = Environment.GetEnvironmentVariable("AZURE_DYNAMIC_SESSION_ENDPOINT") ?? throw new ArgumentNullException("AZURE_DYNAMIC_SESSION_ENDPOINT");
    var setting = new SessionsPythonSettings(endpoint) { SanitizeInput = true };
    this.plugin = new SessionsPythonPlugin(setting, httpFactory, loggerFactory);
    this.logger = loggerFactory.CreateLogger<Application>();
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var packages = await this.plugin.GetPackages();
    logger.LogInformation("Packages: {packages}", packages);
  }

  private async Task CreateFile()
  {
    // var meta = await plugin.UploadFileAsync("", BinaryData.FromObjectAsJson(new { }));

    var id = "DA933A26-B9E6-42D2-BC0C-ED5A88D10B69";

    var preFiles = await plugin.ListFilesAsync(id);
    if (preFiles.Count > 0)
      logger.LogInformation("Pre Files: {files}", preFiles);

    string code = @"
import openpyxl
from openpyxl.utils import get_column_letter
import random
import os

def generate_dummy_excel(filename, num_rows=10, num_cols=5):
    # Create a new workbook and select the active sheet
    workbook = openpyxl.Workbook()
    sheet = workbook.active
    sheet.title = ""Dummy Data""

    # Generate header row
    for col in range(1, num_cols + 1):
        cell = sheet.cell(row=1, column=col)
        cell.value = f""Column {get_column_letter(col)}""

    # Generate dummy data
    for row in range(2, num_rows + 2):
        for col in range(1, num_cols + 1):
            cell = sheet.cell(row=row, column=col)
            cell.value = random.randint(1, 100)

    # Save the workbook to the specified directory
    full_path = os.path.join(""/mnt/data"", filename)
    workbook.save(full_path)
    print(f""Dummy Excel file has been generated at: {full_path}"")

# Usage
generate_dummy_excel(""dummy_data.xlsx"", num_rows=20, num_cols=7)
";

    var result = await this.plugin.ExecuteCodeAsync(id, code);
    if (!string.IsNullOrEmpty(result))
      logger.LogInformation("Result: {result}", result);

    var postFiles = await plugin.ListFilesAsync(id);
    if (postFiles.Count == 0)
      return;

    logger.LogInformation("Post Files: {files}", postFiles);

    var file = postFiles.First();
    var data = await plugin.DownloadFileAsync(id, file.Filename);
    File.WriteAllBytes(file.Filename, data.ToArray());
  }

  
}
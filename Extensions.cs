using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.KernelExtensions;
using Microsoft.SemanticKernel.Skills.Document;
using Microsoft.SemanticKernel.Skills.Document.FileSystem;
using Microsoft.SemanticKernel.Skills.Document.OpenXml;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors;
using Microsoft.SemanticKernel.Skills.Web;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using Microsoft.SemanticKernel.TemplateEngine;

internal sealed class TokenAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _token;

    public TokenAuthenticationProvider(string token)
    {
        this._token = token;
    }

    public Task AuthenticateRequestAsync(HttpRequestMessage request)
    {
        return Task.FromResult(request.Headers.Authorization = new AuthenticationHeaderValue(
            scheme: "bearer",
            parameter: this._token));
    }
}

internal static class Extensions
{

    internal static string PrettyJson(this string json)
    {
        var options = new JsonSerializerOptions(){
            WriteIndented = true
        };
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return JsonSerializer.Serialize(element, options);
    }

    internal static void RegisterMicrosoftServiceSkills(this IKernel kernel, GraphServiceClient graphServiceClient)
    {
        kernel.ImportSkill(new CloudDriveSkill(new OneDriveConnector(graphServiceClient)), nameof(CloudDriveSkill));
        kernel.ImportSkill(new TaskListSkill(new MicrosoftToDoConnector(graphServiceClient)), nameof(TaskListSkill));
        kernel.ImportSkill(new EmailSkill(new OutlookMailConnector(graphServiceClient)), nameof(EmailSkill));
        kernel.ImportSkill(new CalendarSkill(new OutlookCalendarConnector(graphServiceClient)), nameof(CalendarSkill));
        // TODO: ADD MORE..!
    }

    internal static void RegisterGoogleServices(this IKernel kernel)
    {
        // TODO: ADD MORE..!
    }

    internal static void RegisterTextMemory(this IKernel kernel)
    {
        kernel.ImportSkill(new TextMemorySkill(), nameof(TextMemorySkill));
    }

    internal static void RegisterSystemSkills(this IKernel kernel)
    {
        kernel.ImportSkill(new MathSkill(), nameof(MathSkill));
        kernel.ImportSkill(new TextSkill(), nameof(TextSkill));
        kernel.ImportSkill(new TimeSkill(), nameof(TimeSkill));
        kernel.ImportSkill(new WaitSkill(), nameof(WaitSkill));
        kernel.ImportSkill(new ConvertSkill(), nameof(ConvertSkill));
    }

    internal static void RegisterFilesSkills(this IKernel kernel)
    {
        kernel.ImportSkill(new FileIOSkill(), nameof(FileIOSkill));
        // Create DirectorySkill
    }

    internal static void RegisterOfficeSkills(this IKernel kernel)
    {
        kernel.ImportSkill(new DocumentSkill(new WordDocumentConnector(), new LocalFileSystemConnector()), nameof(DocumentSkill));
    }

    internal static void RegisterWebSkills(this IKernel kernel, string? ApiKey = null)
    {
        kernel.ImportSkill(new HttpSkill(), nameof(HttpSkill));
        kernel.ImportSkill(new SearchUrlSkill(), nameof(SearchUrlSkill));
        kernel.ImportSkill(new WebFileDownloadSkill(), nameof(WebFileDownloadSkill));
        
        if(ApiKey is not null)
            kernel.ImportSkill(new WebSearchEngineSkill(new BingConnector(ApiKey)), nameof(WebSearchEngineSkill));
    }

    internal static void RegisterSemanticSkills(
        this IKernel kernel,
        string skillsFolder,
        IEnumerable<string>? skillsToLoad = null)
    {
        
        var paths = System.IO.Directory.EnumerateFiles(skillsFolder, "*.txt", SearchOption.AllDirectories);
        foreach (string skPromptPath in paths)
        {
            var fi = new FileInfo(skPromptPath);
            var skillName = fi.Directory?.Name ?? string.Empty;
            var skillsDirectory = fi.Directory?.Parent?.Name ?? string.Empty;

            if (ShouldLoad(skillName, skillsToLoad))
            {
                try
                {
                    _ = kernel.ImportSemanticSkillFromDirectory(skillsFolder, skillsDirectory);
                }
                catch (TemplateException e)
                {
                    kernel.Log.LogWarning("Could not load skill from {0} with error: {1}", skillsDirectory, e.Message);
                }
            }
        }
    }

    private static bool ShouldLoad(string skillName, IEnumerable<string>? skillsToLoad = null)
    {
        return skillsToLoad?.Contains(skillName, StringComparer.OrdinalIgnoreCase) != false;
    }
}
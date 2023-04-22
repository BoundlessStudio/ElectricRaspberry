using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.KernelExtensions;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.Document;
using Microsoft.SemanticKernel.Skills.Document.FileSystem;
using Microsoft.SemanticKernel.Skills.Document.OpenXml;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors;
using Microsoft.SemanticKernel.Skills.Web;
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
    internal static void RegisterNativeGraphSkills(this IKernel kernel, GraphServiceClient graphServiceClient, IEnumerable<string>? skillsToLoad = null)
    {
        if (ShouldLoad(nameof(CloudDriveSkill), skillsToLoad))
        {
            CloudDriveSkill cloudDriveSkill = new(new OneDriveConnector(graphServiceClient));
            _ = kernel.ImportSkill(cloudDriveSkill, nameof(cloudDriveSkill));
        }

        if (ShouldLoad(nameof(TaskListSkill), skillsToLoad))
        {
            TaskListSkill taskListSkill = new(new MicrosoftToDoConnector(graphServiceClient));
            _ = kernel.ImportSkill(taskListSkill, nameof(taskListSkill));
        }

        if (ShouldLoad(nameof(EmailSkill), skillsToLoad))
        {
            EmailSkill emailSkill = new(new OutlookMailConnector(graphServiceClient));
            _ = kernel.ImportSkill(emailSkill, nameof(emailSkill));
        }

        if (ShouldLoad(nameof(CalendarSkill), skillsToLoad))
        {
            CalendarSkill calendarSkill = new(new OutlookCalendarConnector(graphServiceClient));
            _ = kernel.ImportSkill(calendarSkill, nameof(calendarSkill));
        }
    }

    internal static void RegisterTextMemory(this IKernel kernel)
    {
        _ = kernel.ImportSkill(new TextMemorySkill(), nameof(TextMemorySkill));
    }

    internal static void RegisterNativeSkills(this IKernel kernel, IEnumerable<string>? skillsToLoad = null)
    {
        
        if (ShouldLoad(nameof(FileIOSkill), skillsToLoad))
        {
            var skill = new FileIOSkill();
            _ = kernel.ImportSkill(skill, nameof(FileIOSkill));
        }

        if (ShouldLoad(nameof(HttpSkill), skillsToLoad))
        {
            var skill = new HttpSkill();
            _ = kernel.ImportSkill(skill, nameof(HttpSkill));
        }

        if (ShouldLoad(nameof(MathSkill), skillsToLoad))
        {
            var skill = new MathSkill();
            _ = kernel.ImportSkill(skill, nameof(MathSkill));
        }

        if (ShouldLoad(nameof(PlannerSkill), skillsToLoad))
        {
            var skill = new PlannerSkill(kernel);
            _ = kernel.ImportSkill(skill, nameof(PlannerSkill));
        }

        if (ShouldLoad(nameof(TextMemorySkill), skillsToLoad))
        {
            var skill = new TextMemorySkill();
            _ = kernel.ImportSkill(skill, nameof(TextMemorySkill));
        }

        if (ShouldLoad(nameof(TextSkill), skillsToLoad))
        {
            var skill = new TextSkill();
            _ = kernel.ImportSkill(skill, nameof(TextSkill));
        }

        if (ShouldLoad(nameof(TimeSkill), skillsToLoad))
        {
            var skill = new TimeSkill();
            _ = kernel.ImportSkill(skill, nameof(TimeSkill));
        }

        if (ShouldLoad(nameof(WaitSkill), skillsToLoad))
        {
            var skill = new WaitSkill();
            _ = kernel.ImportSkill(skill, nameof(WaitSkill));
        }

        if (ShouldLoad(nameof(DocumentSkill), skillsToLoad))
        {
            DocumentSkill skill = new(new WordDocumentConnector(), new LocalFileSystemConnector());
            _ = kernel.ImportSkill(skill, nameof(DocumentSkill));
        }

        if (ShouldLoad(nameof(ConversationSummarySkill), skillsToLoad))
        {
            ConversationSummarySkill skill = new(kernel);
            _ = kernel.ImportSkill(skill, nameof(ConversationSummarySkill));
        }

        if (ShouldLoad(nameof(WebFileDownloadSkill), skillsToLoad))
        {
            var skill = new WebFileDownloadSkill();
            _ = kernel.ImportSkill(skill, nameof(WebFileDownloadSkill));
        }
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
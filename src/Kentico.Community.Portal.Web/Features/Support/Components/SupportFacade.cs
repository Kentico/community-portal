﻿using System.Net.Mime;
using System.Text;
using CMS.Core;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Features.Support;

public class SupportFacade
{
    private readonly IWebHostEnvironment environment;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IEventLogService eventLogService;
    private readonly MicrosoftDynamicsSettings dynamicsSettings;

    public SupportFacade(
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        IEventLogService eventLogService,
        IOptions<MicrosoftDynamicsSettings> options)
    {
        this.environment = environment;
        this.httpClientFactory = httpClientFactory;
        this.eventLogService = eventLogService;
        dynamicsSettings = options.Value;
    }

    public async Task ProcessRequest(SupportFormViewModel model)
    {
        try
        {
            var supportCase = await GetSupportCaseAsync(model);
            string directory = Path.Combine(environment.WebRootPath, dynamicsSettings.SupportCasesDirectory);
            string processedDirectory = Path.Combine(directory, dynamicsSettings.ProcessedCasesDirectory);

            string fileName = $"{Guid.NewGuid()}.json";
            string filePath = Path.Combine(directory, fileName);
            string payload = JsonConvert.SerializeObject(supportCase);

            EnsureDirectory(directory);
            EnsureDirectory(processedDirectory);

            await File.WriteAllTextAsync(filePath, payload);
            // await SendSupportCase(payload);

            File.Move(filePath, Path.Combine(processedDirectory, fileName));

            eventLogService.LogInformation(
                nameof(SupportFacade),
                nameof(ProcessRequest),
                fileName);
        }
        catch (Exception exception)
        {
            eventLogService.LogException(
                nameof(SupportFacade),
                nameof(ProcessRequest),
                exception);
        }
    }

    private async Task SendSupportCase(string json)
    {
        var httpClient = httpClientFactory.CreateClient();

        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        using var httpResponseMessage = await httpClient.PostAsync(dynamicsSettings.Url, content);

        httpResponseMessage.EnsureSuccessStatusCode();
    }

    private static void EnsureDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }
    }

    private async Task<SupportCaseDto> GetSupportCaseAsync(SupportFormViewModel model)
    {
        var supportCase = new SupportCaseDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Company = model.Company,
            Subject = model.Issue,
            Description = model.Description,
            Scenario = model.Cause,
            Version = model.Version,
            DevelopmentModel = model.DeploymentModel,
            URL = model.WebsiteURL,
            Fix = model.AttemptedResolution,
            FileName = model.Attachment?.FileName ?? "unknown"
        };

        if (model.Attachment is not null)
        {
            supportCase.FileName = model.Attachment.FileName;

            byte[] fileBytes = await model.Attachment.GetBytes();
            supportCase.File = Convert.ToBase64String(fileBytes);
        }

        return supportCase;
    }
}

public class SupportCaseDto
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [JsonRequired]
    public string Email { get; set; } = "";

    public string Company { get; set; } = "";

    [JsonRequired]
    public string Subject { get; set; } = "";

    [JsonRequired]
    public string Description { get; set; } = "";

    public string Version { get; set; } = "";

    public string Hotfix { get; set; } = "";

    public string DevelopmentModel { get; set; } = "";

    public string URL { get; set; } = "";

    public string Scenario { get; set; } = "";

    public string Fix { get; set; } = "";

    /// <summary>
    /// Base64 encoded file
    /// </summary>
    public string File { get; set; } = "";

    public string FileName { get; set; } = "";
}

public static class FormFileExtensions
{
    public static async Task<byte[]> GetBytes(this IFormFile formFile)
    {
        await using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}

public class MicrosoftDynamicsSettings
{
    public string Url { get; set; } = "";
    public string SupportCasesDirectory { get; set; } = "";
    public string ProcessedCasesDirectory { get; set; } = "";
}

using System.Text;
using System.Text.Json;
using CMS.Base;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Support;

[Route("[controller]/[action]")]
public class SupportFormController(
    CaptchaValidator captchaValidator,
    AzureStorageClientFactory clientFactory,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    private readonly CaptchaValidator captchaValidator = captchaValidator;
    private readonly AzureStorageClientFactory clientFactory = clientFactory;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSupportForm([FromForm] SupportFormViewModel requestModel, CancellationToken cancellationToken)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return ViewComponent(typeof(SupportFormViewComponent), requestModel);
        }

        var captchaResponse = await captchaValidator.ValidateCaptcha(requestModel);

        if (!captchaResponse.IsSuccess)
        {
            ModelState.AddModelError(nameof(SupportFormViewModel.CaptchaToken), "Captcha is invalid.");
        }

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(SupportFormViewComponent), requestModel);
        }

        var containerClient = await clientFactory.GetOrCreateContainerClient(SupportRequestProcessorBackgroundService.CONTAINER_PRIMARY_NAME);
        var queueClient = await clientFactory.GetOrCreateQueue(SupportRequestProcessorBackgroundService.QUEUE_PRIMARY_NAME);
        string blobName = $"{Guid.NewGuid()}.json";
        var requestMessage = await GetSupportCaseAsync(requestModel);
        string requestMessageJSON = JsonSerializer.Serialize(requestMessage);

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        await writer.WriteAsync(requestMessageJSON);
        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;

        _ = await containerClient.UploadBlobAsync(blobName, stream, cancellationToken);
        var queueMessage = new SupportRequestQueueMessage
        {
            BlobName = blobName,
            Subject = requestModel.Issue,
            AuthorName = requestMessage.FirstName,
            AuthorEmail = requestMessage.Email
        };
        _ = await queueClient.SendMessageAsync(JsonSerializer.Serialize(queueMessage), cancellationToken);

        return ViewComponent(typeof(SupportFormViewComponent), new SupportFormViewModel() { IsSuccess = true });
    }

    private static async Task<SupportRequestMessage> GetSupportCaseAsync(SupportFormViewModel model)
    {
        var supportCase = new SupportRequestMessage
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
            FileName = model.Attachment?.FileName ?? "unknown",
            IsKenticoSaaSProductionIssue = model.IsKenticoSaaSProductionIssue
        };

        if (model.Attachment is not null)
        {
            supportCase.FileName = model.Attachment.FileName;

            await using var memoryStream = new MemoryStream();
            await model.Attachment.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();
            supportCase.File = Convert.ToBase64String(fileBytes);
        }

        return supportCase;
    }
}

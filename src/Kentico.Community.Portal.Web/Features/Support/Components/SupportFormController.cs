using System.Text;
using System.Text.Json;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Support;

[Route("[controller]/[action]")]
public class SupportFormController(
    CaptchaValidator captchaValidator,
    AzureStorageClientFactory clientFactory) : Controller
{
    private readonly CaptchaValidator captchaValidator = captchaValidator;
    private readonly AzureStorageClientFactory clientFactory = clientFactory;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSupportForm([FromForm] SupportFormViewModel requestModel, CancellationToken cancellationToken)
    {
        var captchaResponse = await captchaValidator.ValidateCaptcha(requestModel);

        if (!captchaResponse.IsSuccess)
        {
            ModelState.AddModelError(nameof(SupportFormViewModel.CaptchaToken), "Captcha is invalid.");
        }

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(SupportFormViewComponent), requestModel);
        }

        var containerClient = await clientFactory.GetOrCreateContainerClient(SupportMessageProcessorHostedService.CONTAINER_PRIMARY_NAME);
        var queueClient = await clientFactory.GetOrCreateQueue(SupportMessageProcessorHostedService.QUEUE_PRIMARY_NAME);
        string blobName = $"{Guid.NewGuid()}.json";
        var requestMessage = await GetSupportCaseAsync(requestModel);
        string requestMessageJSON = JsonSerializer.Serialize(requestMessage);

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        await writer.WriteAsync(requestMessageJSON);
        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;

        _ = await containerClient.UploadBlobAsync(blobName, stream, cancellationToken);
        _ = await queueClient.SendMessageAsync(JsonSerializer.Serialize(new SupportRequestQueueMessage
        {
            BlobName = blobName,
            Subject = requestModel.Issue
        }), cancellationToken);

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
            FileName = model.Attachment?.FileName ?? "unknown"
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

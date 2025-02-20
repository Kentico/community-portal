using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.Support;

public class SupportRequestProcessorBackgroundService : ApplicationBackgroundService
{
    public const string QUEUE_PRIMARY_NAME = "support-messages";
    public const string QUEUE_DEAD_LETTER_NAME = "support-messages-dead-letter";
    public const string CONTAINER_PRIMARY_NAME = "support-messages";
    public const string CONTAINER_DEAD_LETTER_NAME = "support-messages-dead-letter";

    private readonly ILogger<SupportRequestProcessorBackgroundService> logger;
    private readonly IInfoProvider<SupportRequestConfigurationInfo> configurationProvider;
    private readonly IInfoProvider<SupportRequestProcessingEventInfo> eventProvider;
    private readonly IProgressiveCache cache;
    private readonly SupportRequestProcessingSettings settings;
    private readonly AzureStorageClientFactory clientFactory;

    private QueueClient queueClientPrimary = null!;
    private QueueClient queueClientDeadLetter = null!;
    private BlobContainerClient containerClientPrimary = null!;
    private BlobContainerClient containerClientDeadLetter = null!;
    private readonly HttpClient httpClient;

    public SupportRequestProcessorBackgroundService(
        ILogger<SupportRequestProcessorBackgroundService> logger,
        IHttpClientFactory httpClientFactory,
        AzureStorageClientFactory clientFactory,
        IInfoProvider<SupportRequestConfigurationInfo> configurationProvider,
        IInfoProvider<SupportRequestProcessingEventInfo> eventProvider,
        IProgressiveCache cache,
        IOptions<SupportRequestProcessingSettings> options)
    {
        this.logger = logger;
        this.configurationProvider = configurationProvider;
        this.eventProvider = eventProvider;
        this.cache = cache;
        settings = options.Value;
        this.clientFactory = clientFactory;
        httpClient = httpClientFactory.CreateClient();

        ShouldRestart = true;
        RestartDelay = TimeSpan.FromMinutes(10).Milliseconds;
    }

    protected override async Task ExecuteInternal(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
        {
            var configuration = await GetConfiguration();

            if (settings.IsEnabled && (configuration?.SupportRequestConfigurationIsQueueProcessingEnabled ?? false))
            {
                await InitializeStorageClients();

                await ProcessQueueMessagesAsync(configuration, cancellationToken);
            }
        }
    }

    private async Task ProcessQueueMessagesAsync(SupportRequestConfigurationInfo configuration, CancellationToken cancellationToken)
    {
        var dequeueResponse = await queueClientPrimary.ReceiveMessageAsync(cancellationToken: cancellationToken);
        if (dequeueResponse.Value is not QueueMessage message)
        {
            logger.LogInformation("No messages in Azure queue {queueName}", QUEUE_PRIMARY_NAME);
            return;
        }

        try
        {
            var supportRequest = JsonSerializer.Deserialize<SupportRequestQueueMessage>(message.Body.ToString())
                ?? throw new InvalidOperationException($"Could not deserialize queue message: {message.MessageId}");

            var response = await SendToAzureFunctionAsync(configuration, supportRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new SupportRequestProcessException(supportRequest, $"Failed to send to Azure Function. Status: {response.StatusCode}");
            }

            _ = await queueClientPrimary.DeleteMessageAsync(dequeueResponse.Value.MessageId, dequeueResponse.Value.PopReceipt, cancellationToken);
            _ = await containerClientPrimary.DeleteBlobAsync(supportRequest.BlobName, cancellationToken: cancellationToken);
            await eventProvider.SetAsync(new SupportRequestProcessingEventInfo
            {
                SupportRequestProcessingEventStatus = SupportRequestProcessingEventInfo.Statuses.SUCCESS.ToString(),
                SupportRequestProcessingEventMessage = supportRequest.Subject,
                SupportRequestProcessingEventMessageID = dequeueResponse.Value.MessageId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process support request queue message {exception}.", ex);

            if (dequeueResponse.Value is not QueueMessage queueMessage)
            {
                return;
            }

            var receipt = await queueClientDeadLetter.SendMessageAsync(queueMessage.Body, cancellationToken: cancellationToken);
            _ = await queueClientPrimary.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, cancellationToken);

            if (ex is SupportRequestProcessException processEx)
            {
                await MoveBlobToDeadLetter(processEx.QueueMessage.BlobName);
            }

            await eventProvider.SetAsync(new SupportRequestProcessingEventInfo
            {
                SupportRequestProcessingEventStatus = SupportRequestProcessingEventInfo.Statuses.FAILURE.ToString(),
                SupportRequestProcessingEventMessage = ex.ToString(),
                SupportRequestProcessingEventMessageID = receipt.Value.MessageId
            });
        }
    }

    private async Task<HttpResponseMessage> SendToAzureFunctionAsync(SupportRequestConfigurationInfo configuration, SupportRequestQueueMessage message, CancellationToken cancellationToken)
    {
        var blobClient = containerClientPrimary.GetBlobClient(message.BlobName);
        var stream = await blobClient.OpenReadAsync(position: 0, cancellationToken: cancellationToken);
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var res = await httpClient.PostAsync(configuration.SupportRequestConfigurationExternalEndpointURL, content, cancellationToken);

        return res;
    }

    private async Task MoveBlobToDeadLetter(string blobName)
    {
        var sourceBlob = containerClientPrimary.GetBlobClient(blobName);
        var targetBlob = containerClientDeadLetter.GetBlobClient(blobName);
        _ = await targetBlob.StartCopyFromUriAsync(sourceBlob.Uri);

        var resp = await targetBlob.GetPropertiesAsync();
        while (resp.Value.BlobCopyStatus == CopyStatus.Pending)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            resp = await targetBlob.GetPropertiesAsync();
        }
        _ = await sourceBlob.DeleteIfExistsAsync();
    }

    private async Task<SupportRequestConfigurationInfo?> GetConfiguration() =>
        await cache.LoadAsync(async cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency([$"{SupportRequestConfigurationInfo.OBJECT_TYPE}|all"]);

            return await configurationProvider.Get()
                .TopN(1)
                .FirstOrDefaultAsync();
        }, new CacheSettings(60, [nameof(SupportRequestProcessorBackgroundService), nameof(GetConfiguration)]));

    private async Task InitializeStorageClients()
    {
        queueClientPrimary ??= await clientFactory.GetOrCreateQueue(QUEUE_PRIMARY_NAME);
        queueClientDeadLetter ??= await clientFactory.GetOrCreateQueue(QUEUE_DEAD_LETTER_NAME);
        containerClientPrimary ??= await clientFactory.GetOrCreateContainerClient(CONTAINER_PRIMARY_NAME);
        containerClientDeadLetter ??= await clientFactory.GetOrCreateContainerClient(CONTAINER_DEAD_LETTER_NAME);
    }
}

public class SupportRequestProcessingSettings
{
    /// <summary>
    /// If enabled, support request processing is determined by the Xperience database configuration. Defaults to true.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

public class SupportRequestQueueMessage
{
    public string Subject { get; set; } = "";
    public string BlobName { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string AuthorEmail { get; set; } = "";
}

public class SupportRequestProcessException(SupportRequestQueueMessage queueMessage, string message) : Exception(message)
{
    public SupportRequestQueueMessage QueueMessage { get; } = queueMessage;
};

/// <summary>
/// The request payload sent to the <see cref="SupportRequestConfigurationInfo.SupportRequestConfigurationExternalEndpointURL"/>
/// This contract is owned by internal operations
/// </summary>
public class SupportRequestMessage
{
    [JsonRequired]
    public string FirstName { get; set; } = "";

    [JsonRequired]
    public string LastName { get; set; } = "";

    [JsonRequired]
    public string Email { get; set; } = "";

    public string Company { get; set; } = "";

    #region Data for email only

    [JsonRequired]
    public string Subject { get; set; } = "";

    public string Description { get; set; } = "";

    public string Version { get; set; } = "";

    public string Hotfix { get; set; } = "";

    public string DevelopmentModel { get; set; } = "";

    public string URL { get; set; } = "";

    #region Support Issue only

    public string Scenario { get; set; } = "";

    public string Fix { get; set; } = "";

    public string Phone { get; set; } = "";

    #endregion Support Issue only

    #region SLA only

    public string TicketLevel { get; set; } = "";

    public string ExpectedOutcome { get; set; } = "";

    public string StepsYouHavePerformed { get; set; } = "";

    public string IssueComments { get; set; } = "";

    public string ActualOutcome { get; set; } = "";

    public string CMSVersion { get; set; } = "";

    public string WasTheCMSUpgraded { get; set; } = "";

    public string Browser { get; set; } = "";

    public string IISVersion { get; set; } = "";

    public string SQLServer { get; set; } = "";

    public string OperatingSystem { get; set; } = "";

    public string DOTNETVersion { get; set; } = "";

    #endregion SLA only

    #endregion Data for email only

    /// <summary>
    /// Base64 encoded file
    /// Devnet name: Screenshot
    /// </summary>
    public string File { get; set; } = "";

    public string FileName { get; set; } = "";

    public bool IsSLA { get; set; }

    public bool Is2Level { get; set; }

    public bool IsKenticoSaaSProductionIssue { get; set; }
}

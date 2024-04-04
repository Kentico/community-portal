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

public class SupportRequestProcessorBackgroundService(
    ILogger<SupportRequestProcessorBackgroundService> logger,
    IHttpClientFactory httpClientFactory,
    AzureStorageClientFactory clientFactory,
    IInfoProvider<SupportRequestConfigurationInfo> configurationProvider,
    IInfoProvider<SupportRequestProcessingEventInfo> eventProvider,
    IProgressiveCache cache,
    IOptions<SupportRequestProcessingSettings> options) : BackgroundService
{
    public const string QUEUE_PRIMARY_NAME = "support-messages";
    public const string QUEUE_DEAD_LETTER_NAME = "support-messages-dead-letter";
    public const string CONTAINER_PRIMARY_NAME = "support-messages";
    public const string CONTAINER_DEAD_LETTER_NAME = "support-messages-dead-letter";

    private readonly ILogger<SupportRequestProcessorBackgroundService> logger = logger;
    private readonly IInfoProvider<SupportRequestConfigurationInfo> configurationProvider = configurationProvider;
    private readonly IInfoProvider<SupportRequestProcessingEventInfo> eventProvider = eventProvider;
    private readonly IProgressiveCache cache = cache;
    private readonly SupportRequestProcessingSettings settings = options.Value;
    private readonly AzureStorageClientFactory clientFactory = clientFactory;
    private QueueClient queueClientPrimary = null!;
    private QueueClient queueClientDeadLetter = null!;
    private BlobContainerClient containerClientPrimary = null!;
    private BlobContainerClient containerClientDeadLetter = null!;
    private readonly HttpClient httpClient = httpClientFactory.CreateClient();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var configuration = await GetConfiguration();

            if (settings.IsEnabled && (configuration?.SupportRequestConfigurationIsQueueProcessingEnabled ?? false))
            {
                await InitializeStorageClients();

                await ProcessQueueMessagesAsync(configuration, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessQueueMessagesAsync(SupportRequestConfigurationInfo configuration, CancellationToken stoppingToken)
    {
        var dequeueResponse = await queueClientPrimary.ReceiveMessageAsync(cancellationToken: stoppingToken);
        if (dequeueResponse.Value is not QueueMessage message)
        {
            logger.LogInformation("No messages in Azure queue {queueName}", QUEUE_PRIMARY_NAME);
            return;
        }

        try
        {
            var supportRequest = JsonSerializer.Deserialize<SupportRequestQueueMessage>(message.Body.ToString())
                ?? throw new InvalidOperationException($"Could not deserialize queue message: {message.MessageId}");

            var response = await SendToAzureFunctionAsync(configuration, supportRequest, stoppingToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new SupportRequestProcessException(supportRequest, $"Failed to send to Azure Function. Status: {response.StatusCode}");
            }

            _ = await queueClientPrimary.DeleteMessageAsync(dequeueResponse.Value.MessageId, dequeueResponse.Value.PopReceipt, stoppingToken);
            _ = await containerClientPrimary.DeleteBlobAsync(supportRequest.BlobName, cancellationToken: stoppingToken);
            eventProvider.Set(new SupportRequestProcessingEventInfo
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

            var receipt = await queueClientDeadLetter.SendMessageAsync(queueMessage.Body, cancellationToken: stoppingToken);
            _ = await queueClientPrimary.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, stoppingToken);

            if (ex is SupportRequestProcessException processEx)
            {
                await MoveBlobToDeadLetter(processEx.QueueMessage.BlobName);
            }

            eventProvider.Set(new SupportRequestProcessingEventInfo
            {
                SupportRequestProcessingEventStatus = SupportRequestProcessingEventInfo.Statuses.FAILURE.ToString(),
                SupportRequestProcessingEventMessage = ex.ToString(),
                SupportRequestProcessingEventMessageID = receipt.Value.MessageId
            });
        }
    }

    private async Task<HttpResponseMessage> SendToAzureFunctionAsync(SupportRequestConfigurationInfo configuration, SupportRequestQueueMessage message, CancellationToken stoppingToken)
    {
        var blobClient = containerClientPrimary.GetBlobClient(message.BlobName);
        var stream = await blobClient.OpenReadAsync(position: 0, cancellationToken: stoppingToken);
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var res = await httpClient.PostAsync(configuration.SupportRequestConfigurationExternalEndpointURL, content, stoppingToken);

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

            return (await configurationProvider.Get()
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();
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

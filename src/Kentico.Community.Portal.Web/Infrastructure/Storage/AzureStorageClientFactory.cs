using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Kentico.Xperience.AzureStorage;

namespace Kentico.Community.Portal.Web.Infrastructure.Storage;

public class AzureStorageClientFactory
{
    public async Task<QueueClient> GetOrCreateQueue(string queueName)
    {
        var current = AccountInfo.CurrentAccount;
        var credentials = new StorageSharedKeyCredential(current.AccountName, current.SharedKey);
        var queueUriBuilder = new QueueUriBuilder(new Uri(current.QueueEndpoint))
        {
            QueueName = queueName,
        };
        var uri = queueUriBuilder.ToUri();
        var client = new QueueClient(uri, credentials);
        _ = await client.CreateIfNotExistsAsync();

        return client;
    }

    public async Task<BlobContainerClient> GetOrCreateContainerClient(string containerName)
    {
        var current = AccountInfo.CurrentAccount;
        var credentials = new StorageSharedKeyCredential(current.AccountName, current.SharedKey);
        var blobUriBuilder = new BlobUriBuilder(new Uri(current.BlobEndpoint));
        var uri = blobUriBuilder.ToUri();
        var serviceClient = new BlobServiceClient(uri, credentials);
        var containerClient = serviceClient.GetBlobContainerClient(containerName);
        _ = await containerClient.CreateIfNotExistsAsync();

        return containerClient;
    }
}

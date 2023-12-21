using CMS.Core;
using CMS.DataEngine;
using CMS.IO;
using Kentico.Xperience.AzureStorage;
using Kentico.Xperience.Cloud;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class StorageInitializationModule : Module
{
    private const string LocalStorageAssetsDirectoryName = "$StorageAssets";
    private const string ContainerName = "default";

    public StorageInitializationModule() : base(nameof(StorageInitializationModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit();

        var environment = parameters.Services.GetRequiredService<IWebHostEnvironment>();

        if (environment.IsQa() || environment.IsUat() || environment.IsProduction())
        {
            // Maps the assets directory (e.g. media files) to the Azure storage provider
            MapAzureStoragePath($"~/assets/");
        }
        else
        {
            // Maps the assets directory (e.g. media files) to the dedicated local folder
            MapLocalStoragePath($"~/assets/");
        }
    }

    private static void MapAzureStoragePath(string path)
    {
        // Creates a new StorageProvider instance for Azure
        var provider = AzureStorageProvider.Create();

        // Specifies the target container
        provider.CustomRootPath = ContainerName;
        provider.PublicExternalFolderObject = false;

        StorageHelper.MapStoragePath(path, provider);
    }

    private static void MapLocalStoragePath(string path)
    {
        // Creates a new StorageProvider instance for local storage
        var provider = StorageProvider.CreateFileSystemStorageProvider();

        provider.CustomRootPath = $"{LocalStorageAssetsDirectoryName}/{ContainerName}";

        StorageHelper.MapStoragePath(path, provider);
    }
}

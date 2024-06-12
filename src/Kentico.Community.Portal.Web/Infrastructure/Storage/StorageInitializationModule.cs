using CMS.Core;
using CMS.DataEngine;
using CMS.IO;
using Kentico.Xperience.AzureStorage;
using Kentico.Xperience.Cloud;

using Path = CMS.IO.Path;

namespace Kentico.Community.Portal.Web.Infrastructure.Storage;

public class StorageInitializationModule : Module
{
    private IWebHostEnvironment environment = null!;

    public StorageInitializationModule() : base(nameof(StorageInitializationModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit();

        environment = parameters.Services.GetRequiredService<IWebHostEnvironment>();
        var storagePathService = parameters.Services.GetRequiredService<StoragePathService>();

        string xperienceAssetsContainer = storagePathService.GetXperienceAssetsContainerPath();
        string memberAssetsContainer = storagePathService.GetXperienceAssetsContainerPath();

        if (environment.IsQa() || environment.IsUat() || environment.IsProduction())
        {
            MapAzureStoragePath($"~/assets/", xperienceAssetsContainer);
            MapAzureStoragePath($"~/member-assets/", memberAssetsContainer);
        }
        else
        {
            MapLocalStoragePath($"~/assets/", xperienceAssetsContainer);
            MapLocalStoragePath($"~/member-assets/", memberAssetsContainer);
        }
    }

    private static void MapAzureStoragePath(string path, string containerName)
    {
        var provider = AzureStorageProvider.Create();

        provider.CustomRootPath = containerName;
        provider.PublicExternalFolderObject = false;

        StorageHelper.MapStoragePath(path, provider);
    }

    private static void MapLocalStoragePath(string path, string rootPath)
    {
        var provider = StorageProvider.CreateFileSystemStorageProvider();

        provider.CustomRootPath = rootPath;

        StorageHelper.MapStoragePath(path, provider);
    }
}

public class StoragePathService(IWebHostEnvironment environment)
{
    public const string ContainerNameDefault = "default";

    private const string LocalStorageAssetsDirectoryName = "$StorageAssets";
    private const string LocalStorageMemberAssetsDirectoryName = "$StorageMemberAssets";

    private readonly IWebHostEnvironment environment = environment;

    public string GetMemberAssetsStorageFilePath(string filePath) =>
        Path.Combine(GetMemberAssetsContainerPath(), filePath);

    public string GetXperienceAssetsContainerPath() =>
        environment.IsQa() || environment.IsUat() || environment.IsProduction()
            ? ContainerNameDefault
            : Path.Combine(LocalStorageAssetsDirectoryName, ContainerNameDefault);

    public string GetMemberAssetsContainerPath() =>
        environment.IsQa() || environment.IsUat() || environment.IsProduction()
            ? ContainerNameDefault
            : Path.Combine(LocalStorageMemberAssetsDirectoryName, ContainerNameDefault);
}

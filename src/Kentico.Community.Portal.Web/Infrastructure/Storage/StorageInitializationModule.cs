using CMS.Core;
using CMS.DataEngine;
using CMS.IO;
using Kentico.Community.Portal.Core.Infrastructure;
using Kentico.Xperience.AzureStorage;
using Kentico.Xperience.Cloud;
using static Kentico.Community.Portal.Core.Infrastructure.IStoragePathService;

using Path = CMS.IO.Path;

namespace Kentico.Community.Portal.Web.Infrastructure.Storage;

public class StorageInitializationModule : Module
{
    public StorageInitializationModule() : base(nameof(StorageInitializationModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit();

        var storagePathService = parameters.Services.GetRequiredService<IStoragePathService>();

        string xperiencePrefix = storagePathService.GetStoragePathPrefix(StorageAssetType.Xperience);
        string xperienceContainer = storagePathService.GetContainerPath(StorageAssetType.Xperience);

        string memberPrefix = storagePathService.GetStoragePathPrefix(StorageAssetType.Member);
        string memberContainer = storagePathService.GetContainerPath(StorageAssetType.Member);

        if (storagePathService.ShouldMapAzureStorage)
        {
            MapAzureStoragePath(xperiencePrefix, xperienceContainer);
            MapAzureStoragePath(memberPrefix, memberContainer);
        }
        else
        {
            MapLocalStoragePath(xperiencePrefix, xperienceContainer);
            MapLocalStoragePath(memberPrefix, memberContainer);
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

public class StoragePathService(IWebHostEnvironment environment) : IStoragePathService
{
    private const string ContainerNameDefault = "default";

    private const string AzureStorageXperienceAssetsPathPrefix = "assets";
    private const string AzureStorageMemberAssetsPathPrefix = "member-assets";

    private const string LocalStorageXperienceAssetsDirectoryName = "$StorageAssets";
    private const string LocalStorageMemberAssetsDirectoryName = "$StorageMemberAssets";

    private readonly IWebHostEnvironment environment = environment;

    public bool ShouldMapAzureStorage =>
        environment.IsQa() ||
        environment.IsUat() ||
        environment.IsProduction();

    public string GetStoragePathPrefix(StorageAssetType assetType)
    {
        string path = assetType switch
        {
            StorageAssetType.Xperience => AzureStorageXperienceAssetsPathPrefix,
            StorageAssetType.Member => AzureStorageMemberAssetsPathPrefix,
            _ => throw new ArgumentOutOfRangeException(nameof(assetType))
        };

        return $"~/{path}/";
    }

    public string GetStorageFilePath(string filePath, StorageAssetType assetType)
    {
        string rootPath = assetType switch
        {
            StorageAssetType.Member => AzureStorageMemberAssetsPathPrefix,
            StorageAssetType.Xperience or _ => throw new ArgumentOutOfRangeException(nameof(assetType))
        };

        return Path.Combine(rootPath, filePath);
    }

    public string GetContainerPath(StorageAssetType assetType)
    {
        string path = assetType switch
        {
            StorageAssetType.Xperience => ShouldMapAzureStorage
                ? ContainerNameDefault
                : Path.Combine(LocalStorageXperienceAssetsDirectoryName, ContainerNameDefault),
            StorageAssetType.Member => ShouldMapAzureStorage
                ? ContainerNameDefault
                : Path.Combine(LocalStorageMemberAssetsDirectoryName, ContainerNameDefault),
            _ => throw new ArgumentOutOfRangeException(nameof(assetType))
        };

        return path;
    }
}

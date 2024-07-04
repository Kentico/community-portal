namespace Kentico.Community.Portal.Core.Infrastructure;

public interface IStoragePathService
{
    public enum StorageAssetType
    {
        Xperience,
        Member
    }

    bool ShouldMapAzureStorage { get; }

    string GetStoragePathPrefix(StorageAssetType assetType);
    string GetStorageFilePath(string filePath, StorageAssetType assetType);
    string GetContainerPath(StorageAssetType assetType);
}

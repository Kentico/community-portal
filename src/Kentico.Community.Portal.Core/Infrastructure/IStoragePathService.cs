namespace Kentico.Community.Portal.Core.Infrastructure;

public interface IStoragePathService
{
    public enum StorageAssetType
    {
        Xperience,
        Member
    }

    public bool ShouldMapAzureStorage { get; }

    public string GetStoragePathPrefix(StorageAssetType assetType);
    public string GetStorageFilePath(string filePath, StorageAssetType assetType);
    public string GetContainerPath(StorageAssetType assetType);
}

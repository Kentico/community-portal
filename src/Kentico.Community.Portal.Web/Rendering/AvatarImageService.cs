using System.Data;
using System.Data.Common;
using CMS.Base;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.IO;
using CMS.Membership;
using Kentico.Community.Portal.Core.Infrastructure;
using static Kentico.Community.Portal.Core.Infrastructure.IStoragePathService;
using FileInfo = CMS.IO.FileInfo;
using Path = CMS.IO.Path;

namespace Kentico.Community.Portal.Web.Rendering;

public class AvatarImageService(
    IWebHostEnvironment webHostEnvironment,
    IStoragePathService storagePathService,
    IInfoProvider<MemberInfo> memberProvider,
    IProgressiveCache cache,
    IReadOnlyModeProvider readOnlyProvider)
{
    private const string STORAGE_FOLDER_NAME = "avatars";

    private readonly IWebHostEnvironment webHostEnvironment = webHostEnvironment;
    private readonly IStoragePathService storagePathService = storagePathService;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IProgressiveCache cache = cache;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    public async Task<FileInfo> GetAvatarImage(int memberID)
    {
        string fallbackPath = Path.Combine(webHostEnvironment.WebRootPath, "img", "profile-photo-default.png");
        string filePath = await GetAvatarFilePath(memberID) ?? fallbackPath;
        var fileInfo = StorageHelper.GetFileInfo(filePath);

        return fileInfo is null || !fileInfo.Exists
            ? StorageHelper.GetFileInfo(fallbackPath)
            : fileInfo;
    }

    public async Task UpdateAvatarImage(IFormFile avatarImageFile, int memberID)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return;
        }

        string? currentFile = await GetAvatarFilePath(memberID);

        if (currentFile is not null &&
            StorageHelper.GetFileInfo(currentFile) is FileInfo file &&
            file.Exists)
        {
            file.Delete();
        }

        string avatarRelativePath = Path.Combine(STORAGE_FOLDER_NAME, $"{memberID}{Path.GetExtension(avatarImageFile.FileName)}");
        string fullFilePath = storagePathService.GetStorageFilePath(avatarRelativePath, StorageAssetType.Member);
        using var stream = avatarImageFile.OpenReadStream();

        DirectoryHelper.EnsureDiskPath(fullFilePath, "");
        StorageHelper.SaveFileToDisk(fullFilePath, stream, false);
    }

    private async Task<string?> GetAvatarFilePath(int memberID)
    {
        var map = await GetMemberAvatarFileExtensionsInternal();

        string? path = map.TryGetValue(memberID, out string? name)
            ? storagePathService.GetStorageFilePath(Path.Combine(STORAGE_FOLDER_NAME, $"{memberID}{name}"), StorageAssetType.Member)
            : null;

        return path;
    }

    private async Task<Dictionary<int, string>> GetMemberAvatarFileExtensionsInternal()
    {
        var fileNameMap = await cache.LoadAsync(async cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency("cms.member|all");

            var map = new Dictionary<int, string>();
            var query = memberProvider.Get()
                .WhereNotEmpty("MemberAvatarFileExtension")
                .Columns(nameof(MemberInfo.MemberID), "MemberAvatarFileExtension");

            using (var reader = await query.ExecuteReaderAsync())
            {
                if (reader is not DbDataReader dbDataReader)
                {
                    return map;
                }

                while (dbDataReader.Read())
                {
                    int memberID = dbDataReader.GetInt32(nameof(MemberInfo.MemberID));
                    string fileName = dbDataReader.GetString("MemberAvatarFileExtension");

                    map.Add(memberID, fileName);
                }
            }

            return map;
        }, new CacheSettings(5, nameof(GetMemberAvatarFileExtensionsInternal)));

        return fileNameMap;
    }
}

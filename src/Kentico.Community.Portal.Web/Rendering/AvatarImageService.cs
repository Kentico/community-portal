using System.Data;
using System.Data.Common;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.IO;
using CMS.Membership;
using Kentico.Community.Portal.Web.Infrastructure.Storage;
using SkiaSharp;
using Path = CMS.IO.Path;
using FileInfo = CMS.IO.FileInfo;

namespace Kentico.Community.Portal.Web.Rendering;

public class AvatarImageService(
    IWebHostEnvironment webHostEnvironment,
    StoragePathService storagePathService,
    IInfoProvider<MemberInfo> memberProvider,
    IProgressiveCache cache)
{
    private const string STORAGE_FOLDER_NAME = "avatars";

    private readonly IWebHostEnvironment webHostEnvironment = webHostEnvironment;
    private readonly StoragePathService storagePathService = storagePathService;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IProgressiveCache cache = cache;

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
        string? currentFile = await GetAvatarFilePath(memberID);

        if (currentFile is not null &&
            StorageHelper.GetFileInfo(currentFile) is FileInfo file &&
            file.Exists)
        {
            file.Delete();
        }

        string filePath = storagePathService.GetMemberAssetsStorageFilePath(Path.Combine(STORAGE_FOLDER_NAME, $"{memberID}{Path.GetExtension(avatarImageFile.FileName)}"));
        using var stream = avatarImageFile.OpenReadStream();

        DirectoryHelper.EnsureDiskPath(filePath, "");
        StorageHelper.SaveFileToDisk(filePath, stream, false);
    }

    private async Task<string?> GetAvatarFilePath(int memberID)
    {
        var map = await GetMemberAvatarFileExtensionsInternal();

        return map.TryGetValue(memberID, out string? name)
            ? storagePathService.GetMemberAssetsStorageFilePath(Path.Combine(STORAGE_FOLDER_NAME, $"{memberID}{name}"))
            : null;
    }

    private static Stream GetPngImageStream(string filePath)
    {
        var image = SKImage.FromEncodedData(filePath);
        var imageStream = image.Encode().AsStream();
        imageStream.Position = 0;

        return imageStream;
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

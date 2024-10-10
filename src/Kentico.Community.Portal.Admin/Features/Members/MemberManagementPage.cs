using CMS.IO;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Core.Infrastructure;
using Kentico.Xperience.Admin.Base;
using static Kentico.Community.Portal.Core.Infrastructure.IStoragePathService;
using Path = CMS.IO.Path;

[assembly: UIPage(
    uiPageType: typeof(MemberManagementPage),
    parentType: typeof(CommunityMembersApplicationPage),
    slug: "management",
    name: "Management",
    templateName: "@kentico-community/portal-web-admin/MemberManagementLayout",
    order: 1,
    Icon = Icons.Cogwheels)]

namespace Kentico.Community.Portal.Admin.Features.Members;

public class MemberManagementPage(IStoragePathService storagePathService) : Page<MemberManagementPageClientProperties>
{
    public const string IDENTIFIER = "management";

    private readonly IStoragePathService storagePathService = storagePathService;

    public override async Task<MemberManagementPageClientProperties> ConfigureTemplateProperties(MemberManagementPageClientProperties properties)
    {
        var unmigratedDirectory = StorageHelper.GetDirectoryInfo(Path.Combine("default", "avatars"));
        var unmigratedFiles = unmigratedDirectory.Exists
            ? unmigratedDirectory.GetFiles()
            : [];

        string fullDirectoryPath = storagePathService.GetStorageFilePath("avatars", StorageAssetType.Member);
        var correctDirectory = StorageHelper.GetDirectoryInfo(fullDirectoryPath);
        var correctFiles = correctDirectory.Exists
            ? correctDirectory.GetFiles()
            : [];

        properties.IncorrectAvatarFiles = unmigratedFiles.Select(f => f.FullName).ToArray();
        properties.CorrectAvatarFiles = correctFiles.Select(f => f.FullName).ToArray();

        await Task.CompletedTask;

        return properties;
    }

    [PageCommand]
    public async Task<ICommandResponse> MigrateOldAvatarPaths()
    {
        var result = new AvatarPathMigrationResult([]);
        var directory = StorageHelper.GetDirectoryInfo(Path.Combine("default", "avatars"));
        var files = directory.GetFiles();

        foreach (var file in files)
        {
            using var stream = file.OpenRead();

            string avatarRelativePath = Path.Combine("avatars", file.Name);
            string fullFilePath = storagePathService.GetStorageFilePath(avatarRelativePath, StorageAssetType.Member);

            DirectoryHelper.EnsureDiskPath(fullFilePath, "");
            StorageHelper.SaveFileToDisk(fullFilePath, stream, false);

            var migratedFile = StorageHelper.GetFileInfo(fullFilePath);
            result.MigratedPaths.Add(migratedFile.FullName);
        }

        await Task.CompletedTask;

        return ResponseFrom(result).AddSuccessMessage("All files migrated");
    }
}

public class MemberManagementPageClientProperties : TemplateClientProperties
{
    public IEnumerable<string> IncorrectAvatarFiles { get; set; } = [];
    public IEnumerable<string> CorrectAvatarFiles { get; set; } = [];
}

public record AvatarPathMigrationResult(List<string> MigratedPaths);


using CMS.Base;
using CMS.ContentEngine;
using CMS.IO;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using FileInfo = CMS.IO.FileInfo;
using Path = CMS.IO.Path;

namespace Kentico.Community.Portal.Web.Rendering;

public class AvatarImageService(
    IWebHostEnvironment webHostEnvironment,
    IReadOnlyModeProvider readOnlyProvider,
    MemberAssetContentService memberAssetContentService,
    ContentItemManagerCreator contentItemManagerCreator,
    ILogger<AvatarImageService> logger)
{
    private static readonly Guid avatarImageTagGuid = SystemTaxonomies.MemberAssetTypeTaxonomy.AvatarImageTag.GUID;

    private readonly IWebHostEnvironment webHostEnvironment = webHostEnvironment;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;
    private readonly MemberAssetContentService memberAssetContentService = memberAssetContentService;
    private readonly ContentItemManagerCreator contentItemManagerCreator = contentItemManagerCreator;
    private readonly ILogger<AvatarImageService> logger = logger;

    public async Task<MemberAvatarImageResult> GetAvatarImage(int memberID)
    {
        string fallbackPath = Path.Combine(webHostEnvironment.WebRootPath, "img", "profile-photo-default.png");
        string? contentHubAvatarUrl = await memberAssetContentService.GetContentHubAvatarUrl(memberID);
        if (!string.IsNullOrWhiteSpace(contentHubAvatarUrl))
        {
            logger.LogInformation(
                "Resolved avatar for member {MemberID} from {Source}",
                memberID,
                "content-hub");

            return MemberAvatarImageResult.FromContentHub(contentHubAvatarUrl);
        }

        logger.LogInformation(
            "Resolved avatar for member {MemberID} from {Source}",
            memberID,
            "default-avatar");

        return MemberAvatarImageResult.FromLegacyFile(StorageHelper.GetFileInfo(fallbackPath));
    }

    public async Task UpdateAvatarImage(IFormFile avatarImageFile, int memberID)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return;
        }

        string extension = System.IO.Path.GetExtension(avatarImageFile.FileName);
        string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"member-avatar-{memberID}-{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var stream = avatarImageFile.OpenReadStream())
            {
                await using var tempStream = System.IO.File.Create(tempFilePath);
                await stream.CopyToAsync(tempStream);
            }

            await UpdateContentHubAvatar(memberID, tempFilePath, avatarImageFile.FileName);

            logger.LogInformation("Updated avatar for member {MemberID} in Content Hub", memberID);
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }

    private async Task UpdateContentHubAvatar(int memberID, string fullFilePath, string fileName)
    {
        var contentItemManager = await contentItemManagerCreator.GetContentItemManager();
        var existing = await memberAssetContentService.GetAvatarContentByMemberID(memberID);

        var file = FileInfo.New(fullFilePath);
        var assetMetadata = new ContentItemAssetMetadata
        {
            Extension = file.Extension,
            Identifier = Guid.NewGuid(),
            LastModified = DateTime.UtcNow,
            Name = fileName,
            Size = file.Length
        };

        var fileSource = new ContentItemAssetFileSource(file.FullName, false);
        var assetMetadataWithSource = new ContentItemAssetMetadataWithSource(fileSource, assetMetadata);

        var contentItemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(MemberAssetContent.MemberAssetContentImageAsset), assetMetadataWithSource },
            { nameof(MemberAssetContent.MemberAssetContentMemberID), memberID },
            {
                nameof(MemberAssetContent.MemberAssetContentMemberAssetTypes),
                new List<TagReference> { new() { Identifier = avatarImageTagGuid } }
            }
        });

        if (existing is null)
        {
            var createParameters = new CreateContentItemParameters(
                MemberAssetContent.CONTENT_TYPE_NAME,
                null,
                $"Member Avatar {memberID}",
                PortalWebSiteChannel.DEFAULT_LANGUAGE,
                PortalWebSiteChannel.WORKSPACE_MEMBER_ASSETS)
            {
                AccessSettings = ContentAccessSettings.Public(),
                VersionStatus = VersionStatus.Published
            };

            _ = await contentItemManager.Create(createParameters, contentItemData);
            return;
        }

        _ = await contentItemManager.TryCreateDraft(existing.SystemFields.ContentItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE);
        bool updated = await contentItemManager.TryUpdateDraft(existing.SystemFields.ContentItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, contentItemData);

        if (!updated)
        {
            throw new InvalidOperationException($"Could not update Content Hub avatar for member {memberID}.");
        }

        _ = await contentItemManager.TryPublish(existing.SystemFields.ContentItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE);
    }
}

public record MemberAvatarImageResult(string? ContentHubAvatarUrl, FileInfo? LegacyAvatarFile)
{
    public bool HasContentHubAvatar => !string.IsNullOrWhiteSpace(ContentHubAvatarUrl);

    public static MemberAvatarImageResult FromContentHub(string contentHubAvatarUrl) =>
        new(contentHubAvatarUrl, null);

    public static MemberAvatarImageResult FromLegacyFile(FileInfo? legacyAvatarFile) =>
        new(null, legacyAvatarFile);
}

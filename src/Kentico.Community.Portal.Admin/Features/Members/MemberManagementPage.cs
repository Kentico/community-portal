using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.Migrations;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(MemberManagementPage),
    parentType: typeof(CommunityMembersApplicationPage),
    slug: "management",
    name: "Management",
    templateName: "@kentico-community/portal-web-admin/MemberManagementLayout",
    order: 1,
    Icon = Icons.Cogwheels)]

namespace Kentico.Community.Portal.Admin.Features.Members;

public class MemberManagementPage(
    ) : Page<MemberManagementPageClientProperties>
{
    private const string AvatarMigrationRetiredMessage = "Avatar migration and legacy filesystem inventory are retired because avatars are now fully served from Content Hub assets.";

    public override Task<MemberManagementPageClientProperties> ConfigureTemplateProperties(MemberManagementPageClientProperties properties)
    {
        properties.AvatarMigrationAnalysis = BuildAvatarMigrationAnalysis();
        properties.DefaultBatchSize = 10;

        return Task.FromResult(properties);
    }

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse> AnalyzeMemberAvatarsForContentHubMigration()
    {
        var analysis = BuildAvatarMigrationAnalysis();
        return Task.FromResult<ICommandResponse>(
            ResponseFrom(analysis)
                .AddErrorMessage(AvatarMigrationRetiredMessage));
    }

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse> MigrateAvatarsToContentHub(MigrateMemberAvatarsToContentHubCommandParams _)
    {
        var migrationResult = new MigrationResult(
            "member-avatar-content-hub-retired",
            "Member Avatar Content Hub Migration (Retired)",
            [],
            [AvatarMigrationRetiredMessage],
            0);

        return Task.FromResult<ICommandResponse>(
            ResponseFrom(migrationResult)
                .AddErrorMessage(AvatarMigrationRetiredMessage));
    }

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse> MigrateSingleAvatarToContentHub(MigrateSingleMemberAvatarToContentHubCommandParams _)
    {
        var migrationResult = new MigrationResult(
            "member-avatar-content-hub-retired",
            "Member Avatar Content Hub Migration (Retired)",
            [],
            [AvatarMigrationRetiredMessage],
            0);

        return Task.FromResult<ICommandResponse>(
            ResponseFrom(migrationResult)
                .AddErrorMessage(AvatarMigrationRetiredMessage));
    }

    private static MemberAvatarMigrationAnalysisResult BuildAvatarMigrationAnalysis() => new(0, 0, 0, 0, 0, [], []);
}

public class MemberManagementPageClientProperties : TemplateClientProperties
{
    public MemberAvatarMigrationAnalysisResult AvatarMigrationAnalysis { get; set; } = new(0, 0, 0, 0, 0, [], []);
    public int DefaultBatchSize { get; set; } = 10;
}

public record MigrateMemberAvatarsToContentHubCommandParams(int BatchSize = 10);

public record MigrateSingleMemberAvatarToContentHubCommandParams(int MemberID);

public record MemberAvatarMigrationAnalysisResult(
    int MembersWithAvatarFiles,
    int MembersMissingAvatarFiles,
    int MembersAlreadyInContentHub,
    int MembersPendingMigration,
    long TotalAvatarBytes,
    IEnumerable<MemberAvatarMigrationCandidate> Candidates,
    IEnumerable<ManagedAvatarFileInventoryItem> ManagedAvatarFiles);

public record MemberAvatarMigrationCandidate(
    int MemberID,
    string AvatarFileExtension,
    string AvatarPath,
    long AvatarSizeBytes,
    bool ExistsInContentHub,
    string AvatarUrl);

public record ManagedAvatarFileInventoryItem(
    string FileName,
    string AvatarPath,
    string AvatarExtension,
    long AvatarSizeBytes,
    DateTime LastModifiedUtc,
    DateTime CreatedUtc,
    bool IsReadOnly,
    int? MemberID,
    bool MemberExists,
    string MemberUserName,
    string MemberFullName,
    string MemberEmail,
    string MemberAvatarFileExtension,
    bool ExistsInContentHub);


using CMS.ContentEngine;
using CMS.Membership;
using CMS.Websites;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.Migrations;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Content;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Authentication;

[assembly: UIPage(
    uiPageType: typeof(MigrationManagementPage),
    parentType: typeof(MigrationsApplicationPage),
    slug: "management",
    name: "Management",
    templateName: "@kentico-community/portal-web-admin/MigrationManagementLayout",
    order: 1,
    Icon = Icons.Cogwheels)]

namespace Kentico.Community.Portal.Admin.Features.Migrations;

public class MigrationManagementPage(IEnumerable<IDataMigrator> migrators) : Page<MigrationManagementPageClientProperties>
{
    public const string COMMAND_NAME = "Migrate";

    public override async Task<MigrationManagementPageClientProperties> ConfigureTemplateProperties(MigrationManagementPageClientProperties properties)
    {
        await Task.CompletedTask;

        List<MigrationState> migrations = [];
        foreach (var dataMigrator in migrators)
        {
            var items = await dataMigrator.MigrateableItems();
            migrations.Add(new(dataMigrator.Name, dataMigrator.DisplayName, items.Count));
        }
        properties.AvailableMigrations = migrations;

        properties.CommandName = COMMAND_NAME;
        return properties;
    }

    [PageCommand(CommandName = COMMAND_NAME, Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse> Migrate(MigrateCommandParams commandParams)
    {
        var (name, count) = commandParams;
        var migration = migrators.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

        if (migration is null)
        {
            return ResponseFrom(new MigrationResult(name, "", [], [], 0))
                .AddErrorMessage($"Migration [{name}] is not registered");
        }

        var result = await migration.Migrate(count);

        return ResponseFrom(result)
            .AddSuccessMessage("Migration complete");
    }
}

public class MigrationManagementPageClientProperties : TemplateClientProperties
{
    public string CommandName { get; set; } = "";
    public IEnumerable<MigrationState> AvailableMigrations { get; set; } = [];
}

public record MigrateCommandParams(string MigrationName, int MigrateItemsCount = 0);

public interface IDataMigrator
{
    public string Name { get; }
    public string DisplayName { get; }
    public Task<Dictionary<string, string>> MigrateableItems(CancellationToken token = default);
    public Task<MigrationResult> Migrate(int count, CancellationToken token = default);
}

public record MigrationState(string Name, string DisplayName, int MigratableItemsCount);
public record MigrationResult(
    string Name,
    string DisplayName,
    IEnumerable<string> Successes,
    IEnumerable<string> Failures,
    int MigratableItemsCount);

public class BlogPostPageMigrator(
    IContentQueryExecutor queryExecutor,
    IWebPageManagerFactory managerFactory,
    IAuthenticatedUserAccessor userAccessor) : IDataMigrator
{
    public string Name { get; } = "BlogPostPages";
    public string DisplayName { get; } = "Blog post pages";
    private readonly IContentQueryExecutor queryExecutor = queryExecutor;
    private readonly IWebPageManagerFactory managerFactory = managerFactory;
    private readonly IAuthenticatedUserAccessor userAccessor = userAccessor;

    public async Task<Dictionary<string, string>> MigrateableItems(CancellationToken token = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                BlogPostPage.CONTENT_TYPE_NAME,
                q => q.Where(w => w
                    .WhereEquals(nameof(BlogPostPage.BlogPostPageIsContentMigrated), false)
                    .Or()
                    .WhereNull(nameof(BlogPostPage.BlogPostPageIsContentMigrated))));
        var pages = await queryExecutor.GetMappedResult<BlogPostPage>(query, cancellationToken: token);

        return pages
            .Select(p => (p.SystemFields.ContentItemID, p.SystemFields.ContentItemName))
            .ToDictionary(p => p.ContentItemID.ToString(), p => p.ContentItemName);
    }

    public async Task<MigrationResult> Migrate(int count, CancellationToken token = default)
    {
        List<string> successes = [];
        List<string> failures = [];

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(query =>
                query
                    .ForWebsite()
                    .OfContentType(BlogPostPage.CONTENT_TYPE_NAME)
                    .WithContentTypeFields()
                    .WithLinkedItems(3))
            .Parameters(q =>
            {
                _ = q
                .Where(w => w
                    .WhereEquals(nameof(BlogPostPage.BlogPostPageIsContentMigrated), false)
                    .Or()
                    .WhereNull(nameof(BlogPostPage.BlogPostPageIsContentMigrated)));

                if (count > 0)
                {
                    _ = q.TopN(count);
                }
            })
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);
        var pages = await queryExecutor.GetMappedWebPageResult<BlogPostPage>(query, cancellationToken: token);

        foreach (var page in pages)
        {
            var user = await userAccessor.Get();
            var manager = managerFactory.Create(page.SystemFields.WebPageItemWebsiteChannelId, user.UserID);

            bool draftSuccess = await manager.TryCreateDraft(page.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);

            if (!draftSuccess)
            {
                failures.Add($"Could not create draft for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                continue;
            }

            var content = page.BlogPostPageBlogPostContent.First();

            if (content is null)
            {
                failures.Add($"Could not find linked Blog Post Content for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                continue;
            }

            var itemData = new ContentItemData(new Dictionary<string, object>
            {
                {
                    nameof(BlogPostPage.WebPageMetaTitle),
                    content.ListableItemTitle
                },
                {
                    nameof(BlogPostPage.WebPageMetaDescription),
                    content.ListableItemShortDescription
                },
                {
                    nameof(BlogPostPage.BlogPostPagePublishedDate),
                    content.BlogPostContentPublishedDate
                },
                {
                    nameof(BlogPostPage.BlogPostPageAuthorContent),
                    content.BlogPostContentAuthor.Select(a => new ContentItemReference{ Identifier = a.SystemFields.ContentItemGUID }).ToList()
                },
                {
                    nameof(BlogPostPage.BlogPostPageBlogType),
                    content.BlogPostContentBlogType.Select(t => new TagReference{ Identifier = t.Identifier }).ToList()
                },
                {
                    nameof(BlogPostPage.BlogPostPageDXTopics),
                    content.BlogPostContentDXTopics.Select(t => new TagReference{ Identifier = t.Identifier }).ToList()
                },
                {
                    nameof(BlogPostPage.BlogPostPageIsContentMigrated),
                    true
                }
            });
            var draftData = new UpdateDraftData(itemData);
            bool update = await manager.TryUpdateDraft(page.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, draftData, token);
            if (!update)
            {
                failures.Add($"Could not update draft for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                _ = await manager.TryDiscardDraft(page.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
                continue;
            }

            bool publish = await manager.TryPublish(page.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
            if (!publish)
            {
                failures.Add($"Could not publish [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                _ = await manager.TryDiscardDraft(page.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
            }

            successes.Add($"[{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
        }

        query = new ContentItemQueryBuilder()
            .ForContentType(
                BlogPostPage.CONTENT_TYPE_NAME,
                q => q.Where(w => w.WhereEquals(nameof(BlogPostPage.BlogPostPageIsContentMigrated), false)
                    .Or()
                    .WhereNull(nameof(BlogPostPage.BlogPostPageIsContentMigrated))))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);
        var items = await queryExecutor.GetMappedResult<BlogPostPage>(query, cancellationToken: token);

        return new(Name, DisplayName, successes, failures, items.Count());
    }
}

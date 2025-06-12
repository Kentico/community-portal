using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core;

namespace Kentico.Community.Portal.Admin.Features.Migrations;

public class BlogPostContentMigrator(
    IContentQueryExecutor queryExecutor,
    ContentItemManagerCreator contentItemManagerCreator)
    : IDataMigrator
{
    public string Name => nameof(BlogPostContentMigrator);
    public string DisplayName => "Blog post content items";

    public async Task<Dictionary<string, string>> MigrateableItems(CancellationToken token = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfContentType(BlogPostContent.CONTENT_TYPE_NAME).WithContentTypeFields())
            .Parameters(q => q.Where(w => w
                .WhereNull(nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics))
                .Or()
                .WhereEmpty(nameof(BlogPostContent.BlogPostContentDXTopics))))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var items = await queryExecutor.GetMappedResult<BlogPostContent>(query, cancellationToken: token);
        return items.ToDictionary(p => p.SystemFields.ContentItemGUID.ToString(), p => p.SystemFields.ContentItemName);
    }

    public async Task<MigrationResult> Migrate(int count, CancellationToken token = default)
    {
        List<string> successes = [];
        List<string> failures = [];

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.WithContentTypeFields().OfContentType(BlogPostContent.CONTENT_TYPE_NAME))
            .Parameters(q =>
            {
                _ = q
                    .Where(w => w
                        .WhereNull(nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics))
                        .Or()
                        .WhereEmpty(nameof(BlogPostContent.BlogPostContentDXTopics)))
                    .OrderBy(nameof(BlogPostContent.SystemFields.ContentItemID));
                if (count > 0)
                {
                    _ = q.TopN(count);
                }
            })
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var items = await queryExecutor.GetMappedResult<BlogPostContent>(query, cancellationToken: token);

        foreach (var item in items)
        {
            try
            {
                var manager = await contentItemManagerCreator.GetContentItemManager();
                bool draftSuccess = await manager.TryCreateDraft(item.SystemFields.ContentItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
                if (!draftSuccess)
                {
                    failures.Add($"Could not create draft for [{item.SystemFields.ContentItemID}] - {item.SystemFields.ContentItemName}");
                    continue;
                }

                var itemData = new ContentItemData(new Dictionary<string, object>
                {
                    [nameof(BlogPostContent.CoreTaxonomyDXTopics)] = item.BlogPostContentDXTopics?.ToArray() ?? [],
                });

                bool updateSuccess = await manager.TryUpdateDraft(item.SystemFields.ContentItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, itemData, token);
                if (!updateSuccess)
                {
                    failures.Add($"Could not update draft for [{item.SystemFields.ContentItemID}] - {item.SystemFields.ContentItemName}");
                    continue;
                }

                bool publishSuccess = await manager.TryPublish(item.SystemFields.ContentItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
                if (!publishSuccess)
                {
                    failures.Add($"Could not publish draft for [{item.SystemFields.ContentItemID}] - {item.SystemFields.ContentItemName}");
                    continue;
                }

                successes.Add($"Migrated [{item.SystemFields.ContentItemID}] - {item.SystemFields.ContentItemName}");
            }
            catch (Exception ex)
            {
                failures.Add($"Exception for [{item.SystemFields.ContentItemID}] - {item.SystemFields.ContentItemName}: {ex.Message}");
            }
        }

        return new MigrationResult(Name, DisplayName, successes, failures, items.Count());
    }
}

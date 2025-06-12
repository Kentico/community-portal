using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using CMS.Websites;
using CSharpFunctionalExtensions;

namespace Kentico.Community.Portal.Admin.Features.Migrations;

public class BlogPostPageMigrator(
    IContentQueryExecutor queryExecutor,
    ContentItemManagerCreator contentItemManagerCreator)
    : IDataMigrator
{
    public string Name => nameof(BlogPostPageMigrator);
    public string DisplayName => "Blog post pages";

    public async Task<Dictionary<string, string>> MigrateableItems(CancellationToken token = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfContentType(BlogPostPage.CONTENT_TYPE_NAME))
            .Parameters(q => q.Where(w => w
                .WhereNull(nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics))))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await queryExecutor.GetMappedWebPageResult<BlogPostPage>(query, cancellationToken: token);
        return pages.ToDictionary(p => p.SystemFields.ContentItemGUID.ToString(), p => p.SystemFields.ContentItemName);
    }

    public async Task<MigrationResult> Migrate(int count, CancellationToken token = default)
    {
        List<string> successes = [];
        List<string> failures = [];

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.ForWebsite().WithContentTypeFields().OfContentType(BlogPostPage.CONTENT_TYPE_NAME))
            .Parameters(q =>
            {
                _ = q
                    .Where(w => w
                        .WhereNull(nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics)))
                    .OrderBy(nameof(BlogPostPage.SystemFields.ContentItemID));

                if (count > 0)
                {
                    _ = q.TopN(count);
                }
            })
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await queryExecutor.GetMappedWebPageResult<BlogPostPage>(query, cancellationToken: token);

        foreach (var page in pages)
        {
            try
            {
                var manager = await contentItemManagerCreator.GetWebPageItemManager(page.SystemFields.WebPageItemWebsiteChannelId);
                bool draftSuccess = await manager.TryCreateDraft(page.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
                if (!draftSuccess)
                {
                    failures.Add($"Could not create draft for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                    continue;
                }

                var itemData = new ContentItemData(new Dictionary<string, object>
                {
                    [nameof(BlogPostPage.BasicItemTitle)] = page.WebPageMetaTitle,
                    [nameof(BlogPostPage.CoreTaxonomyDXTopics)] = page.BlogPostPageDXTopics?.ToArray() ?? [],
                });

                await GetQAndADiscussionContentItemGUID(page, token)
                    .Execute(g =>
                        itemData.SetValue<IEnumerable<ContentItemReference>>(
                            nameof(BlogPostPage.BlogPostPageQAndAQuestionPages),
                            [new ContentItemReference { Identifier = g }]));

                bool updateSuccess = await manager.TryUpdateDraft(page.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, new UpdateDraftData(itemData), token);
                if (!updateSuccess)
                {
                    failures.Add($"Could not update draft for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                    continue;
                }

                bool publishSuccess = await manager.TryPublish(page.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
                if (!publishSuccess)
                {
                    failures.Add($"Could not publish draft for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                    continue;
                }

                successes.Add($"Migrated [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
            }
            catch (Exception ex)
            {
                failures.Add($"Exception for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}: {ex.Message}");
            }
        }

        return new MigrationResult(Name, DisplayName, successes, failures, pages.Count());
    }

    private async Task<Maybe<Guid>> GetQAndADiscussionContentItemGUID(BlogPostPage page, CancellationToken cancellationToken)
    {
        var webPageGUID = (page.BlogPostPageQAndADiscussionPage ?? []).Select(p => p.WebPageGuid).FirstOrDefault();

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.ForWebsite().OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME))
            .Parameters(q => q.Where(w => w.WhereEquals(nameof(QAndAQuestionPage.SystemFields.WebPageItemGUID), webPageGUID)))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await queryExecutor.GetMappedWebPageResult<QAndAQuestionPage>(query, cancellationToken: cancellationToken);

        return pages
            .TryFirst()
            .Map(p => p.SystemFields.ContentItemGUID);
    }
}

using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using CMS.Websites;
using CSharpFunctionalExtensions;

namespace Kentico.Community.Portal.Admin.Features.Migrations;

public class QAndAQuestionPageMigrator(
    IContentQueryExecutor queryExecutor,
    ContentItemManagerCreator contentItemManagerCreator)
    : IDataMigrator
{
    public string Name => nameof(QAndAQuestionPageMigrator);
    public string DisplayName => "Q&A question pages";

    public async Task<Dictionary<string, string>> MigrateableItems(CancellationToken token = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME).WithContentTypeFields())
            .Parameters(q => q
                .Where(w => w
                    .Where(w2 => w2
                        .WhereNull(nameof(QAndAQuestionPage.QAndAQuestionPageBlogPostPages))
                        .WhereEmpty(nameof(QAndAQuestionPage.BasicItemTitle))
                    .Or()
                    .WhereNull(nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics)))))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await queryExecutor.GetMappedWebPageResult<QAndAQuestionPage>(query, cancellationToken: token);
        return pages.ToDictionary(p => p.SystemFields.ContentItemGUID.ToString(), p => p.SystemFields.ContentItemName);
    }

    public async Task<MigrationResult> Migrate(int count, CancellationToken token = default)
    {
        List<string> successes = [];
        List<string> failures = [];

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.ForWebsite().WithContentTypeFields().OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME))
            .Parameters(q =>
            {
                _ = q
                    .Where(w => w
                        .Where(w2 => w2
                            .WhereNull(nameof(QAndAQuestionPage.QAndAQuestionPageBlogPostPages))
                            .WhereEmpty(nameof(QAndAQuestionPage.BasicItemTitle))
                        .Or()
                        .WhereNull(nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics))))
                    .OrderBy(nameof(QAndAQuestionPage.SystemFields.ContentItemID));

                if (count > 0)
                {
                    _ = q.TopN(count);
                }
            })
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await queryExecutor.GetMappedWebPageResult<QAndAQuestionPage>(query, cancellationToken: token);

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
                    [nameof(QAndAQuestionPage.BasicItemTitle)] = page.QAndAQuestionPageTitle,
                    [nameof(QAndAQuestionPage.CoreTaxonomyDXTopics)] = page.QAndAQuestionPageDXTopics?.ToArray() ?? [],
                });

                await GetBlogPostPage(page, token)
                    .Execute(p =>
                    {
                        itemData.SetValue(nameof(QAndAQuestionPage.CoreTaxonomyDXTopics), p.CoreTaxonomyDXTopics);

                        itemData.SetValue<IEnumerable<ContentItemReference>>(
                            nameof(QAndAQuestionPage.QAndAQuestionPageBlogPostPages),
                            [new ContentItemReference { Identifier = p.SystemFields.ContentItemGUID }]);
                    });

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

    private async Task<Maybe<BlogPostPage>> GetBlogPostPage(QAndAQuestionPage page, CancellationToken cancellationToken)
    {
        var webPageGUID = (page.QAndAQuestionPageBlogPostPage ?? []).Select(p => p.WebPageGuid).FirstOrDefault();

        if (webPageGUID == default)
        {
            return Maybe<BlogPostPage>.None;
        }

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.ForWebsite().OfContentType(BlogPostPage.CONTENT_TYPE_NAME))
            .Parameters(q => q.Where(w => w.WhereEquals(nameof(BlogPostPage.SystemFields.WebPageItemGUID), webPageGUID)))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await queryExecutor.GetMappedWebPageResult<BlogPostPage>(query, cancellationToken: cancellationToken);

        return pages.TryFirst();
    }
}

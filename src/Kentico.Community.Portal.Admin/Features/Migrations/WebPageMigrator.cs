using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using CMS.Websites;

namespace Kentico.Community.Portal.Admin.Features.Migrations;

public class WebPageMigrator(
    IContentQueryExecutor queryExecutor,
    ContentItemManagerCreator contentItemManagerCreator)
    : IDataMigrator
{
    public string Name => nameof(WebPageMigrator);
    public string DisplayName => "Web pages";

    public async Task<Dictionary<string, string>> MigrateableItems(CancellationToken token = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.ForWebsite().OfReusableSchema(IBasicItemFields.REUSABLE_FIELD_SCHEMA_NAME))
            .Parameters(q => q.Where(w => w.WhereEmpty(nameof(IBasicItemFields.BasicItemTitle))))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await queryExecutor.GetMappedWebPageResult<IContentItemFieldsSource>(query, cancellationToken: token);
        return pages.ToDictionary(p => p.SystemFields.ContentItemGUID.ToString(), p => p.SystemFields.ContentItemName);
    }

    public async Task<MigrationResult> Migrate(int count, CancellationToken token = default)
    {
        List<string> successes = [];
        List<string> failures = [];

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.ForWebsite().WithContentTypeFields().OfReusableSchema(IBasicItemFields.REUSABLE_FIELD_SCHEMA_NAME))
            .Parameters(q =>
            {
                _ = q
                    .Where(w => w.WhereEmpty(nameof(IBasicItemFields.BasicItemTitle)))
                    .OrderBy(nameof(IWebPageFieldsSource.SystemFields.ContentItemID));

                if (count > 0)
                {
                    _ = q.TopN(count);
                }
            })
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await queryExecutor.GetMappedWebPageResult<IContentItemFieldsSource>(query, cancellationToken: token);

        foreach (var page in pages)
        {
            try
            {
                if (page is not IWebPageFieldsSource webPageSource || page is not IWebPageMetaFields metaFields)
                {
                    failures.Add($"Could not get web page data for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                    continue;
                }
                var manager = await contentItemManagerCreator.GetWebPageItemManager(webPageSource.SystemFields.WebPageItemWebsiteChannelId);
                bool draftSuccess = await manager.TryCreateDraft(webPageSource.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
                if (!draftSuccess)
                {
                    failures.Add($"Could not create draft for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                    continue;
                }

                var (title, description) = page switch
                {
                    BlogLandingPage blp => (blp.BlogLandingPageTitle, blp.BlogLandingPageShortDescription),
                    BlogPostPage pp => (pp.WebPageMetaTitle, pp.WebPageMetaShortDescription),
                    CommunityLandingPage clp => (clp.CommunityLandingPageTitle, clp.CommunityLandingPageShortDescription),
                    CommunityLinksPage clinksp => (clinksp.WebPageMetaTitle, clinksp.WebPageMetaShortDescription),
                    HomePage hp => (hp.HomePageTitle, hp.HomePageShortDescription),
                    IntegrationsLandingPage ilp => (ilp.IntegrationsLandingPageTitle, ilp.IntegrationsLandingPageShortDescription),
                    LandingPage lp => (lp.LandingPageTitle, lp.LandingPageShortDescription),
                    QAndALandingPage qlp => (qlp.QAndALandingPageTitle, qlp.QAndALandingPageShortDescription),
                    QAndANewQuestionPage nqp => (nqp.QAndANewQuestionPageTitle, nqp.QAndANewQuestionPageShortDescription),
                    QAndAQuestionPage qp => (qp.QAndAQuestionPageTitle, qp.WebPageMetaShortDescription),
                    QAndAQuestionsRootPage qrp => (metaFields.WebPageMetaTitle, metaFields.WebPageMetaShortDescription),
                    ResourceHubPage rhp => (rhp.ResourceHubPageTitle, rhp.ShortDescription),
                    SupportPage sp => (sp.SupportPageTitle, sp.SupportPageShortDescription),
                    _ => (metaFields.WebPageMetaTitle, metaFields.WebPageMetaShortDescription)
                };
                var itemData = new ContentItemData(new Dictionary<string, object>
                {
                    [nameof(IBasicItemFields.BasicItemTitle)] = title,
                    [nameof(IBasicItemFields.BasicItemShortDescription)] = description,
                });

                bool updateSuccess = await manager.TryUpdateDraft(webPageSource.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, new(itemData), token);
                if (!updateSuccess)
                {
                    failures.Add($"Could not update draft for [{page.SystemFields.ContentItemID}] - {page.SystemFields.ContentItemName}");
                    continue;
                }

                bool publishSuccess = await manager.TryPublish(webPageSource.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, token);
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
}

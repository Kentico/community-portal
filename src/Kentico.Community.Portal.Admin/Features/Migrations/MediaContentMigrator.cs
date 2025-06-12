using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core;

namespace Kentico.Community.Portal.Admin.Features.Migrations;

public class MediaContentMigrator(
    IContentQueryExecutor queryExecutor,
    ContentItemManagerCreator contentItemManagerCreator)
    : IDataMigrator
{
    public string Name => nameof(MediaContentMigrator);
    public string DisplayName => "Media content items (file, video, image)";

    public async Task<Dictionary<string, string>> MigrateableItems(CancellationToken token = default)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .OfContentType(FileContent.CONTENT_TYPE_NAME)
                .OfContentType(ImageContent.CONTENT_TYPE_NAME)
                .OfContentType(VideoContent.CONTENT_TYPE_NAME))
            .Parameters(q => q.Where(w => w
                .WhereNull(nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics))
                .Or()
                .WhereEmpty(nameof(IBasicItemFields.BasicItemTitle))))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var items = await queryExecutor.GetMappedResult<IContentItemFieldsSource>(query, cancellationToken: token);
        return items.ToDictionary(p => p.SystemFields.ContentItemGUID.ToString(), p => p.SystemFields.ContentItemName);
    }

    public async Task<MigrationResult> Migrate(int count, CancellationToken token = default)
    {
        List<string> successes = [];
        List<string> failures = [];

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .WithContentTypeFields()
                .OfContentType(FileContent.CONTENT_TYPE_NAME)
                .OfContentType(ImageContent.CONTENT_TYPE_NAME)
                .OfContentType(VideoContent.CONTENT_TYPE_NAME))
            .Parameters(q =>
            {
                _ = q
                    .Where(w => w
                        .WhereNull(nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics))
                        .Or()
                        .WhereEmpty(nameof(IBasicItemFields.BasicItemTitle)))
                    .OrderBy(nameof(IContentItemFieldsSource.SystemFields.ContentItemID));
                if (count > 0)
                {
                    _ = q.TopN(count);
                }
            })
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var items = await queryExecutor.GetMappedResult<IContentItemFieldsSource>(query, cancellationToken: token);

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

                if (item is not IMediaItemFields mediaItem)
                {
                    failures.Add($"Item [{item.SystemFields.ContentItemID}] - {item.SystemFields.ContentItemName} is not a valid media item");
                    continue;
                }

                var itemData = new ContentItemData(new Dictionary<string, object>
                {
                    [nameof(IBasicItemFields.BasicItemTitle)] = mediaItem.MediaItemTitle,
                    [nameof(IBasicItemFields.BasicItemShortDescription)] = mediaItem.MediaItemShortDescription,
                    [nameof(ICoreTaxonomyFields.CoreTaxonomyDXTopics)] = mediaItem.MediaItemTaxonomy?.ToArray() ?? [],
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

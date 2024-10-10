using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Newtonsoft.Json;

[assembly: PageExtender(typeof(ContentTypeGeneralExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ContentTypeGeneralExtender(
    IReusableFieldSchemaManager schemaManager,
    IPageLinkGenerator linkGenerator) : PageExtender<ContentTypeGeneral>
{
    private readonly IReusableFieldSchemaManager schemaManager = schemaManager;
    private readonly IPageLinkGenerator linkGenerator = linkGenerator;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var currentClassInfo = DataClassInfoProvider.GetDataClassInfo(Page.ObjectId);

        var referencingTypes = GetReusableFieldSchemaReferences(currentClassInfo);
        referencingTypes.AddRange(await GetContentTypeReferences(currentClassInfo));

        Page.PageConfiguration.Callouts.Add(new CalloutConfiguration
        {
            Headline = "This content type is used by the following content types",
            Content = string.Join("<br >", referencingTypes.Select(i =>
            {
                var (dc, rfs) = i;
                string contentTypeURL = linkGenerator.GetPath<ContentTypeGeneral>(new()
                {
                    { typeof(ContentTypeEditSection), dc.ClassID }
                });
                string contentTypeLink = $"""<a href="/admin{contentTypeURL}">{dc.ClassDisplayName}</a>""";

                return rfs
                    .Match(
                        s =>
                        {
                            string rfsURL = linkGenerator.GetPath<ReusableFieldSchemaEdit>(new()
                            {
                                { typeof(ReusableFieldSchemaEditSection), s.Guid },
                            });
                            return $"""{contentTypeLink} ➡️ <a href="/admin{rfsURL}">{s.DisplayName}</a> reusable field schema""";
                        },
                        () => contentTypeLink
                    );
            })),
            ContentAsHtml = true,
            Type = CalloutType.QuickTip,
            Placement = CalloutPlacement.OnDesk,
        });
    }

    private static async Task<List<(DataClassInfo Class, Maybe<ReusableFieldSchema> Schema)>> GetContentTypeReferences(DataClassInfo currentClassInfo)
    {
        var contentTypeReferences = await DataClassInfoProvider.GetClasses()
            .WhereLike(nameof(DataClassInfo.ClassFormDefinition), $"%{currentClassInfo.ClassGUID}%")
            .WhereNotEquals(nameof(DataClassInfo.ClassName), ContentItemCommonDataInfo.OBJECT_TYPE)
            .GetEnumerableTypedResultAsync();

        return contentTypeReferences.Select(dc => (dc, Maybe<ReusableFieldSchema>.None)).ToList();
    }

    private List<(DataClassInfo Class, Maybe<ReusableFieldSchema> Schema)> GetReusableFieldSchemaReferences(DataClassInfo currentClassInfo)
    {
        var allRFSs = schemaManager.GetAll();
        foreach (var schema in allRFSs)
        {
            foreach (var field in schemaManager.GetSchemaFields(schema.Name))
            {
                if (!field.Settings.Contains("AllowedContentItemTypeIdentifiers") ||
                    field.Settings["AllowedContentItemTypeIdentifiers"] is not string settingsJSON ||
                    !settingsJSON.Contains(currentClassInfo.ClassGUID.ToString()))
                {
                    continue;
                }

                var classGUIDs = JsonConvert.DeserializeObject<IEnumerable<Guid>>(settingsJSON) ?? [];

                if (!classGUIDs.Contains(currentClassInfo.ClassGUID))
                {
                    continue;
                }

                return schemaManager
                    .GetContentTypesWithSchema(schema.Guid)
                    .Select(DataClassInfoProvider.GetDataClassInfo)
                    .Select(contentType => (contentType, Maybe.From(schema)))
                    .ToList();
            }
        }

        return [];
    }
}

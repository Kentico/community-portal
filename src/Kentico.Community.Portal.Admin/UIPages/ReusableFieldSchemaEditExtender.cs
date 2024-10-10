using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(ReusableFieldSchemaEditExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ReusableFieldSchemaEditExtender(
    IReusableFieldSchemaManager schemaManager,
    IPageLinkGenerator pageLinkGenerator) : PageExtender<ReusableFieldSchemaEdit>
{
    private readonly IReusableFieldSchemaManager schemaManager = schemaManager;
    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var contentTypes = schemaManager.GetContentTypesWithSchema(Page.SchemaGuid);

        Page.PageConfiguration.Callouts.Add(new CalloutConfiguration
        {
            Headline = "This schema is used by the following content types",
            Content = string.Join("<br >", contentTypes.Select(c =>
            {
                var dc = DataClassInfoProvider.GetDataClassInfo(c);
                string url = pageLinkGenerator.GetPath<ContentTypeGeneral>(new()
                {
                    { typeof(ContentTypeEditSection), dc.ClassID },
                });
                return $"""<a href="/admin{url}">{dc.ClassDisplayName}</a>""";
            })),
            ContentAsHtml = true,
            Type = CalloutType.QuickTip,
            Placement = CalloutPlacement.OnDesk,
        });
    }
}

using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Microsoft.AspNetCore.Http;

[assembly: PageExtender(typeof(ReusableFieldSchemaEditExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ReusableFieldSchemaEditExtender(
    IReusableFieldSchemaManager schemaManager,
    IHttpContextAccessor contextAccessor,
    IPageUrlGenerator urlGenerator) : PageExtender<ReusableFieldSchemaEdit>
{
    private readonly IReusableFieldSchemaManager schemaManager = schemaManager;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly IPageUrlGenerator urlGenerator = urlGenerator;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var context = contextAccessor.HttpContext!;

        var contentTypes = schemaManager.GetContentTypesWithSchema(Page.SchemaGuid);

        Page.PageConfiguration.Callouts.Add(new CalloutConfiguration
        {
            Headline = "This schema is used by the following content types",
            Content = string.Join("<br >", contentTypes.Select(c =>
            {
                var dc = DataClassInfoProvider.GetDataClassInfo(c);
                string url = urlGenerator.GenerateUrl(typeof(ContentTypeGeneral), dc.ClassID.ToString());
                return $"""<a href="/admin{url}">{c}</a>""";
            })),
            ContentAsHtml = true,
            Type = CalloutType.QuickTip,
            Placement = CalloutPlacement.OnDesk,
        });
    }
}

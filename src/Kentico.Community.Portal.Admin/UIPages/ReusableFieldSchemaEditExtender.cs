using System.Text.RegularExpressions;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Helpers;
using CSharpFunctionalExtensions;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Microsoft.AspNetCore.Http;

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

        /*
         * Yup, this is a hack, but I don't know how to get the page's bound value from the URL 🤷
         */
        if (!URLGUIDExtractor.TryExtractGUID(context.Request.Form["path"].FirstOrDefault() ?? "", out var schemaGUID))
        {
            return;
        }

        var contentTypes = schemaManager.GetContentTypesWithSchema(schemaGUID);

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

public static partial class URLGUIDExtractor
{
    [GeneratedRegex(
        @"\b[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-\b[a-fA-F0-9]{12}\b",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex MatchIfGUID();
    public static bool TryExtractGUID(string url, out Guid schemaGUID)
    {
        var match = MatchIfGUID().Match(url);

        if (match.Success)
        {
            schemaGUID = ValidationHelper.GetGuid(match.Value, default);
            return true;
        }

        schemaGUID = default;
        return false;
    }
}

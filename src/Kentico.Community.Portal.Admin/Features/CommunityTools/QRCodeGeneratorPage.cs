using CMS.ContentEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.CommunityTools;
using Kentico.Community.Portal.Core.Content;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(QRCodeGeneratorPage),
    parentType: typeof(CommunityToolsApplicationPage),
    slug: "qr-code-generator",
    name: "QR Code Generator",
    templateName: "@kentico-community/portal-web-admin/QRCodeGenerator",
    order: 1,
    Icon = Icons.QrCode)]

namespace Kentico.Community.Portal.Admin.Features.CommunityTools;

[UIPermission(SystemPermissions.VIEW)]
public class QRCodeGeneratorPage(IContentQueryExecutor contentQueryExecutor) : Page<QRCodeGeneratorPageClientProperties>
{
    // The Kentico circle logo used as the QR code center image
    private const string KENTICO_LOGO_CODE_NAME = "KenticoLogo_shortcutColorPositive-31i3t58n";

    private readonly IContentQueryExecutor contentQueryExecutor = contentQueryExecutor;

    public override async Task<QRCodeGeneratorPageClientProperties> ConfigureTemplateProperties(QRCodeGeneratorPageClientProperties properties)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .OfContentType(ImageContent.CONTENT_TYPE_NAME)
                .WithContentTypeFields())
            .Parameters(q => q
                .Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemName), KENTICO_LOGO_CODE_NAME)));

        var images = await contentQueryExecutor.GetMappedResult<ImageContent>(b, new ContentQueryExecutionOptions
        {
            ForPreview = false,
            IncludeSecuredItems = false
        });

        if (images.FirstOrDefault() is ImageContent logo && logo.ImageContentAsset is { } asset)
        {
            properties.LogoUrl = asset.Url.TrimStart('~');
        }

        return properties;
    }
}

public class QRCodeGeneratorPageClientProperties : TemplateClientProperties
{
    public string LogoUrl { get; set; } = "";
}

using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.WebsiteChannels;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: UIApplication(
    identifier: WebsiteChannelsApplicationPage.IDENTIFIER,
    type: typeof(WebsiteChannelsApplicationPage),
    slug: "website-channels",
    name: "Website channels",
    category: BaseApplicationCategories.CONTENT_MANAGEMENT,
    icon: Icons.Earth,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.WebsiteChannels;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class WebsiteChannelsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "website-channels-app";
}

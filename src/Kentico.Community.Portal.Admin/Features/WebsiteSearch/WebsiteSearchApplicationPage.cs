using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.WebsiteSearch;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: UIApplication(
    identifier: WebsiteSearchApplicationPage.IDENTIFIER,
    type: typeof(WebsiteSearchApplicationPage),
    slug: "website-search",
    name: "Website search",
    category: BaseApplicationCategories.CONTENT_MANAGEMENT,
    icon: Icons.Earth,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.WebsiteSearch;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class WebsiteSearchApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "website-search-app";
}

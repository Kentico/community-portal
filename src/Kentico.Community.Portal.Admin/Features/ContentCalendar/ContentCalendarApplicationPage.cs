using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.ContentCalendar;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: UIApplication(
    identifier: ContentCalendarApplicationPage.IDENTIFIER,
    type: typeof(ContentCalendarApplicationPage),
    slug: "content-calendar",
    name: "Content calendar",
    category: BaseApplicationCategories.CONTENT_MANAGEMENT,
    icon: Icons.Calendar,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.ContentCalendar;

[UIPermission(SystemPermissions.VIEW)]
public class ContentCalendarApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "content-calendar-app";
}

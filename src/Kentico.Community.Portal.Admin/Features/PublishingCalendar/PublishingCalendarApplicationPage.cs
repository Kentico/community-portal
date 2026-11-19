using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.PublishingCalendar;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: UIApplication(
    identifier: PublishingCalendarApplicationPage.IDENTIFIER,
    type: typeof(PublishingCalendarApplicationPage),
    slug: "publishing-calendar",
    name: "Publishing calendar",
    category: BaseApplicationCategories.CONTENT_MANAGEMENT,
    icon: Icons.Calendar,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.PublishingCalendar;

[UIPermission(SystemPermissions.VIEW)]
public class PublishingCalendarApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "publishing-calendar-app";
}

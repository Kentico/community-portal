using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: PageExtender(typeof(WebPageLayoutExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

/// <summary>
/// Extends the <see cref="WebPageLayout"/> to:
/// - Remove the Rocket Surgery tab from the navigation on the root page (WebPageItemID == 0)
/// - Remove the Rocket Surgery tab when the user lacks the VIEW permission
/// </summary>
[UIPermission(
    KenticoCommunityPermissions.ROCKET_SURGERY.VIEW.Name,
    KenticoCommunityPermissions.ROCKET_SURGERY.VIEW.DisplayName)]
[UIPermission(
    KenticoCommunityPermissions.ROCKET_SURGERY.EDIT.Name,
    KenticoCommunityPermissions.ROCKET_SURGERY.EDIT.DisplayName)]
public class WebPageLayoutExtender(IUIPermissionEvaluator permissionEvaluator)
    : PageExtender<WebPageLayout>
{
    private readonly IUIPermissionEvaluator permissionEvaluator = permissionEvaluator;

    public override async Task<TemplateClientProperties> ConfigureTemplateProperties(TemplateClientProperties properties)
    {
        var props = await base.ConfigureTemplateProperties(properties);

        if (props.Navigation?.Items is not List<NavigationItem> items)
        {
            return props;
        }

        bool isRootPage = Page.WebPageIdentifier.WebPageItemID == 0;
        if (isRootPage)
        {
            items.RemoveAll((NavigationItem i) =>
                string.Equals("rocket-surgery", i.Path, StringComparison.OrdinalIgnoreCase));

            return props;
        }

        var canView = await permissionEvaluator.Evaluate(KenticoCommunityPermissions.ROCKET_SURGERY.VIEW.Name);
        if (!canView.Succeeded)
        {
            items.RemoveAll((NavigationItem i) =>
                string.Equals("rocket-surgery", i.Path, StringComparison.OrdinalIgnoreCase));
        }

        return props;
    }
}

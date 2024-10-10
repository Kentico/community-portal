using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages.Internal;

[assembly: PageExtender(typeof(AutomationProcessContactsTabExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class AutomationProcessContactsTabExtender : PageExtender<AutomationProcessContactsTab>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        Page.PageConfiguration.FilterFormModel = new AutomationStepListFilter();
    }
}

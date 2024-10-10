using CMS.Activities;
using CMS.ContentEngine;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(ActivityListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ActivityListExtender : PageExtender<ActivityList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        Page.PageConfiguration.QueryModifiers.AddModifier((query, s) =>
            query
                .AddColumn(nameof(ActivityTypeInfo.ActivityTypeName))
                .AddColumn(nameof(ChannelInfo.ChannelName)));

        Page.PageConfiguration.FilterFormModel = new ActivityListFilter();
    }
}

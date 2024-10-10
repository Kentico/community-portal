using CMS.Activities;
using CMS.ContentEngine;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(ContactActivityListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ContactActivityListExtender : PageExtender<ContactActivityList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        Page.PageConfiguration.QueryModifiers.AddModifier((query, s) =>
            query
                .Source(s => s.LeftJoin<ChannelInfo>($"OM_Activity.{nameof(ActivityInfo.ActivityChannelID)}", nameof(ChannelInfo.ChannelID)))
                .AddColumn(nameof(ActivityTypeInfo.ActivityTypeName))
                .AddColumn(nameof(ChannelInfo.ChannelName)));

        Page.PageConfiguration.FilterFormModel = new ActivityListFilter();
    }
}

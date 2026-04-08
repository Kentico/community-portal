using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.ScheduledTasks;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages.Internal;

[assembly: PageExtender(typeof(ScheduledTaskConfigurationListExtender))]

namespace Kentico.Community.Portal.Admin.Features.ScheduledTasks;

public class ScheduledTaskConfigurationListExtender : PageExtender<ScheduledTaskConfigurationList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var configs = Page.PageConfiguration.ColumnConfigurations;

        configs
            .TryFirst(c => string.Equals(c.Name, "ScheduledTaskConfigurationDisplayName", StringComparison.OrdinalIgnoreCase))
            .Tap(c => c.Searchable = true);

        configs
            .TryFirst(c => string.Equals(c.Name, "ScheduledTaskConfigurationName", StringComparison.OrdinalIgnoreCase))
            .Tap(c => c.Searchable = true);

        configs
            .TryFirst(c => string.Equals(c.Name, "ScheduledTaskConfigurationEnabled", StringComparison.OrdinalIgnoreCase))
            .Tap(c =>
            {
                c.Visible = true;
                c.Caption = "Enabled";
            });
    }
}

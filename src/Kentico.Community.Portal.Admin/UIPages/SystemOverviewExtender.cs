using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(SystemOverviewExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class SystemOverviewExtender(ApplicationAssemblyInformation assemblyInformation)
    : PageExtender<SystemOverview>
{
    private readonly ApplicationAssemblyInformation assemblyInformation = assemblyInformation;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Look at the decompiled source of the "SystemOverview" page to understand the assumptions we're making about the UI here
        Page.PageConfiguration.CardGroups
            .TryFirst()
            .Bind(group => group.Cards.TryFirst())
            .Execute(c =>
            {
                c.Components.Add(new FormCardComponent
                {
                    Items =
                    [
                        CreateFormItem("Application assembly version", assemblyInformation.Version),
                        CreateFormItem("Git hash", assemblyInformation.GitHash),
                    ]
                });
            });
    }

    private static TextWithLabelClientProperties CreateFormItem(string headline, string text) =>
        new()
        {
            Name = headline,
            Label = headline,
            Value = text,
            ComponentName = "@kentico/xperience-admin-base/TextWithLabel"
        };
}


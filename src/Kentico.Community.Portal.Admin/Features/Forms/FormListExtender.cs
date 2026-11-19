using CMS.DataEngine;
using CMS.OnlineForms;
using Kentico.Community.Portal.Admin.Features.Forms;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(FormListExtender))]

namespace Kentico.Community.Portal.Admin.Features.Forms;

public class FormListExtender(IInfoProvider<FormRetentionPolicyInfo> policyProvider) : PageExtender<FormList>
{
    private readonly IInfoProvider<FormRetentionPolicyInfo> policyProvider = policyProvider;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = Page.PageConfiguration.ColumnConfigurations.AddColumn(
            nameof(BizFormInfo.FormLastModified),
            caption: "Date Modified",
            defaultSortDirection: SortTypeEnum.Desc);

        var policies = (await policyProvider.Get().GetEnumerableTypedResultAsync())
            .GroupBy(p => p.FormRetentionPolicyFormID)
            .ToDictionary(g => g.Key, g => g.First());

        _ = Page.PageConfiguration.ColumnConfigurations.AddColumn(
            nameof(BizFormInfo.FormID),
            caption: "Retention",
            sortable: false,
            formatter: (value, _) => Describe(policies.GetValueOrDefault(value is int id ? id : 0)));
    }

    private static string Describe(FormRetentionPolicyInfo? policy) => policy switch
    {
        null => FormRetentionModel.UNCLASSIFIED,
        { FormRetentionPolicyType: FormRetentionPolicyTypes.SHORT_LIVED, FormRetentionPolicyRetentionDays: > 0 } p =>
            $"Short-lived ({p.FormRetentionPolicyRetentionDays} days)",
        { FormRetentionPolicyType: FormRetentionPolicyTypes.SHORT_LIVED } => "Short-lived (default)",
        _ => policy.FormRetentionPolicyType,
    };
}

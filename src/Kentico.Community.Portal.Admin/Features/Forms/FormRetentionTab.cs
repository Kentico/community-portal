using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.Forms;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(FormEditSection),
    slug: "retention",
    uiPageType: typeof(FormRetentionTab),
    name: "Retention",
    templateName: TemplateNames.EDIT,
    order: 900)]

namespace Kentico.Community.Portal.Admin.Features.Forms;

/// <summary>
/// Sets how long the form's submissions are kept. Unclassified forms are kept and the
/// form retention task logs a warning for each.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class FormRetentionTab(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<FormRetentionPolicyInfo> policyProvider)
    : ModelEditPage<FormRetentionModel>(formItemCollectionProvider, formDataBinder)
{
    private readonly IInfoProvider<FormRetentionPolicyInfo> policyProvider = policyProvider;

    private FormRetentionModel model = new();

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override FormRetentionModel Model => model;

    protected override async Task<ICollection<IFormItem>> GetFormItems()
    {
        model = FormRetentionModel.From(await GetPolicy());

        return await base.GetFormItems();
    }

    protected override async Task<ICommandResponse> ProcessFormData(FormRetentionModel model, ICollection<IFormItem> formItems)
    {
        var policy = await GetPolicy();

        if (model.Policy == FormRetentionModel.UNCLASSIFIED)
        {
            if (policy is not null)
            {
                await policyProvider.DeleteAsync(policy);
            }
        }
        else
        {
            policy ??= new FormRetentionPolicyInfo
            {
                FormRetentionPolicyGUID = Guid.NewGuid(),
                FormRetentionPolicyFormID = ObjectId,
            };
            policy.FormRetentionPolicyType = model.Policy;
            policy.FormRetentionPolicyRetentionDays = model.Policy == FormRetentionPolicyTypes.SHORT_LIVED ? model.RetentionDays : 0;

            await policyProvider.SetAsync(policy);
        }

        return ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess)
        {
            Items = await (await GetFormItems()).OnlyVisible().GetClientProperties(),
        })
        .AddSuccessMessage("Retention policy saved.");
    }

    private async Task<FormRetentionPolicyInfo?> GetPolicy() =>
        (await policyProvider.Get()
            .WhereEquals(nameof(FormRetentionPolicyInfo.FormRetentionPolicyFormID), ObjectId)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();
}

public class FormRetentionModel
{
    public const string UNCLASSIFIED = "Unclassified";

    [RadioGroupComponent(
        Label = "Retention policy",
        ExplanationText = "History: kept while the portal runs (polls, program activity); erasure removes the email only. Short-lived: deleted after the retention days; erasure deletes the row. Unclassified: kept, and the nightly retention task logs a warning.",
        Options = $"{UNCLASSIFIED};Unclassified\r\n{FormRetentionPolicyTypes.HISTORY};History\r\n{FormRetentionPolicyTypes.SHORT_LIVED};Short-lived",
        Order = 0)]
    public string Policy { get; set; } = UNCLASSIFIED;

    [NumberInputComponent(
        Label = "Retention days",
        ExplanationText = "Short-lived only. 0 uses the default (RetentionOptions:ShortLivedFormDefaultDays, 3 years).",
        Order = 1)]
    [VisibleIfEqualTo(nameof(Policy), FormRetentionPolicyTypes.SHORT_LIVED)]
    [MinimumIntegerValueValidationRule(0)]
    public int RetentionDays { get; set; }

    public static FormRetentionModel From(FormRetentionPolicyInfo? policy) => new()
    {
        Policy = policy?.FormRetentionPolicyType ?? UNCLASSIFIED,
        RetentionDays = policy?.FormRetentionPolicyRetentionDays ?? 0,
    };
}

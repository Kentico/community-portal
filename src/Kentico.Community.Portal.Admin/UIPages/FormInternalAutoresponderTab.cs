using CMS.DataEngine;
using CMS.Membership;
using CMS.OnlineForms;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(FormBuilderTab),
    slug: "internal-autoresponder",
    uiPageType: typeof(FormInternalAutoresponderTab),
    name: "Internal Autoresponder",
    templateName: TemplateNames.EDIT,
    order: 201, // After Autoresponder tab
    icon: Icons.Messages)]

namespace Kentico.Community.Portal.Admin.UIPages;

/// <summary>
/// Custom internal autoresponder tab of the form edit page.
/// </summary>
[UIPageLocation(PageLocationEnum.SidePanel)]
public sealed class FormInternalAutoresponderTab(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<BizFormInfo> bizFormProvider,
    IInfoProvider<BizFormSettingsInfo> bizFormSettingsProvider,
    IInfoProvider<UserInfo> userProvider,
    ISystemClock clock)
    : ModelEditPage<FormInternalAutoresponderPropertiesModel>(formItemCollectionProvider, formDataBinder)
{
    private BizFormInfo? mEditedForm;
    private FormInternalAutoresponderPropertiesModel? mModel;
    private readonly IInfoProvider<BizFormInfo> bizFormProvider = bizFormProvider;
    private readonly IInfoProvider<BizFormSettingsInfo> bizFormSettingsProvider = bizFormSettingsProvider;
    private readonly IInfoProvider<UserInfo> userProvider = userProvider;
    private readonly ISystemClock clock = clock;

    private BizFormInfo EditedForm => mEditedForm ??= bizFormProvider.Get(ObjectId);

    /// <summary>
    /// Gets edited model that represents the form.
    /// </summary>
    protected override FormInternalAutoresponderPropertiesModel Model
    {
        get
        {
            mModel ??= new FormInternalAutoresponderPropertiesModel
            {
                IsInternalAutoresponderEnabled = false
            };

            var formSettings = bizFormSettingsProvider.Get()
                .WhereEquals(nameof(BizFormSettingsInfo.BizFormSettingsBizFormID), EditedForm.FormID)
                .GetEnumerableTypedResult()
                .FirstOrDefault();

            if (formSettings is not null)
            {
                mModel.IsInternalAutoresponderEnabled = formSettings.BizFormSettingsInternalAutoresponderIsEnabled;

                string username = userProvider.Get()
                    .WhereEquals(nameof(UserInfo.UserID), formSettings.BizFormSettingsInternalAutoresponderRecipientUserID)
                    .Column(nameof(UserInfo.UserName))
                    .GetScalarResult<string>();

                mModel.Users = [new() { ObjectCodeName = username }];
            }

            return mModel;
        }
    }


    /// <summary>
    /// ID of the form.
    /// </summary>
    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override async Task<ICommandResponse> ProcessFormData(FormInternalAutoresponderPropertiesModel model, ICollection<IFormItem> formItems)
    {
        var formValidationResult = EditedForm.Generalized.Validate();
        if (!formValidationResult.IsValid)
        {
            var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure)
            {
                Items = await GetClientFormItems(formItems)
            });
            return response.AddErrorMessage(LocalizationService?.GetString("base.forms.error.validationerror"));
        }

        EditedForm.Generalized.SetObject();

        var formSettings = await bizFormSettingsProvider.Get()
            .WhereEquals(nameof(BizFormSettingsInfo.BizFormSettingsBizFormID), EditedForm.FormID)
            .FirstOrDefaultAsync();

        formSettings ??= new()
        {
            BizFormSettingsBizFormID = EditedForm.FormID
        };

        formSettings.BizFormSettingsInternalAutoresponderIsEnabled = model.IsInternalAutoresponderEnabled;
        formSettings.BizFormSettingsInternalAutoresponderRecipientUserID = await GetRecipientUserID(model);
        if (formSettings.BizFormSettingsID == 0)
        {
            formSettings.BizFormSettingsDateCreated = clock.Now;
        }
        bizFormSettingsProvider.Set(formSettings);

        return await base.ProcessFormData(model, formItems);
    }

    private async Task<int> GetRecipientUserID(FormInternalAutoresponderPropertiesModel model)
    {
        string? userCodename = model.Users
            .Select(u => u.ObjectCodeName)
            .FirstOrDefault();

        return await userProvider.Get()
            .WhereEquals(nameof(UserInfo.UserName), userCodename)
            .Column(nameof(UserInfo.UserID))
            .GetScalarResultAsync(0);
    }

    public override Task ConfigurePage()
    {
        PageConfiguration.Headline = "Internal Autoresponder";

        return base.ConfigurePage();
    }

    protected override Task<string> GetObjectDisplayName(FormInternalAutoresponderPropertiesModel model) =>
        Task.FromResult(EditedForm.FormDisplayName);
}

[FormCategory(Label = "Internal Autoresponder settings", Order = 5)]
public class FormInternalAutoresponderPropertiesModel
{
    [CheckBoxComponent(
        Label = "Send internal autoresponder?",
        ExplanationText = "If true, an autoresponder is sent to the email address of the user selected below",
        Order = 1)]
    public bool IsInternalAutoresponderEnabled { get; set; } = false;

    [ObjectSelectorComponent(
        UserInfo.OBJECT_TYPE,
        Label = "Recipient",
        ExplanationText = "Internal user recipient of autoresponder",
        MaximumItems = 1,
        WhereConditionProviderType = typeof(UserAutoresponderRecipientWhereConditionProvider),
        OrderBy = [$"{nameof(UserInfo.Email)} DESC"],
        Order = 2)]
    [VisibleIfTrue(nameof(IsInternalAutoresponderEnabled))]
    public IEnumerable<ObjectRelatedItem> Users { get; set; } = [];
}

public class UserAutoresponderRecipientWhereConditionProvider : IObjectSelectorWhereConditionProvider
{
    public WhereCondition Get() => new WhereCondition().WhereTrue(nameof(UserInfo.UserEnabled));
}

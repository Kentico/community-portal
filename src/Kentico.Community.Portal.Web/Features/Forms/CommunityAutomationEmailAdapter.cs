using CMS;
using CMS.Automation;
using CMS.Automation.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.OnlineForms;
using Kentico.Community.Portal.Web.Features.Forms;

[assembly: RegisterImplementation(
    implementedType: typeof(IAutomationEmailAdapter),
    implementation: typeof(CommunityAutomationEmailAdapter),
    Priority = RegistrationPriority.Default)]

namespace Kentico.Community.Portal.Web.Features.Forms;

public class CommunityAutomationEmailAdapter(
    IInfoProvider<BizFormInfo> bizFormProvider,
    IAutomationEmailAdapter defaultAutomationEmailAdapater,
    FormInternalAutoresponderEmailSender internalAutoresponderEmailSender) : IAutomationEmailAdapter
{
    private readonly IInfoProvider<BizFormInfo> bizFormProvider = bizFormProvider;
    private readonly IAutomationEmailAdapter defaultAutomationEmailAdapater = defaultAutomationEmailAdapater;
    private readonly FormInternalAutoresponderEmailSender internalAutoresponderEmailSender = internalAutoresponderEmailSender;

    public async Task<AutomationEmail> GetCodeBasedEmail(ContainerCustomData stateCustomData, BaseInfo contactInfo)
    {
        await SendInternalAutoresponder(stateCustomData);

        return await defaultAutomationEmailAdapater.GetCodeBasedEmail(stateCustomData, contactInfo);
    }

    public async Task<AutomationEmail> GetEmailFromLibrary(Guid emailGuid, ContainerCustomData stateCustomData, BaseInfo contactInfo)
    {
        await SendInternalAutoresponder(stateCustomData);

        return await defaultAutomationEmailAdapater.GetEmailFromLibrary(emailGuid, stateCustomData, contactInfo);
    }

    private async Task SendInternalAutoresponder(ContainerCustomData stateCustomData)
    {
        if (stateCustomData[TriggerDataConstants.TRIGGER_DATA_BIZFORM_ITEM_ID] is int formItemID
            && stateCustomData[TriggerDataConstants.TRIGGER_DATA_BIZFORM_ID] is int formID)
        {
            var form = await bizFormProvider.GetAsync(formID);

            string className = DataClassInfoProvider.GetClassName(form.FormClassID);
            var bizFormItem = BizFormItemProvider.GetItem(formItemID, className);

            await internalAutoresponderEmailSender.SendEmail(bizFormItem, form);
        }
    }
}

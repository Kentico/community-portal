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
    IAutomationEmailAdapter defaultAutomationEmailAdapater,
    FormInternalAutoresponderEmailSender internalAutoresponderEmailSender) : IAutomationEmailAdapter
{
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
        if (stateCustomData[TriggerDataConstants.TRIGGER_DATA_FORMDATA] is BizFormItem bizFormItem
            && stateCustomData[TriggerDataConstants.TRIGGER_DATA_BIZFORM] is BizFormInfo form)
        {
            await internalAutoresponderEmailSender.SendEmail(bizFormItem, form);
        }
    }
}

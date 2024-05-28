using CMS.ContactManagement;
using CMS.EmailMarketing;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(RecipientListSubscriberListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class RecipientListSubscriberListExtender : PageExtender<RecipientListSubscriberList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var config = Page.PageConfiguration;

        _ = config.QueryModifiers
            .AddModifier(q =>
                q.Source(s =>
                    s.InnerJoin<EmailSubscriptionConfirmationInfo>(
                        $"OM_ContactGroupMember.{nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID)}",
                        nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID)))
                .AddColumn(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate)));

        _ = config.ColumnConfigurations.AddColumn(
            nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate),
            caption: "Confirmed On");
    }
}

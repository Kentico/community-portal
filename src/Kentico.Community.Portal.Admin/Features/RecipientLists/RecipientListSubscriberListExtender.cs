using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailMarketing;
using CMS.Membership;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.RecipientLists;
using Kentico.Community.Portal.Core.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(RecipientListSubscriberListExtender))]

namespace Kentico.Community.Portal.Admin.Features.RecipientLists;

public class RecipientListSubscriberListExtender(
    RecipientListManager recipientListManager,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider,
    IInfoProvider<ContactInfo> contactProvider) : PageExtender<RecipientListSubscriberList>
{
    private readonly RecipientListManager recipientListManager = recipientListManager;

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
            caption: "Confirmed On", defaultSortDirection: SortTypeEnum.Desc);

        config.ColumnConfigurations
            .TryFirst(c => string.Equals(c.Name, nameof(ContactInfo.ContactLastName), StringComparison.OrdinalIgnoreCase))
            .Tap(c => c.Sorting = new() { Sortable = c.Sorting.Sortable });

        _ = config.TableActions.AddDeleteAction(nameof(Delete));
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse<RowActionResult>> Delete(int contactGroupMemberID)
    {
        var groupMember = await contactGroupMemberProvider.GetAsync(contactGroupMemberID);
        var contact = await contactProvider.GetAsync(groupMember.ContactGroupMemberRelatedID);
        await recipientListManager.Unsubscribe(contact.ContactEmail, Page.ObjectId);
        return ResponseFrom(new RowActionResult(true));
    }
}

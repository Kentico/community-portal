using CMS.ContactManagement;
using CMS.EmailMarketing;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.RecipientLists;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(RecipientListSubscriberListExtender))]

namespace Kentico.Community.Portal.Admin.Features.RecipientLists;

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
            caption: "Confirmed On", defaultSortDirection: SortTypeEnum.Desc);

        config.ColumnConfigurations
            .TryFirst(c => string.Equals(c.Name, nameof(ContactInfo.ContactLastName), StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.Sorting = new() { Sortable = c.Sorting.Sortable });
    }
}

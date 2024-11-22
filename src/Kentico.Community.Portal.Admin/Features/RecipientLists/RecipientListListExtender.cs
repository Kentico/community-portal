using CMS.ContactManagement;
using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.RecipientLists;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(RecipientListListExtender))]

namespace Kentico.Community.Portal.Admin.Features.RecipientLists;

public class RecipientListListExtender : PageExtender<RecipientListList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        Page.PageConfiguration.QueryModifiers.Add(new QueryModifier((q, s) =>
        {
            var aggregate =
                new ObjectQuery(ContactGroupMemberInfo.OBJECT_TYPE_CONTACT)
                    .Column(new AggregatedColumn(AggregationType.Count, nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID)))
                    .Where($"{nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID)} = {nameof(ContactGroupInfo.ContactGroupID)}")
                    .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), 0);

            return q
                .Columns(
                    // We have to specify an explicit column list because .AddColumn() won't expand the aggregate into a subquery
                    new QueryColumn(nameof(ContactGroupInfo.ContactGroupID)),
                    new QueryColumn(nameof(ContactGroupInfo.ContactGroupDisplayName)),
                    aggregate.AsColumn("ContactsCount"));
        }));

        var configs = Page.PageConfiguration.ColumnConfigurations
            .AddColumn("ContactsCount", caption: "Count");
    }
}


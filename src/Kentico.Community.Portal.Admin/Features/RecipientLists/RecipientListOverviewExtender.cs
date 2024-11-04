using System.Globalization;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailMarketing;
using CMS.Helpers;
using CsvHelper;
using Kentico.Community.Portal.Admin.Features.RecipientLists;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;
using Action = Kentico.Xperience.Admin.Base.Action;

[assembly: PageExtender(typeof(RecipientListOverviewExtender))]

namespace Kentico.Community.Portal.Admin.Features.RecipientLists;

public class RecipientListOverviewExtender(
    IInfoProvider<ContactGroupMemberInfo> groupMemberProvider,
    IInfoProvider<ContactGroupInfo> contactGroupProvider) : PageExtender<RecipientListOverview>
{
    private readonly IInfoProvider<ContactGroupMemberInfo> groupMemberProvider = groupMemberProvider;
    private readonly IInfoProvider<ContactGroupInfo> contactGroupProvider = contactGroupProvider;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var recipientList = await contactGroupProvider.GetAsync(Page.ObjectId);

        var component = new DataExportComponent()
        {
            Properties = new DataExportProperties
            {
                FileNamePrefix = recipientList.ContactGroupName
            }
        };

        var card = new OverviewCard
        {
            Headline = "Export",
            Identifier = "export",
            Actions =
            [
                new Action(ActionType.CustomComponent)
                {
                    ButtonColor = ButtonColor.Secondary,
                    Icon = Icons.ArrowDown,
                    Title = "Export data",
                    Label = "Export",
                    Destructive = false,
                    Identifier = "export",
                    ComponentProperties = await component.GetClientProperties(),
                }
            ],
            Components = []
        };
        var group = new OverviewCardGroup
        {
            Cards = [card]
        };

        Page.PageConfiguration.CardGroups
            .Add(group);
    }

    [PageCommand(CommandName = "EXPORT_LIST")]
    public async Task<ICommandResponse> PageCommandHandler(CancellationToken cancellationToken = default)
    {
        var results = await groupMemberProvider.Get()
            .Source(s => s
                .InnerJoin<ContactInfo>("ContactGroupMemberRelatedID", "ContactID")
                .LeftJoin<EmailBounceInfo>("OM_Contact.ContactEmail", "EmailBounceEmailAddress")
                .InnerJoin<EmailSubscriptionConfirmationInfo>(
                    $"OM_ContactGroupMember.{nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID)}",
                    nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID)))
            .Where("ContactGroupMemberContactGroupID", QueryOperator.Equals, Page.ObjectId)
            .GetDataContainerResultAsync(cancellationToken: cancellationToken);

        var items = new List<Item>();

        foreach (var r in results)
        {
            int contactID = ValidationHelper.GetInteger(r.GetValue(nameof(ContactInfo.ContactID)), 0);
            string email = ValidationHelper.GetString(r.GetValue(nameof(ContactInfo.ContactEmail)), "");
            var confirmedDate = ValidationHelper.GetDateTime(r.GetValue(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate)), default);

            items.Add(new Item(contactID, email, confirmedDate));
        }

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(items);
        writer.Flush();
        ms.Position = 0;

        return ResponseFrom(new { file = Convert.ToBase64String(ms.ToArray()) });
    }

    public override async Task<TemplateClientProperties> ConfigureTemplateProperties(TemplateClientProperties properties)
    {
        var props = await base.ConfigureTemplateProperties(properties);

        return props;
    }
}

public class DataExportComponent : ActionComponent<DataExportProperties, DataExportClientProperties>
{
    public override string ClientComponentName => "@kentico-community/portal-web-admin/DataExport";

    protected override Task ConfigureClientProperties(DataExportClientProperties clientProperties)
    {
        clientProperties.FileNamePrefix = Properties.FileNamePrefix;

        return base.ConfigureClientProperties(clientProperties);
    }
}
public class DataExportProperties : IActionComponentProperties
{
    public string FileNamePrefix { get; set; } = "";
}
public class DataExportClientProperties : IActionComponentClientProperties
{
    public string ComponentName { get; init; } = "";

    public string FileNamePrefix { get; set; } = "";
}

public record Item(int ContactID, string ContactEmail, DateTime ConfirmedDate);

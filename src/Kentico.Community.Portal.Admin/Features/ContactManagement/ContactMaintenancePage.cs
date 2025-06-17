using System.Data;
using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using Kentico.Community.Portal.Admin.Features.ContactManagement;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: UIPage(
    uiPageType: typeof(ContactMaintenancePage),
    parentType: typeof(ContactManagementApplication),
    slug: "maintenance",
    name: "Maintenance",
    templateName: "@kentico-community/portal-web-admin/ContactMaintenanceLayout",
    order: 1000,
    Icon = Icons.Graph)]

namespace Kentico.Community.Portal.Admin.Features.ContactManagement;

public class ContactMaintenancePage(
    TimeProvider clock,
    IInfoProvider<ContactInfo> contactProvider,
    IInfoProvider<ActivityInfo> activityProvider) : Page<ContactMaintenancePageProperties>
{
    public const string IDENTIFIER = "contact-maintenance";

    /// <summary>
    /// Prevent accidental deletion of contacts/activities newer than 1 year old
    /// </summary>
    public const int DELETE_MONTH_LIMIT = -6;

    private readonly TimeProvider clock = clock;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly IInfoProvider<ActivityInfo> activityProvider = activityProvider;

    public override async Task<ContactMaintenancePageProperties> ConfigureTemplateProperties(ContactMaintenancePageProperties properties)
    {
        int contactCount = await contactProvider.Get()
            .Column("ContactID")
            .GetCountAsync();

        int activityCount = await activityProvider.Get()
            .Column("ActivityID")
            .GetCountAsync();

        properties.TotalContacts = contactCount;
        properties.TotalActivities = activityCount;

        return properties;
    }

    [PageCommand(CommandName = "REFRESH_COUNTS")]
    public async Task<ICommandResponse> RefreshCounts()
    {
        int totalContacts = await contactProvider.Get()
            .Column("ContactID")
            .GetCountAsync();

        int totalActivities = await activityProvider.Get()
            .Column("ActivityID")
            .GetCountAsync();

        return ResponseFrom(new { totalContacts, totalActivities });
    }

    [PageCommand(CommandName = "DELETE_CONTACTS")]
    public async Task<ICommandResponse> DeleteContacts(DeleteContactCommand command)
    {
        string query = $"""
        DECLARE @TotalContactsDeleted INT = 0;
        DECLARE @BatchContactsDeleted INT;

        DECLARE @TotalActivitiesDeleted INT = 0;
        DECLARE @BatchActivitiesDeleted INT;

        WHILE 1 = 1
        BEGIN
            DELETE v
            FROM OM_VisitorToContact v
            INNER JOIN OM_Contact c
                ON v.VisitorToContactContactID = c.ContactID
            LEFT JOIN OM_Activity a
                ON c.ContactID = a.ActivityContactID
            LEFT JOIN EmailLibrary_EmailSubscriptionConfirmation esc
                ON c.ContactID = esc.EmailSubscriptionConfirmationContactID
            LEFT JOIN CMS_ConsentAgreement ca
                ON c.ContactID = ca.ConsentAgreementContactID
            WHERE ((a.ActivityCreated < @DateTo)
                    OR (c.ContactCreated < @DateTo AND a.ActivityID IS NULL))
                AND c.ContactLastName LIKE 'Anonymous - %'
                AND esc.EmailSubscriptionConfirmationContactID is null
                AND ca.ConsentAgreementID is null;

            DELETE TOP (100) c
            FROM OM_Contact c
            LEFT JOIN OM_Activity a
                ON c.ContactID = a.ActivityContactID
            LEFT JOIN EmailLibrary_EmailSubscriptionConfirmation esc
                ON c.ContactID = esc.EmailSubscriptionConfirmationContactID
            LEFT JOIN CMS_ConsentAgreement ca
                ON c.ContactID = ca.ConsentAgreementContactID
            WHERE ((a.ActivityCreated < @DateTo)
                    OR (c.ContactCreated < @DateTo AND a.ActivityID IS NULL))
                AND c.ContactLastName LIKE 'Anonymous - %'
                AND esc.EmailSubscriptionConfirmationContactID is null
                AND ca.ConsentAgreementID is null;

            SET @BatchContactsDeleted = @@ROWCOUNT;
            SET @TotalContactsDeleted = @TotalContactsDeleted + @BatchContactsDeleted;

            DELETE a
            FROM OM_Activity a
            LEFT JOIN OM_Contact c
                ON a.ActivityContactID = c.ContactID
            WHERE c.ContactID IS NULL 
                OR (a.ActivityCreated < @DateTo AND a.ActivityChannelID IS NULL)

            SET @BatchActivitiesDeleted = @@ROWCOUNT;
            IF @BatchActivitiesDeleted = 0
                BREAK;

            SET @BatchActivitiesDeleted = @@ROWCOUNT;
            SET @TotalActivitiesDeleted = @TotalActivitiesDeleted + @BatchActivitiesDeleted;

            IF @BatchActivitiesDeleted = 0 AND @BatchContactsDeleted = 0
                BREAK;

            WAITFOR DELAY '00:00:05'; -- 5-second delay between batches
        END

        SELECT @TotalContactsDeleted as TotalContacts, @TotalActivitiesDeleted as TotalActivities
        """;

        var toDate = command.DateTo > clock.GetLocalNow().DateTime.AddMonths(DELETE_MONTH_LIMIT)
            ? clock.GetLocalNow().DateTime.AddMonths(DELETE_MONTH_LIMIT)
            : command.DateTo;

        var qp = new QueryDataParameters
        {
            new DataParameter("@DateTo", toDate)
        };

        var result = new DeleteContactCommandResult(0, 0, toDate);
        bool check = false;
        if (check)
        {
            return ResponseFrom(result);
        }

        var reader = await ConnectionHelper.ExecuteReaderAsync(query, qp, QueryTypeEnum.SQLQuery, CommandBehavior.Default, default);
        while (reader.Read())
        {
            int contactsDeleted = reader.GetInt32("TotalContacts");
            int activitiesDeleted = reader.GetInt32("TotalActivities");
            result = new DeleteContactCommandResult(contactsDeleted, activitiesDeleted, toDate);
        }

        await reader.CloseAsync();
        return ResponseFrom(result);
    }
}

public record DeleteContactCommand(DateTime? DateTo, bool OnlyDeleteContactsWithoutActivities);
public record DeleteContactCommandResult(int ContactsDeleted, int ActivitiesDeleted, DateTime? DateTo);

public class ContactMaintenancePageProperties : TemplateClientProperties
{
    public string Label { get; set; } = "Contacts Maintenance";
    public int TotalContacts { get; set; } = 0;
    public int TotalActivities { get; set; } = 0;
}

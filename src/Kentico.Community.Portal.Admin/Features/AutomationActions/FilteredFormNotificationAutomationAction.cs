using CMS.Automation;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.Helpers.Internal;
using CMS.Membership;
using CMS.Notifications;
using CMS.OnlineForms;
using CMS.OnlineForms.Internal;
using Kentico.Community.Portal.Admin.Features.AutomationActions;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<FilteredFormNotificationAction>(
    identifier: FilteredFormNotificationAction.IDENTIFIER,
    displayName: "Filtered form notification",
    Description = "Sends a notification to users or roles when a form field matches the specified value.",
    IconName = Icons.Bell)]

namespace Kentico.Community.Portal.Admin.Features.AutomationActions;

public class FilteredFormNotificationActionProperties : IAutomationActionProperties
{
    [DropDownComponent(
        Label = "Recipient type",
        ExplanationText = "Controls whether notifications are sent to specific users or users in roles.",
        Options = "Users;Users\nRoles;Roles",
        Order = 10)]
    [RequiredValidationRule]
    public string RecipientType { get; set; } = "Users";

    [GeneralSelectorComponent(
        dataProviderType: typeof(UserGeneralSelectorDataProvider),
        Label = "Users",
        ExplanationText = "Only enabled users will receive notifications.",
        Placeholder = "Select users",
        Order = 20)]
    [VisibleIfEqualTo(nameof(RecipientType), "Users")]
    public IEnumerable<string> Users { get; set; } = [];

    [GeneralSelectorComponent(
        dataProviderType: typeof(RoleGeneralSelectorDataProvider),
        Label = "Roles",
        ExplanationText = "Enabled users in the selected roles will receive notifications.",
        Placeholder = "Select roles",
        Order = 30)]
    [VisibleIfEqualTo(nameof(RecipientType), "Roles")]
    public IEnumerable<string> Roles { get; set; } = [];

    [DropDownComponent(
        Label = "Notification",
        ExplanationText = "The custom notification email to send to recipients.",
        DataProviderType = typeof(CustomNotificationDropDownOptionsProvider),
        Order = 40)]
    [RequiredValidationRule]
    public string NotificationCodeName { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Form field name",
        ExplanationText = "The name of the form field to check. Must match a field from the form used as the automation trigger.",
        Order = 50)]
    [RequiredValidationRule]
    public string FormFieldName { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Form field value",
        ExplanationText = "The value the form field must match to trigger the notification.",
        Order = 60)]
    [RequiredValidationRule]
    public string FormFieldValue { get; set; } = string.Empty;
}

public class FilteredFormNotificationAction(
    IInfoProvider<BizFormInfo> bizFormInfoProvider,
    IInfoProvider<RoleInfo> roleProvider,
    IInfoProvider<UserInfo> userProvider,
    IInfoProvider<UserRoleInfo> userRoleProvider,
    INotificationEmailMessageProvider notificationEmailMessageProvider,
    IEmailService emailService,
    IDeepLinkBuilder<FormSubmissionDeepLinkParameters> deepLinkBuilder,
    ILogger<FilteredFormNotificationAction> logger) : AutomationAction<FilteredFormNotificationActionProperties>
{
    public const string IDENTIFIER = "KenticoCommunity.FilteredFormNotification";

    private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider = bizFormInfoProvider;
    private readonly IInfoProvider<RoleInfo> roleProvider = roleProvider;
    private readonly IInfoProvider<UserInfo> userProvider = userProvider;
    private readonly IInfoProvider<UserRoleInfo> userRoleProvider = userRoleProvider;
    private readonly INotificationEmailMessageProvider notificationEmailMessageProvider = notificationEmailMessageProvider;
    private readonly IEmailService emailService = emailService;
    private readonly IDeepLinkBuilder<FormSubmissionDeepLinkParameters> deepLinkBuilder = deepLinkBuilder;
    private readonly ILogger<FilteredFormNotificationAction> logger = logger;

    public override async Task Execute(
        FilteredFormNotificationActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.TriggeredByFormSubmission(cancellationToken))
        {
            logger.LogWarning("Action {Identifier} requires a form submission trigger.", IDENTIFIER);
            return;
        }

        var formData = await context.GetTriggerData<FormSubmissionTriggerData>(cancellationToken);
        if (formData is null)
        {
            logger.LogWarning("No form submission trigger data found for action {Identifier}.", IDENTIFIER);
            return;
        }

        var formItem = await GetFormItemAsync(formData, cancellationToken);
        if (formItem is null)
        {
            logger.LogWarning("Form item {BizFormItemId} not found for form {BizFormId}.",
                formData.BizFormItemId, formData.BizFormId);
            return;
        }

        string fieldValue = formItem.GetStringValue(properties.FormFieldName, string.Empty);
        if (!string.Equals(fieldValue, properties.FormFieldValue, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var recipients = await GetRecipientsAsync(properties, cancellationToken);
        if (!recipients.Any())
        {
            logger.LogInformation("No recipients found for action {Identifier}.", IDENTIFIER);
            return;
        }

        string formSubmissionLink = deepLinkBuilder.GetDeepLink(new FormSubmissionDeepLinkParameters
        {
            FormId = formData.BizFormId,
            SubmissionId = formData.BizFormItemId
        });

        var placeholders = new FormLinkNotificationPlaceholders(properties.NotificationCodeName, formSubmissionLink);
        foreach (var recipient in recipients)
        {
            try
            {
                var emailMessage = await notificationEmailMessageProvider.CreateEmailMessage(
                    properties.NotificationCodeName,
                    recipient.UserID,
                    placeholders,
                    cancellationToken);

                await emailService.SendEmail(emailMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to send notification {NotificationCodeName} to user {UserId} in action {Identifier}.",
                    properties.NotificationCodeName,
                    recipient.UserID,
                    IDENTIFIER);
            }
        }
    }

    private async Task<BizFormItem?> GetFormItemAsync(FormSubmissionTriggerData formData, CancellationToken cancellationToken)
    {
        var formInfo = await bizFormInfoProvider.GetAsync(formData.BizFormId, cancellationToken);
        if (formInfo is null)
        {
            return null;
        }

        string formClassName = DataClassInfoProvider.GetDataClassInfo(formInfo.FormClassID).ClassName;
        return BizFormItemProvider.GetItem(formData.BizFormItemId, formClassName);
    }

    private async Task<IEnumerable<UserInfo>> GetRecipientsAsync(
        FilteredFormNotificationActionProperties properties,
        CancellationToken cancellationToken) =>
        string.Equals(properties.RecipientType, "Roles", StringComparison.OrdinalIgnoreCase)
            ? await GetUsersInRolesAsync(properties.Roles, cancellationToken)
            : await GetUsersAsync(properties.Users, cancellationToken);

    private async Task<IEnumerable<UserInfo>> GetUsersInRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken)
    {
        var roles = await roleProvider.Get()
            .WhereIn(nameof(RoleInfo.RoleName), roleNames.ToList())
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var roleIds = roles.Select(r => r.RoleID).ToList();
        if (roleIds.Count == 0)
        {
            return [];
        }

        var userRoles = await userRoleProvider.Get()
            .WhereIn(nameof(UserRoleInfo.RoleID), roleIds)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var userIds = userRoles.Select(ur => ur.UserID).Distinct().ToList();
        if (userIds.Count == 0)
        {
            return [];
        }

        return await userProvider.Get()
            .WhereIn(nameof(UserInfo.UserID), userIds)
            .WhereEquals(nameof(UserInfo.UserEnabled), true)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }

    private async Task<IEnumerable<UserInfo>> GetUsersAsync(
        IEnumerable<string> userNames,
        CancellationToken cancellationToken) =>
        await userProvider.Get()
            .WhereIn(nameof(UserInfo.UserName), userNames.ToList())
            .WhereEquals(nameof(UserInfo.UserEnabled), true)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
}

internal sealed class FormLinkNotificationPlaceholders(
    string notificationEmailName,
    string formLink) : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName { get; } = notificationEmailName;

    [PlaceholderDisableHtmlEncoding]
    public string FormLink { get; } = formLink;
}

internal sealed class CustomNotificationDropDownOptionsProvider(
    IInfoProvider<NotificationEmailInfo> notificationEmailInfoProvider) : IDropDownOptionsProvider
{
    private readonly IInfoProvider<NotificationEmailInfo> notificationEmailInfoProvider = notificationEmailInfoProvider;

    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var notificationEmails = notificationEmailInfoProvider
            .Get()
            .WhereEquals(nameof(NotificationEmailInfo.NotificationEmailEventType), NotificationEmailEventType.CUSTOM)
            .GetEnumerableTypedResult();

        var options = notificationEmails
            .OrderBy(email => email.NotificationEmailDisplayName)
            .Select(email => new DropDownOptionItem
            {
                Value = email.NotificationEmailName,
                Text = string.IsNullOrWhiteSpace(email.NotificationEmailDisplayName)
                    ? email.NotificationEmailName
                    : email.NotificationEmailDisplayName,
                Tooltip = email.NotificationEmailName
            });

        return Task.FromResult(options);
    }
}

internal sealed class RoleGeneralSelectorDataProvider(
    IInfoProvider<RoleInfo> roleProvider) : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<RoleInfo> roleProvider = roleProvider;
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Invalid", Value = string.Empty };

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var roles = await roleProvider.Get()
            .Columns(nameof(RoleInfo.RoleName), nameof(RoleInfo.RoleDisplayName))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var items = string.IsNullOrEmpty(searchTerm)
            ? roles
            : roles.Where(r => r.RoleDisplayName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);

        return new PagedSelectListItems<string>
        {
            NextPageAvailable = false,
            Items = items.Select(r => new ObjectSelectorListItem<string> { IsValid = true, Text = r.RoleDisplayName, Value = r.RoleName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(
        IEnumerable<string> selectedValues,
        CancellationToken cancellationToken)
    {
        var roles = await roleProvider.Get()
            .Columns(nameof(RoleInfo.RoleName), nameof(RoleInfo.RoleDisplayName))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return (selectedValues ?? []).Select(value =>
            roles.FirstOrDefault(r => string.Equals(value, r.RoleName, StringComparison.OrdinalIgnoreCase)) is { } role
                ? new ObjectSelectorListItem<string> { IsValid = true, Text = role.RoleDisplayName, Value = role.RoleName }
                : InvalidItem);
    }
}

internal sealed class UserGeneralSelectorDataProvider(
    IInfoProvider<UserInfo> userProvider) : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<UserInfo> userProvider = userProvider;
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Invalid", Value = string.Empty };

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var query = userProvider.Get()
            .Columns(nameof(UserInfo.UserName))
            .WhereEquals(nameof(UserInfo.UserEnabled), true);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.WhereContains(nameof(UserInfo.UserName), searchTerm);
        }

        var users = await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return new PagedSelectListItems<string>
        {
            NextPageAvailable = false,
            Items = users.Select(u => new ObjectSelectorListItem<string> { IsValid = true, Text = u.UserName, Value = u.UserName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(
        IEnumerable<string> selectedValues,
        CancellationToken cancellationToken)
    {
        var users = await userProvider.Get()
            .Columns(nameof(UserInfo.UserName))
            .WhereEquals(nameof(UserInfo.UserEnabled), true)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return (selectedValues ?? []).Select(value =>
            users.FirstOrDefault(u => string.Equals(value, u.UserName, StringComparison.OrdinalIgnoreCase)) is { } user
                ? new ObjectSelectorListItem<string> { IsValid = true, Text = user.UserName, Value = user.UserName }
                : InvalidItem);
    }
}

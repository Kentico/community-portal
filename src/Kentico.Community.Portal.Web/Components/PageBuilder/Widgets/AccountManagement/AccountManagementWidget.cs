using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountManagement;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: AccountManagementWidget.IDENTIFIER,
    viewComponentType: typeof(AccountManagementWidget),
    name: "Account Management",
    propertiesType: typeof(AccountManagementWidgetProperties),
    Description = "Displays account management interface for authenticated users.",
    IconClass = KenticoIcons.BRIEFCASE,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountManagement;

public class AccountManagementWidget(
    UserManager<CommunityMember> userManager,
    MemberBadgeService memberBadgeService,
    LinkGenerator linkGenerator,
    QAndANotificationSettingsManager notificationsManager,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever urlRetriever) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.AccountManagementWidget";

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<AccountManagementWidgetProperties> _)
    {
        var member = (await userManager.GetUserAsync(HttpContext.User))!;
        if (member is null)
        {
            var mockVm = new AccountManagementWidgetViewModel(new());
            return View("~/Components/PageBuilder/Widgets/AccountManagement/AccountManagement.cshtml", mockVm);
        }
        var accountVM = new MyAccountViewModel
        {
            MemberID = member.Id,
            Username = member.UserName ?? "",
            Email = member.Email ?? "",
            ProfileLinkURL = linkGenerator.GetUriByAction(HttpContext, nameof(MemberController.MemberDetail), "Member", new { memberID = member.Id }) ?? "",
            Profile = new()
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                LinkedInIdentifier = member.LinkedInIdentifier,
                JobTitle = member.JobTitle,
                EmployerName = member.EmployerLink.Label,
                EmployerWebsiteURL = member.EmployerLink.URL,
                TimeZone = member.TimeZone,
            },
            DateCreated = member.Created,
            AvatarForm = new()
            {
                MemberID = member.Id
            },
            BadgesForm = new(await memberBadgeService.GetAllBadgesFor(member.Id)),
            NotificationsForm = await GetNotificationsFormViewModel(member)
        };

        return View("~/Components/PageBuilder/Widgets/AccountManagement/AccountManagement.cshtml", new AccountManagementWidgetViewModel(accountVM));
    }

    private async Task<QAndANotificationsFormViewModel> GetNotificationsFormViewModel(CommunityMember member)
    {
        var webPageItemIDs = await notificationsManager.GetSubscribedWebPageItemIDs(member);
        int[] subscribedItemIDsArray = [.. webPageItemIDs];
        var subscribedDiscussions = subscribedItemIDsArray.Length > 0
            ? await contentRetriever.RetrievePages<QAndAQuestionPage>(
                RetrievePagesParameters.Default,
                q => q.Where(w => w.WhereIn(
                    nameof(QAndAQuestionPage.SystemFields.WebPageItemID),
                    subscribedItemIDsArray)),
                RetrievalCacheSettings.CacheDisabled)
            : [];

        var frequency = await notificationsManager.GetMemberFrequency(member.Id);
        bool autoSubscribeEnabled = await notificationsManager.GetAutoSubscribeEnabled(member.Id);

        var subscribedDiscussionModels = new List<SubscribedDiscussionViewModel>();
        foreach (var q in subscribedDiscussions.OrderByDescending(q => q.QAndAQuestionPageDateCreated))
        {
            var pageUrl = await urlRetriever.Retrieve(q);

            subscribedDiscussionModels.Add(new SubscribedDiscussionViewModel
            {
                WebPageItemID = q.SystemFields.WebPageItemID,
                Title = q.BasicItemTitle,
                QuestionURL = pageUrl.RelativePath.RelativePathTrimmed(),
                DateCreated = q.QAndAQuestionPageDateCreated,
                DateModified = q.QAndAQuestionPageDateModified
            });
        }

        return new QAndANotificationsFormViewModel(frequency, autoSubscribeEnabled, subscribedDiscussionModels);
    }
}

public class AccountManagementWidgetProperties : BaseWidgetProperties
{
}

public class AccountManagementWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => "Account Management";

    public MyAccountViewModel Account { get; set; }

    public AccountManagementWidgetViewModel(MyAccountViewModel account) => Account = account;
}


public class MyAccountViewModel
{
    public int MemberID { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string ProfileLinkURL { get; set; } = "";
    public ProfileViewModel Profile { get; set; } = new();
    public BadgesFormViewModel BadgesForm { get; set; } = new([]);
    public DateTime DateCreated { get; set; }
    public UpdatePasswordViewModel PasswordInfo { get; set; } = new();
    public AvatarFormViewModel AvatarForm { get; set; } = new(); public QAndANotificationsFormViewModel NotificationsForm { get; set; } = new();
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CMS.Base;
using EnumsNET;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kentico.Community.Portal.Web.Features.Accounts;

[Route("[controller]/[action]")]
[Authorize]
public class AccountController(
    WebPageMetaService metaService,
    UserManager<CommunityMember> userManager,
    SignInManager<CommunityMember> signInManager,
    MemberContactManager contactManager,
    MemberBadgeService memberBadgeService,
    AvatarImageService avatarImageService,
    ILogger<AccountController> logger,
    LinkGenerator linkGenerator,
    IReadOnlyModeProvider readOnlyProvider,
    QAndANotificationSettingsManager notificationsManager,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever urlRetriever) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly SignInManager<CommunityMember> signInManager = signInManager;
    private readonly MemberContactManager contactManager = contactManager;
    private readonly MemberBadgeService memberBadgeService = memberBadgeService;
    private readonly AvatarImageService avatarImageService = avatarImageService;
    private readonly ILogger<AccountController> logger = logger;
    private readonly LinkGenerator linkGenerator = linkGenerator;
    private readonly QAndANotificationSettingsManager notificationsManager = notificationsManager;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;

    [HttpGet]
    public async Task<ActionResult> MyAccount()
    {
        metaService.SetMeta(new WebPageMeta("My Account", "Manage your member account."));

        var member = await userManager.GetUserAsync(User);
        if (member is null)
        {
            return Unauthorized();
        }

        var vm = new MyAccountViewModel
        {
            Page = new PortalPage("My Account", "Manage your account profile, password, and member settings."),
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

        return View("~/Features/Accounts/MyAccount.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateProfile(ProfileViewModel model)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        if (!ModelState.IsValid)
        {
            return PartialView("~/Features/Accounts/_ProfileForm.cshtml", model);
        }

        var member = await userManager.GetUserAsync(User);
        if (member is null)
        {
            return Unauthorized();
        }

        member.FirstName = model.FirstName ?? "";
        member.LastName = model.LastName ?? "";
        member.LinkedInIdentifier = model.LinkedInIdentifier ?? "";
        member.JobTitle = model.JobTitle ?? "";
        member.EmployerLink.Label = model.EmployerName ?? "";
        member.EmployerLink.URL = model.EmployerWebsiteURL ?? "";
        member.TimeZone = model.TimeZone ?? "";

        _ = await userManager.UpdateAsync(member);

        _ = await contactManager.SetMemberAsCurrentContact(member);

        return PartialView("~/Features/Accounts/_ProfileForm.cshtml", new ProfileViewModel
        {
            FirstName = member.FirstName,
            LastName = member.LastName,
            LinkedInIdentifier = member.LinkedInIdentifier,
            TimeZone = member.TimeZone,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdatePassword(UpdatePasswordViewModel model)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        if (!ModelState.IsValid)
        {
            return PartialView("~/Features/Accounts/_PasswordForm.cshtml", model);
        }

        var member = await userManager.GetUserAsync(User);
        if (member is null)
        {
            return Unauthorized();
        }

        var result = await userManager.ChangePasswordAsync(member, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return PartialView("~/Features/Accounts/_PasswordForm.cshtml", model);
        }

        // Reauthenticate the user because the security stamp has changed
        await signInManager.SignInAsync(member, isPersistent: false);

        return PartialView("~/Features/Accounts/_PasswordForm.cshtml", new UpdatePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateAvatarImage(AvatarFormViewModel model)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        var member = await userManager.GetUserAsync(User);
        if (member is null)
        {
            return Unauthorized();
        }

        model.MemberID = member.Id;

        if (!ModelState.IsValid)
        {
            return PartialView("~/Features/Accounts/_AvatarForm.cshtml", model);
        }

        var attachment = model.AvatarImageFileAttachment!;

        try
        {
            await avatarImageService.UpdateAvatarImage(attachment, member.Id);

            member.AvatarFileExtension = Path.GetExtension(attachment.FileName);
            _ = await userManager.UpdateAsync(member);
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, "UPDATE_AVATAR_FAILURE"), ex, "Failed to update avatar for member {MemberID}", member.Id);
            ModelState.AddModelError(nameof(AvatarFormViewModel.AvatarImageFileAttachment), "There was a problem with the application and we could not update your avatar.");
            return PartialView("~/Features/Accounts/_AvatarForm.cshtml", model);
        }

        return PartialView("~/Features/Accounts/_AvatarForm.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateSelectedBadges(UpdateBadgesRequest request)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        var member = await userManager.GetUserAsync(User);
        if (member is null)
        {
            return Unauthorized();
        }

        // Include always-selected badges in the total count limit
        if (request.Badges.Count(x => x.IsSelected) > BadgesFormViewModel.MAX_SELECTED_BADGES)
        {
            return ValidationProblem();
        }

        await memberBadgeService.UpdateSelectedBadgesFor(request.Badges, member.Id);
        var badges = await memberBadgeService.GetAllBadgesFor(member.Id);

        return PartialView("~/Features/Accounts/_BadgesForm.cshtml", new BadgesFormViewModel(badges));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateQAndANotifications(QAndANotificationsFormViewModel model)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        var member = await userManager.GetUserAsync(User);
        if (member is null)
        {
            return Unauthorized();
        }

        await notificationsManager.UpdateMemberSettings(member.Id, model.SelectedFrequency, model.AutoSubscribeEnabled);

        var updatedModel = await GetNotificationsFormViewModel(member);
        updatedModel.SelectedFrequency = model.SelectedFrequency;
        updatedModel.AutoSubscribeEnabled = model.AutoSubscribeEnabled;

        return PartialView("~/Features/Accounts/_DiscussionNotificationsForm.cshtml", updatedModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UnsubscribeDiscussion(int webPageItemID)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        var member = await userManager.GetUserAsync(User);
        if (member is null)
        {
            return Unauthorized();
        }

        var questionPages = await contentRetriever.RetrievePages<QAndAQuestionPage>(
            RetrievePagesParameters.Default,
            q => q.Where(w => w.WhereEquals(
                nameof(QAndAQuestionPage.SystemFields.WebPageItemID),
                webPageItemID)),
            RetrievalCacheSettings.CacheDisabled);

        if (questionPages.FirstOrDefault() is QAndAQuestionPage questionPage)
        {
            _ = await notificationsManager.UnsubscribeFromDiscussion(member.Id, questionPage);
        }

        var model = await GetNotificationsFormViewModel(member);
        return PartialView("~/Features/Accounts/_DiscussionSubscriptions.cshtml", model.SubscribedDiscussions);
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

public record UpdateBadgesRequest(List<SelectedBadgeViewModel> Badges);

public class BadgesFormViewModel
{
    public const int MAX_SELECTED_BADGES = 5;

    public IReadOnlyList<MemberBadgeViewModel> Badges { get; }
    public IReadOnlyList<MemberBadgeViewModel> AlwaysSelectedBadges { get; }
    public IReadOnlyList<MemberBadgeViewModel> SelectableBadges { get; }
    public int InitialSelectedCount { get; }
    public int RemainingSlots { get; }

    public BadgesFormViewModel(IReadOnlyList<MemberBadgeViewModel> badges)
    {
        Badges = badges;
        AlwaysSelectedBadges = [.. badges.Where(x => x.AlwaysSelected)];
        SelectableBadges = [.. badges.Where(x => !x.AlwaysSelected)];
        InitialSelectedCount = badges.Count(x => x.IsSelected && !x.AlwaysSelected);
        RemainingSlots = MAX_SELECTED_BADGES - badges.Count(x => x.AlwaysSelected);
    }
}

public class SelectedBadgeViewModel
{
    public int BadgeId { get; set; }
    public string BadgeCodeName { get; set; } = "";
    public bool IsSelected { get; set; }
}

public class MyAccountViewModel
{
    public required PortalPage Page { get; set; }
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

public class ProfileViewModel
{
    [DataType(DataType.Text)]
    [DisplayName("First name")]
    [MaxLength(40, ErrorMessage = "First name cannot be longer than 40 characters.")]
    public string? FirstName { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("Last name")]
    [MaxLength(40, ErrorMessage = "Last Name cannot be longer than 40 characters.")]
    public string? LastName { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("LinkedIn Identifier")]
    [MaxLength(40, ErrorMessage = "The LinkedIn Identifier cannot be longer than 40 characters")]
    [RegularExpression(@"^(?!.*https:\/\/www\.linkedin\.com\/).*$", ErrorMessage = "The value cannot contain the LinkedIn URL.")]
    public string? LinkedInIdentifier { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("Job title")]
    [MaxLength(40, ErrorMessage = "Job title cannot be longer than 40 characters.")]
    public string? JobTitle { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("Employer name")]
    [MaxLength(40, ErrorMessage = "Employer name cannot be longer than 40 characters.")]
    public string? EmployerName { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("Employer website URL")]
    [MaxLength(40, ErrorMessage = "Employer website URL cannot be longer than 40 characters.")]
    [Url(ErrorMessage = "Must be a valid URL")]
    public string? EmployerWebsiteURL { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("Time zone")]
    [MaxLength(100, ErrorMessage = "Time zone cannot be longer than 100 characters.")]
    public string? TimeZone { get; set; } = "";

    [BindNever]
    public IReadOnlyList<SelectListItem> TimeZoneOptions { get; } = TimeZoneInfo.GetSystemTimeZones()
        .Select(tz => new SelectListItem(tz.DisplayName, tz.Id))
        .ToList();
}

public class UpdatePasswordViewModel
{
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "The current password cannot be empty.")]
    [DisplayName("Current Password")]
    [MaxLength(100, ErrorMessage = "The password cannot be longer than 100 characters.")]
    public string CurrentPassword { get; set; } = "";

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "The new password cannot be empty.")]
    [DisplayName("New Password")]
    [MaxLength(100, ErrorMessage = "The password cannot be longer than 100 characters.")]
    public string NewPassword { get; set; } = "";

    [DataType(DataType.Password)]
    [DisplayName("Password confirmation")]
    [MaxLength(100, ErrorMessage = "The password confirmation cannot be longer than 100 characters.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The entered passwords do not match.")]
    public string PasswordConfirmation { get; set; } = "";
}

public class AvatarFormViewModel
{
    public const int MAX_FILE_SIZE = 102_400;
    public static string[] ALLOWED_EXTENSIONS { get; } = [".jpg", ".jpeg", ".png", ".webp"];
    public static string ALLOWED_EXTENSIONS_JOINED { get; } = string.Join(", ", ALLOWED_EXTENSIONS);

    [BindNever]
    public int MemberID { get; set; }

    [Required]
    [DisplayName("Avatar Image")]
    [AllowedExtensions(extensions: [".jpg", ".jpeg", ".png", ".webp"])]
    // 100 kb
    [MaxFileSize(MAX_FILE_SIZE)]
    public IFormFile? AvatarImageFileAttachment { get; set; }
}

public class QAndANotificationsFormViewModel
{
    public QAndANotificationFrequencyType SelectedFrequency { get; set; }
    public bool AutoSubscribeEnabled { get; set; }
    public IReadOnlyList<SubscribedDiscussionViewModel> SubscribedDiscussions { get; } = [];
    public IReadOnlyList<SelectListItem> FrequencyOptions { get; } = [];

    public QAndANotificationsFormViewModel()
    {
    }

    public QAndANotificationsFormViewModel(
        QAndANotificationFrequencyType selectedFrequency,
        bool autoSubscribeEnabled,
        IReadOnlyList<SubscribedDiscussionViewModel> subscribedDiscussions)
    {
        SelectedFrequency = selectedFrequency;
        AutoSubscribeEnabled = autoSubscribeEnabled;
        SubscribedDiscussions = subscribedDiscussions;
        FrequencyOptions = Enums
            .GetMembers<QAndANotificationFrequencyType>(EnumMemberSelection.All)
            .Select(e => new SelectListItem
            {
                Value = e.Value.ToString(),
                Text = e.Attributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? e.Name,
                Selected = e.Value == selectedFrequency
            })
            .ToList();
    }
}

public class SubscribedDiscussionViewModel
{
    public int WebPageItemID { get; set; }
    public string Title { get; set; } = "";
    public string QuestionURL { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
}

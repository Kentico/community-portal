using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CMS.Core;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    IEventLogService log,
    LinkGenerator linkGenerator) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly SignInManager<CommunityMember> signInManager = signInManager;
    private readonly MemberContactManager contactManager = contactManager;
    private readonly MemberBadgeService memberBadgeService = memberBadgeService;
    private readonly AvatarImageService avatarImageService = avatarImageService;
    private readonly IEventLogService log = log;
    private readonly LinkGenerator linkGenerator = linkGenerator;

    [HttpGet]
    public async Task<ActionResult> MyAccount()
    {
        metaService.SetMeta(new("My Account", "Manage your Community Portal account."));

        var member = await userManager.GetUserAsync(User);

        if (member is null)
        {
            return Unauthorized();
        }

        var vm = new MyAccountViewModel
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
            },
            DateCreated = member.Created,
            AvatarForm = new()
            {
                MemberID = member.Id
            },
            BadgesForm = new()
            {
                Badges = await memberBadgeService.GetAllBadgesFor(member.Id)
            }
        };

        return View("~/Features/Accounts/MyAccount.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateProfile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.State = UpdateState.Error;
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

        _ = await userManager.UpdateAsync(member);

        _ = await contactManager.SetMemberAsCurrentContact(member);

        return PartialView("~/Features/Accounts/_ProfileForm.cshtml", new ProfileViewModel
        {
            FirstName = member.FirstName,
            LastName = member.LastName,
            LinkedInIdentifier = member.LinkedInIdentifier,
            State = UpdateState.Updated,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdatePassword(UpdatePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.State = UpdateState.Error;
            return PartialView("~/Features/Accounts/_PasswordForm.cshtml", model);
        }

        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.State = UpdateState.Error;
            return PartialView("~/Features/Accounts/_PasswordForm.cshtml", model);
        }

        // Reauthenticate the user because the security stamp has changed
        await signInManager.SignInAsync(user, isPersistent: false);

        return PartialView("~/Features/Accounts/_PasswordForm.cshtml", new UpdatePasswordViewModel
        {
            State = UpdateState.Updated
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateAvatarImage(AvatarFormViewModel model)
    {
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
            log.LogException(nameof(UpdateAvatarImage), "UPDATE_AVATAR", ex);

            ModelState.AddModelError(nameof(AvatarFormViewModel.AvatarImageFileAttachment), "There was a problem with the application and we could not update your avatar.");

            return PartialView("~/Features/Accounts/_AvatarForm.cshtml", model);
        }

        model.State = UpdateState.Updated;
        return PartialView("~/Features/Accounts/_AvatarForm.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateSelectedBadges(UpdateBadgesRequest request)
    {
        var member = await userManager.GetUserAsync(User);

        if (member is null)
        {
            return Unauthorized();
        }

        if (request.Badges.Count(x => x.IsSelected) > BadgesFormViewModel.MAX_SELECTED_BADGES)
        {
            return ValidationProblem();
        }

        await memberBadgeService.UpdateSelectedBadgesFor(request.Badges, member.Id);
        var badges = await memberBadgeService.GetAllBadgesFor(member.Id);

        return PartialView("~/Features/Accounts/_BadgesForm.cshtml", new BadgesFormViewModel
        {
            Badges = badges,
            State = UpdateState.Updated
        });
    }
}

public record UpdateBadgesRequest(List<SelectedBadgeViewModel> Badges);

public class BadgesFormViewModel
{
    public const int MAX_SELECTED_BADGES = 4;

    [BindNever]
    public UpdateState State { get; set; } = UpdateState.Unmodified;

    public IReadOnlyList<MemberBadgeViewModel> Badges { get; set; } = [];
}

public class SelectedBadgeViewModel
{
    public int BadgeId { get; set; }
    public bool IsSelected { get; set; }
}

public class MyAccountViewModel : IPortalPage
{
    public int MemberID { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string ProfileLinkURL { get; set; } = "";
    public ProfileViewModel Profile { get; set; } = new();
    public BadgesFormViewModel BadgesForm { get; set; } = new();
    public DateTime DateCreated { get; set; }
    public UpdatePasswordViewModel PasswordInfo { get; set; } = new();
    public string Title => "My Account";
    public string ShortDescription => "Manage your account profile, password, and member settings.";

    public AvatarFormViewModel AvatarForm { get; set; } = new();
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

    public UpdateState State { get; set; } = UpdateState.Unmodified;
}

public enum UpdateState
{
    Unmodified,
    Error,
    Updated
}

public class UpdatePasswordViewModel
{
    public UpdateState State { get; set; } = UpdateState.Unmodified;

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
    public UpdateState State { get; set; } = UpdateState.Unmodified;

    [BindNever]
    public int MemberID { get; set; }

    [Required]
    [DisplayName("Avatar Image")]
    [AllowedExtensions(extensions: [".jpg", ".jpeg", ".png", ".webp"])]
    // 100 kb
    [MaxFileSize(MAX_FILE_SIZE)]
    public IFormFile? AvatarImageFileAttachment { get; set; }
}

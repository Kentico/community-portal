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
    IEventLogService log) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly SignInManager<CommunityMember> signInManager = signInManager;
    private readonly MemberContactManager contactManager = contactManager;
    private readonly MemberBadgeService memberBadgeService = memberBadgeService;
    private readonly AvatarImageService avatarImageService = avatarImageService;
    private readonly IEventLogService log = log;

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
            Profile = new()
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                LinkedInIdentifier = member.LinkedInIdentifier,
            },
            DateCreated = member.Created,
            AvatarForm = new()
            {
                MemberID = member.Id,
                ShowForm = false
            },
            EarnedBadges = await memberBadgeService.GetAllBadgesFor(member.Id)
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

        _ = await userManager.UpdateAsync(member);

        _ = contactManager.SetMemberAsCurrentContact(member);

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

        model.ShowForm = true;
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

        model.ShowForm = false;
        return PartialView("~/Features/Accounts/_AvatarForm.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateSelectedBadges(List<SelectedBadgeViewModel> badges)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        if (badges.Count(x => x.IsSelected) > 3)
        {
            return ValidationProblem();
        }

        _ = await memberBadgeService.UpdateSelectedBadgesFor(badges, user.Id);

        return RedirectToAction(nameof(MyAccount));
    }
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
    public ProfileViewModel Profile { get; set; } = new();
    public IReadOnlyList<MemberBadgeViewModel> EarnedBadges { get; set; } = [];
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
    [MaxLength(40, ErrorMessage = "First Name be longer than 40 characters.")]
    public string? FirstName { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("Last Name")]
    [MaxLength(40, ErrorMessage = "Last Name be longer than 40 characters.")]
    public string? LastName { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("LinkedIn Identifier")]
    [MaxLength(40, ErrorMessage = "The LinkedIn Identifier cannot be longer than 40 characters")]
    public string? LinkedInIdentifier { get; set; } = "";

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
    [BindNever]
    public int MemberID { get; set; }

    [BindNever]
    public bool ShowForm { get; set; }

    [Required]
    [DisplayName("Avatar Image")]
    [AllowedExtensions(extensions: [".jpg", ".jpeg", ".png", ".webp"])]
    // 100 kb
    [MaxFileSize(102_400)]
    public IFormFile? AvatarImageFileAttachment { get; set; }
}

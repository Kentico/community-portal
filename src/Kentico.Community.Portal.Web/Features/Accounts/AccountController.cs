using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Accounts;

[Route("[controller]/[action]")]
[Authorize]
public class AccountController : Controller
{
    private readonly WebPageMetaService metaService;
    private readonly UserManager<CommunityMember> userManager;
    private readonly SignInManager<CommunityMember> signInManager;
    private readonly MemberContactManager contactManager;

    public AccountController(
        WebPageMetaService metaService,
        UserManager<CommunityMember> userManager,
        SignInManager<CommunityMember> signInManager,
        MemberContactManager contactManager)
    {
        this.metaService = metaService;
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.contactManager = contactManager;
    }

    [HttpGet]
    public async Task<ActionResult> MyAccount()
    {
        metaService.SetMeta(new Meta("My Account", "Manage your Community Portal account."));

        var member = await userManager.GetUserAsync(User);

        if (member is null)
        {
            return Unauthorized();
        }

        var vm = new MyAccountViewModel
        {
            Username = member.UserName ?? "",
            Email = member.Email ?? "",
            Profile = new()
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                LinkedInIdentifier = member.LinkedInIdentifier,
            },
            DateCreated = member.Created
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

        member.FirstName = model.FirstName;
        member.LastName = model.LastName;
        member.LinkedInIdentifier = model.LinkedInIdentifier;

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
}

public class MyAccountViewModel : IPortalPage
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public ProfileViewModel Profile { get; set; } = new();
    public DateTime DateCreated { get; set; }
    public UpdatePasswordViewModel PasswordInfo { get; set; } = new();

    public string Title => "My Account";
    public string ShortDescription => "Manage your account profile, password, and member settings.";
}

public class ProfileViewModel
{
    [DataType(DataType.Text)]
    [DisplayName("First name")]
    [MaxLength(25, ErrorMessage = "First Name be longer than 25 characters.")]
    public string FirstName { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("Last Name")]
    [MaxLength(25, ErrorMessage = "Last Name be longer than 25 characters.")]
    public string LastName { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("LinkedIn Identifier")]
    [MaxLength(25, ErrorMessage = "The LinkedIn Identifier cannot be longer than 25 characters")]
    public string LinkedInIdentifier { get; set; } = "";

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

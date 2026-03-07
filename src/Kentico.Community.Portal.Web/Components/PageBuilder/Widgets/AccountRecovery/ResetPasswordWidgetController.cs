using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CMS.Base;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountRecovery;

[Route("[controller]/[action]")]
public class ResetPasswordWidgetController(
    UserManager<CommunityMember> userManager,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    /// <summary>
    /// Step 4
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ResetPassword(ResetPasswordFormViewModel model)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_ResetPasswordForm.cshtml", model);
        }

        if (!ModelState.IsValid)
        {
            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_ResetPasswordForm.cshtml", model);
        }

        var member = await userManager.FindByIdAsync(model.UserId.ToString());
        if (member is null)
        {
            ModelState.AddModelError(nameof(model.UserId), "No matching user could be found.");

            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_ResetPasswordForm.cshtml", model);
        }

        if (member.IsUnderModeration)
        {
            return View("~/Components/PageBuilder/Widgets/Registration/_ModerationStatus.cshtml");
        }

        var result = await userManager.ResetPasswordAsync(member, model.Token, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_ResetPasswordForm.cshtml", model);
        }

        return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_ResetPasswordForm.cshtml", new ResetPasswordFormViewModel());
    }
}

public class ResetPasswordFormViewModel
{
    [HiddenInput]
    public int UserId { get; set; }

    [HiddenInput]
    public string Token { get; set; } = "";

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "The password cannot be empty.")]
    [DisplayName("Password")]
    [MaxLength(100, ErrorMessage = "The password cannot be longer than 100 characters.")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [DisplayName("Password confirmation")]
    [MaxLength(100, ErrorMessage = "The password cannot be longer than 100 characters.")]
    [Compare(nameof(Password), ErrorMessage = "The entered passwords do not match.")]
    public string PasswordConfirmation { get; set; } = "";
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.PasswordRecovery;

public class SetNewPasswordViewModel
{
    [HiddenInput]
    public int UserId { get; set; }

    [HiddenInput]
    public string Token { get; set; }

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "The password cannot be empty.")]
    [DisplayName("Password")]
    [MaxLength(100, ErrorMessage = "The password cannot be longer than 100 characters.")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [DisplayName("Password confirmation")]
    [MaxLength(100, ErrorMessage = "The password cannot be longer than 100 characters.")]
    [Compare(nameof(Password), ErrorMessage = "The entered passwords do not match.")]
    public string PasswordConfirmation { get; set; }

    public State SubmissionState { get; set; } = State.Not_Reset;
}

public enum State
{
    Not_Reset,
    Reset,
}

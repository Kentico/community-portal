
using System.ComponentModel.DataAnnotations;

namespace Kentico.Community.Portal.Web.Features.PasswordRecovery;

public class RequestRecoveryEmailViewModel
{
    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "The email address cannot be empty.")]
    [Display(Name = "Email address")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(254, ErrorMessage = "The Email address cannot be longer than 254 characters.")]
    public string Email { get; set; }

    public string Title { get; set; } = "Reset Your Password";
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kentico.Community.Portal.Web.Features.Authentication;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your user name or email address")]
    [DisplayName("User name or email")]
    [MaxLength(100, ErrorMessage = "Maximum allowed length of the input text is {1}")]
    public string UserNameOrEmail { get; set; } = "";

    [DataType(DataType.Password)]
    [DisplayName("Password")]
    [MaxLength(100, ErrorMessage = "Maximum allowed length of the input text is {1}")]
    public string Password { get; set; } = "";

    [DisplayName("Stay signed in")]
    public bool StaySignedIn { get; set; } = false;
}

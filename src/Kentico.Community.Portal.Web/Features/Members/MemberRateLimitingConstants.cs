namespace Kentico.Community.Portal.Web.Features.Members;

/// <summary>
/// Constants for authentication and member-related rate limiting policy names
/// </summary>
public static class MemberRateLimitingConstants
{
    /// <summary>
    /// Rate limiting policy for login attempts - 5 per 15 minutes per IP/user
    /// </summary>
    public const string Login = "Member_Login";

    /// <summary>
    /// Rate limiting policy for registration attempts - 3 per hour per IP
    /// </summary>
    public const string Registration = "Member_Registration";

    /// <summary>
    /// Rate limiting policy for forgot password requests - 3 per hour per IP/user
    /// </summary>
    public const string ForgotPassword = "Member_ForgotPassword";
}

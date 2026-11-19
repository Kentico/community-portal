namespace Kentico.Community.Portal.Web.Features.Members;

/// <summary>
/// Constants for authentication and member-related rate limiting policy names
/// </summary>
public static class MemberRateLimitingConstants
{
    /// <summary>
    /// Rate limiting policy for login attempts. Limits: <see cref="Infrastructure.RateLimitingOptions.Login"/>
    /// </summary>
    public const string Login = "Member_Login";

    /// <summary>
    /// Rate limiting policy for registration attempts. Limits: <see cref="Infrastructure.RateLimitingOptions.Registration"/>
    /// </summary>
    public const string Registration = "Member_Registration";

    /// <summary>
    /// Rate limiting policy for forgot password requests. Limits: <see cref="Infrastructure.RateLimitingOptions.ForgotPassword"/>
    /// </summary>
    public const string ForgotPassword = "Member_ForgotPassword";

    /// <summary>
    /// Rate limiting policy for member avatar updates
    /// </summary>
    public const string UpdateAvatar = "Member_UpdateAvatar";

    /// <summary>
    /// Rate limiting policy for requests that send an email verification link. Limits: <see cref="Infrastructure.RateLimitingOptions.EmailVerification"/>
    /// </summary>
    public const string EmailVerification = "Member_EmailVerification";
}

using System.Collections.Immutable;
using CMS.Websites.Internal;

namespace Kentico.Community.Portal.Core.Modules;

public static class SystemWebpages
{
    public static EmailConfirmationPage EmailConfirmation { get; } = new();
    public static ResetPasswordPage ResetPassword { get; } = new();

    public static readonly ImmutableList<ISystemWebpage> ProtectedWebpages =
    [
        EmailConfirmation,
        ResetPassword
    ];

    public static bool Includes(WebPageItemInfo webpage) =>
        ProtectedWebpages.Select(c => c.WebpageGUID).Contains(webpage.WebPageItemGUID);

    public record EmailConfirmationPage : ISystemWebpage
    {
        public static Guid GUID { get; } = new Guid("994d5575-4ef4-4ae3-afb7-735532917041");
        public const string CodeName = "EmailConfirmation-8d4ed3mp";

        public Guid WebpageGUID => GUID;

        public string WebpageName => CodeName;
    }

    public record ResetPasswordPage : ISystemWebpage
    {
        public static Guid GUID { get; } = new Guid("c6f20b2a-5e1e-42e7-8d79-60975f1bf061");
        public const string CodeName = "ResetPassword-8cq06mu4";

        public Guid WebpageGUID => GUID;

        public string WebpageName => CodeName;
    }
}

public interface ISystemWebpage
{
    public Guid WebpageGUID { get; }
    public string WebpageName { get; }
}

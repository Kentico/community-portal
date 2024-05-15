using System.Collections.Immutable;
using CMS.EmailLibrary;

namespace Kentico.Community.Portal.Core.Modules;

public static class SystemEmails
{
    public static SupportRequestConfirmationEmail SupportRequestConfirmation { get; } = new();

    public static readonly ImmutableList<ISystemEmailConfiguration> ProtectedEmails =
    [
        SupportRequestConfirmation,
    ];

    public static bool Includes(EmailConfigurationInfo config) =>
        ProtectedEmails.Select(c => c.EmailConfigurationGUID).Contains(config.EmailConfigurationGUID);

    public record SupportRequestConfirmationEmail : ISystemEmailConfiguration
    {
        public static Guid GUID { get; } = new Guid("f8ea991c-f960-40cc-81d7-1137505c2287");
        public const string CodeName = "SupportRequestConfirmation-qsfk6uj0";

        public Guid EmailConfigurationGUID => GUID;

        public string EmailConfigurationName => CodeName;
    }
}

public interface ISystemEmailConfiguration
{
    Guid EmailConfigurationGUID { get; }
    string EmailConfigurationName { get; }
}

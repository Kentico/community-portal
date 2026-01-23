using System.Collections.Immutable;
using CMS.EmailLibrary;

namespace Kentico.Community.Portal.Core.Modules;

public static class SystemEmails
{
    public static SupportRequestConfirmationEmail SupportRequestConfirmation { get; } = new();
    public static PasswordRecoveryConfirmationEmail PasswordRecoveryConfirmation { get; } = new();
    public static RegistrationConfirmationEmail RegistrationConfirmation { get; } = new();
    public static QAndADiscussionNotificationEmail QAndADiscussionNotification { get; } = new();

    public static readonly ImmutableList<ISystemEmailConfiguration> ProtectedEmails =
    [
        SupportRequestConfirmation,
        PasswordRecoveryConfirmation,
        RegistrationConfirmation,
        QAndADiscussionNotification
    ];

    public static bool Includes(EmailConfigurationInfo config) =>
        ProtectedEmails.Select(c => c.EmailConfigurationGUID).Contains(config.EmailConfigurationGUID);

    public record SupportRequestConfirmationEmail : ISystemEmailConfiguration
    {
        public static Guid GUID { get; } = new Guid("57d3ca11-3fa9-49ab-b48e-26e7c856b33e");
        public const string CodeName = "SupportRequestConfirmation-pu4fmsre";

        public Guid EmailConfigurationGUID => GUID;

        public string EmailConfigurationName => CodeName;
    }

    public record PasswordRecoveryConfirmationEmail : ISystemEmailConfiguration
    {
        public static Guid GUID { get; } = new Guid("5e227217-ca57-4c64-90ae-c227d8919776");
        public const string CodeName = "MemberResetPasswordConfirmation-edclynqe";

        public Guid EmailConfigurationGUID => GUID;

        public string EmailConfigurationName => CodeName;
    }

    public record RegistrationConfirmationEmail : ISystemEmailConfiguration
    {
        public static Guid GUID { get; } = new Guid("225d32b8-7089-4c24-8f7f-177ca9f27317");
        public const string CodeName = "MemberRegistrationEmailConfirmation-qz9jyy5q";

        public Guid EmailConfigurationGUID => GUID;

        public string EmailConfigurationName => CodeName;
    }

    public record QAndADiscussionNotificationEmail : ISystemEmailConfiguration
    {
        public static Guid GUID { get; } = new Guid("17ac6ad9-d0cb-4e82-a64f-b63589c6e53d");
        public const string CodeName = "QAndADiscussionNotification-x49wqflo";

        public Guid EmailConfigurationGUID => GUID;

        public string EmailConfigurationName => CodeName;
    }
}

public interface ISystemEmailConfiguration
{
    public Guid EmailConfigurationGUID { get; }
    public string EmailConfigurationName { get; }
}

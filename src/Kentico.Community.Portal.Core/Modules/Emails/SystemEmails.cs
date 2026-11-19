using System.Collections.Immutable;
using CMS.EmailLibrary;

namespace Kentico.Community.Portal.Core.Modules;

public static class SystemEmails
{
    public static PasswordRecoveryConfirmationEmail PasswordRecoveryConfirmation { get; } = new();
    public static RegistrationConfirmationEmail RegistrationConfirmation { get; } = new();
    public static QAndADiscussionNotificationEmail QAndADiscussionNotification { get; } = new();
    public static MemberEmailVerificationEmail MemberEmailVerification { get; } = new();
    public static MemberEmailChangedNotificationEmail MemberEmailChangedNotification { get; } = new();

    public static readonly ImmutableList<ISystemEmailConfiguration> ProtectedEmails =
    [
        PasswordRecoveryConfirmation,
        RegistrationConfirmation,
        QAndADiscussionNotification,
        MemberEmailVerification,
        MemberEmailChangedNotification
    ];

    public static bool Includes(EmailConfigurationInfo config) =>
        ProtectedEmails.Select(c => c.EmailConfigurationGUID).Contains(config.EmailConfigurationGUID);

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

    /// <summary>
    /// Verification link for a new (changed) member email or an alternate email. Sent to the address being verified.
    /// </summary>
    public record MemberEmailVerificationEmail : ISystemEmailConfiguration
    {
        public static Guid GUID { get; } = new Guid("b6f1c2a4-5d3e-4f7a-9c8b-1e2d3f4a5b6c");
        public const string CodeName = "MemberEmailVerification-k3v9qx2m";

        public Guid EmailConfigurationGUID => GUID;

        public string EmailConfigurationName => CodeName;
    }

    /// <summary>
    /// Sent to a member's previous email after their account email changed.
    /// </summary>
    public record MemberEmailChangedNotificationEmail : ISystemEmailConfiguration
    {
        public static Guid GUID { get; } = new Guid("d2e8a7c1-3b4f-4e6d-8a9c-7f1e2d3c4b5a");
        public const string CodeName = "MemberEmailChangedNotification-p7w4rz5t";

        public Guid EmailConfigurationGUID => GUID;

        public string EmailConfigurationName => CodeName;
    }
}

public interface ISystemEmailConfiguration
{
    public Guid EmailConfigurationGUID { get; }
    public string EmailConfigurationName { get; }
}

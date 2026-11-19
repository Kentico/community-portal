namespace Kentico.Community.Portal.Core.DataProtection;

/// <summary>
/// Settings for recording <see cref="ConsentCodeNames.EMAIL_MARKETING"/> agreements from newsletter confirmations.
/// </summary>
public class EmailMarketingOptions
{
    public const string SECTION = "EmailMarketingOptions";

    /// <summary>
    /// When the Email marketing consent text went live on the sign-up forms (server local time,
    /// UTC in SaaS).
    /// </summary>
    public static readonly DateTime CONSENT_GO_LIVE = new(2026, 11, 10, 22, 30, 0);

    /// <summary>
    /// Newsletter confirmations dated before this never get an agreement: those subscribers saw the
    /// old sign-up text, not the Email marketing consent. Defaults to <see cref="CONSENT_GO_LIVE"/>;
    /// Development and CI set an earlier date so tests can seed confirmations on both sides.
    /// </summary>
    public DateTime AgreementCutoffDate { get; set; } = CONSENT_GO_LIVE;
}

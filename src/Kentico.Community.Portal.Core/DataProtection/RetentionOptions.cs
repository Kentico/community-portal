namespace Kentico.Community.Portal.Core.DataProtection;

/// <summary>
/// Retention periods for the nightly retention tasks. <c>/privacy</c> states the same periods;
/// change both together. Defaults: anonymous contacts 1 year; everything else that expires,
/// 3 years after the last activity.
/// </summary>
public class RetentionOptions
{
    public const string SECTION = "RetentionOptions";

    /// <summary>Contacts without an email are deleted after this many days without activity.</summary>
    public int AnonymousContactInactiveDays { get; set; } = 365;

    /// <summary>
    /// Contacts with an email that aren't members or active recipient list subscribers are deleted
    /// after this many days without activity.
    /// </summary>
    public int KnownContactInactiveDays { get; set; } = 1095;

    /// <summary>Activities older than this are deleted, except <see cref="KeptActivityTypes"/>.</summary>
    public int ActivityDays { get; set; } = 1095;

    /// <summary>
    /// Activity types the activity task never deletes: contact groups use member registration,
    /// and form submissions follow their form's own retention.
    /// </summary>
    public List<string> KeptActivityTypes { get; set; } = ["memberregistration", "bizformsubmit"];

    /// <summary>
    /// Activities deleted per statement by the activity retention task.
    /// 4000 keeps each DELETE under SQL Server's 5000-lock escalation threshold (row locks on the
    /// clustered key and each index, plus page intent locks), so live activity logging isn't
    /// blocked by a table lock, and each batch is a short transaction.
    /// </summary>
    public int ActivityDeleteBatchSize { get; set; } = 4000;

    /// <summary>Default for <see cref="FormRetentionPolicyTypes.SHORT_LIVED"/> forms without their own retention days.</summary>
    public int ShortLivedFormDefaultDays { get; set; } = 1095;
}

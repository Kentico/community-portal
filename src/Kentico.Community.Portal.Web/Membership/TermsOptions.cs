using System.ComponentModel.DataAnnotations;

namespace Kentico.Community.Portal.Web.Membership;

/// <summary>
/// The Terms of Use members accept at registration. Stored on the member, not as a consent.
/// </summary>
public class TermsOptions
{
    public const string SECTION = "TermsOptions";

    /// <summary>
    /// Version of the Terms shown at <c>/terms</c>. Stored as <see cref="CommunityMember.TermsAcceptedVersion" />.
    /// The <c>/terms</c> page shows it through <see cref="TermsVersionPlaceholder.TOKEN" />.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CurrentVersion { get; set; } = "";
}

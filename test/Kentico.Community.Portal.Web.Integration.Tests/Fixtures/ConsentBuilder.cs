using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;
using Kentico.Community.Portal.Core;

namespace Kentico.Community.Portal.Web.Tests.Fixtures;

/// <summary>
/// Builds a throwaway consent, so tests don't depend on (or change) the real consents' texts.
/// Use <see cref="Named" /> when the code under test looks a consent up by a fixed code name that
/// doesn't exist in the database yet.
/// </summary>
public class ConsentBuilder(PrivacyTestData data)
{
    private readonly PrivacyTestData data = data;

    private string? codeName;
    private string shortText = "<p>Integration test consent short text.</p>";
    private string fullText = "<p>Integration test consent full text.</p>";

    public ConsentBuilder Named(string consentCodeName)
    {
        codeName = consentCodeName;
        return this;
    }

    public ConsentBuilder WithText(string shortTextHtml, string fullTextHtml)
    {
        shortText = shortTextHtml;
        fullText = fullTextHtml;
        return this;
    }

    public ConsentInfo Build()
    {
        string name = codeName ?? data.UniqueName("Consent");

        var consent = new ConsentInfo
        {
            ConsentName = name,
            ConsentDisplayName = name,
        };
        consent.UpsertConsentText(PortalWebSiteChannel.DEFAULT_LANGUAGE, shortText, fullText);

        using (PrivacyTestData.WriteContext())
        {
            Service.Resolve<IInfoProvider<ConsentInfo>>().Set(consent);
        }

        return data.Track(consent);
    }
}

using Kentico.Community.Portal.Core.DataProtection;
using Microsoft.Extensions.Configuration;

namespace Kentico.Community.Portal.Web.Tests.Features.Newsletter;

/// <summary>
/// Deployed environments use the code default, so a missing key can't stop agreements from being
/// recorded. Development and CI move the cutoff earlier for the E2E specs.
/// </summary>
public class EmailMarketingOptionsTests
{
    [TestCase("Production")]
    [TestCase("qa")]
    [TestCase("uat")]
    public void Deployed_Environments_Use_The_Consent_Go_Live_Cutoff(string environment) =>
        Assert.That(Bind(environment).AgreementCutoffDate, Is.EqualTo(EmailMarketingOptions.CONSENT_GO_LIVE));

    [TestCase("Development")]
    [TestCase("CI")]
    public void Test_Environments_Use_An_Earlier_Cutoff(string environment) =>
        Assert.That(Bind(environment).AgreementCutoffDate, Is.LessThan(EmailMarketingOptions.CONSENT_GO_LIVE));

    /// <summary>
    /// The Web project's appsettings files are copied to this project's output by the project reference.
    /// </summary>
    private static EmailMarketingOptions Bind(string environment)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: false)
            .Build();

        var options = new EmailMarketingOptions();
        config.GetSection(EmailMarketingOptions.SECTION).Bind(options);

        return options;
    }
}

using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Tests.Infrastructure;

/// <summary>
/// appsettings use <c>ReCaptcha:ScoreThreshold</c>; the property must bind to it.
/// </summary>
public class ReCaptchaSettingsTests
{
    [Test]
    public void Binds_ScoreThreshold_From_Configuration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ReCaptcha:ScoreThreshold"] = "0.2" })
            .Build();
        var services = new ServiceCollection()
            .Configure<ReCaptchaSettings>(config.GetSection("ReCaptcha"))
            .BuildServiceProvider();

        var settings = services.GetRequiredService<IOptions<ReCaptchaSettings>>().Value;

        Assert.That(settings.ScoreThreshold, Is.EqualTo(0.2));
    }
}

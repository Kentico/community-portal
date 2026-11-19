using Kentico.OnlineMarketing.Web.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Tests.Configuration;

public class ServiceCollectionMembershipExtensionsTests
{
    [Test]
    public async Task Authenticated_Access_Denied_Returns_Forbidden_Without_Redirecting()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IMemberToContactMapper>());
        services.AddAppXperienceMembership();

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        var httpContext = new DefaultHttpContext();
        var redirectContext = new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            new AuthenticationScheme(
                IdentityConstants.ApplicationScheme,
                displayName: null,
                typeof(CookieAuthenticationHandler)),
            options,
            new AuthenticationProperties(),
            "/login");

        await options.Events.OnRedirectToAccessDenied(redirectContext);

        Assert.Multiple(() =>
        {
            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
            Assert.That(httpContext.Response.Headers.Location, Is.Empty);
            Assert.That(options.LoginPath.Value, Is.EqualTo("/login"));
        });
    }
}

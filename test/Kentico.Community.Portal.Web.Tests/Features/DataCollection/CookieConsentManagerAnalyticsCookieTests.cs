using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Community.Portal.Web.Tests.Features.DataCollection;

/// <summary>
/// GA cookie cleanup: only host-only <c>_ga</c>/<c>_ga_*</c> cookies are deleted. A delete
/// without a Domain never touches <c>.kentico.com</c> cookies, which kentico.com's banner governs.
/// </summary>
public class CookieConsentManagerAnalyticsCookieTests
{
    private DefaultHttpContext httpContext = null!;
    private CookieConsentManager sut = null!;

    [SetUp]
    public void SetUp()
    {
        httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "_ga=GA1.1.1.1; _ga_X720WCX590=GS1.1.1; _gid=GA1.1.2; kcp.cookieconsent=v2";

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var cookieLevelProvider = Substitute.For<ICurrentCookieLevelProvider>();
        var contactProvider = Substitute.For<IInfoProvider<ContactInfo>>();
        var readOnly = Substitute.For<IReadOnlyModeProvider>();

        sut = new CookieConsentManager(
            new ConsentRetriever(Substitute.For<IInfoProvider<ConsentInfo>>(), Substitute.For<IProgressiveCache>()),
            Substitute.For<IConsentAgreementService>(),
            cookieLevelProvider,
            Substitute.For<ICookieAccessor>(),
            httpContextAccessor,
            new MemberContactManager(
                contactProvider,
                Substitute.For<ICurrentContactProvider>(),
                cookieLevelProvider,
                Substitute.For<IContactMergeService>(),
                readOnly),
            Substitute.For<UserManager<CommunityMember>>(
                Substitute.For<IUserStore<CommunityMember>>(), null, null, null, null, null, null, null, null),
            readOnly,
            contactProvider,
            Substitute.For<ICurrentContactAccessor>(),
            TimeProvider.System,
            NullLogger<CookieConsentManager>.Instance);
    }

    [Test]
    public void RemoveHostAnalyticsCookies_Deletes_Host_Only_GA_Cookies()
    {
        sut.RemoveHostAnalyticsCookies();

        AssertDeletedHostOnly("_ga", "_ga_X720WCX590");
    }

    [Test]
    public void ForgetVisitor_Deletes_Host_Only_GA_Cookies()
    {
        sut.ForgetVisitor();

        AssertDeletedHostOnly("_ga", "_ga_X720WCX590");
    }

    private void AssertDeletedHostOnly(params string[] names)
    {
        string[] setCookies = [.. httpContext.Response.Headers.SetCookie.Select(v => v ?? "")];

        Assert.Multiple(() =>
        {
            Assert.That(setCookies.Select(v => v.Split('=')[0]), Is.EquivalentTo(names));
            Assert.That(setCookies, Has.All.Contains("expires=Thu, 01 Jan 1970"));
            Assert.That(setCookies, Has.All.Contains("path=/"));
            Assert.That(setCookies, Has.None.Contains("domain="));
        });
    }
}

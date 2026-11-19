using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Tests;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using CookieLevel = Kentico.Web.Mvc.CookieLevel;

namespace Kentico.Community.Portal.Web.Tests.Features.DataCollection;

/// <summary>
/// Reconciles the saved choice with the cookie level before the page renders. Every request reaches
/// the next middleware, even when reconciliation fails.
/// </summary>
public class CookieConsentMiddlewareTests : UnitTests
{
    private const string TRACKING_CHOICE = "v2|a=1|t=1|1760486400";

    private CookieConsentTestContext ctx = null!;
    private ContactInfo contact = null!;
    private ConsentInfo consent = null!;
    private bool nextCalled;
    private CookieConsentMiddleware sut = null!;

    [SetUp]
    public void SetUp()
    {
        var contactFake = Fake<ContactInfo, IInfoProvider<ContactInfo>>();
        contactFake.WithData();
        Fake<ConsentInfo>();
        contact = new ContactInfo { ContactID = 7, ContactGUID = Guid.NewGuid() };
        consent = new ConsentInfo { ConsentID = 3, ConsentName = ConsentCodeNames.WEBSITE_TRACKING };

        ctx = new CookieConsentTestContext((IInfoProvider<ContactInfo>)contactFake.ProviderObject);
        ctx.UseConsent(consent);
        ctx.UseCurrentContact(contact);
        ctx.HttpContext.Request.Path = "/q-and-a";

        var preview = Substitute.For<IPreviewFeature>();
        var pageBuilder = Substitute.For<IPageBuilderFeature>();
        ctx.HttpContext.Kentico().SetFeature(preview);
        ctx.HttpContext.Kentico().SetFeature(pageBuilder);

        nextCalled = false;
        sut = new CookieConsentMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<CookieConsentMiddleware>.Instance);
    }

    private Task Invoke() =>
        sut.InvokeAsync(
            ctx.HttpContext,
            ctx.Cookies,
            ctx.CookieLevel,
            ctx.Agreements,
            ctx.ConsentRetriever,
            ctx.ReadOnly,
            ctx.Manager,
            ctx.CurrentContact);

    private bool BannerShown => ctx.HttpContext.Items.ContainsKey(CookieConsentMiddleware.SHOW_BANNER_ITEM);

    [Test]
    public async Task Legacy_Consent_Cookies_Are_Removed()
    {
        ctx.HttpContext.Request.Headers.Cookie = "kenticocommunityportal.cookieconsentlevel=1000; kenticocommunityportal.cookielevelselection=1; other=1";

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.Received(1).Remove("kenticocommunityportal.cookieconsentlevel");
            ctx.Cookies.Received(1).Remove("kenticocommunityportal.cookielevelselection");
            ctx.Cookies.DidNotReceive().Remove("other");
            Assert.That(nextCalled, Is.True);
        });
    }

    [Test]
    public async Task No_Choice_Lowers_To_Essential_And_Shows_Banner()
    {
        ctx.UseCookieLevel(CookieLevel.All.Level);

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            ctx.Cookies.DidNotReceive().Remove(CookieNames.CONSENT);
            Assert.That(BannerShown, Is.True);
        });
    }

    [TestCase("garbage")]
    [TestCase("v2|a=1|t=1|999999999999999")]
    [TestCase("v2|a=1|t=1|99999999999999999999999")]
    public async Task Malformed_Or_Tampered_Choice_Is_Removed_And_Shows_Banner(string value)
    {
        ctx.UseConsentCookie(value);
        ctx.UseCookieLevel(CookieLevel.All.Level);

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.Received(1).Remove(CookieNames.CONSENT);
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            Assert.That(BannerShown, Is.True);
            Assert.That(nextCalled, Is.True);
        });
    }

    [Test]
    public async Task Provisional_Choice_After_Read_Only_Is_Forgotten()
    {
        ctx.UseConsentCookie("v2|a=1|t=0|1760486400|ro=1");

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.Received(1).Remove(CookieNames.CONSENT);
            Assert.That(BannerShown, Is.True);
        });
    }

    [Test]
    public async Task Provisional_Choice_During_Read_Only_Is_Kept()
    {
        ctx.ReadOnly.IsReadOnly.Returns(true);
        ctx.UseConsentCookie("v2|a=1|t=0|1760486400|ro=1");

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.DidNotReceive().Remove(CookieNames.CONSENT);
            Assert.That(BannerShown, Is.False);
        });
    }

    [Test]
    public async Task Pending_Revoke_After_Read_Only_Saves_Final_Choice_Without_Banner()
    {
        ctx.UseConsentCookie($"v2|a=1|t=0|1760486400|rv={Guid.NewGuid():D}");

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=1|t=0|1760486400", Arg.Any<Microsoft.AspNetCore.Http.CookieOptions>());
            ctx.Cookies.DidNotReceive().Remove(CookieNames.CONSENT);
            Assert.That(BannerShown, Is.False);
        });
    }

    [Test]
    public async Task Tracking_Choice_Without_Agreement_Is_Forgotten()
    {
        ctx.UseConsentCookie(TRACKING_CHOICE);
        ctx.UseCookieLevel(CookieLevel.All.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(false);

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.Received(1).Remove(CookieNames.CONSENT);
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            Assert.That(BannerShown, Is.True);
        });
    }

    [Test]
    public async Task Tracking_Choice_With_Lost_Level_Is_Forgotten()
    {
        ctx.UseConsentCookie(TRACKING_CHOICE);
        ctx.UseCookieLevel(CookieLevel.Essential.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(true);

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.Received(1).Remove(CookieNames.CONSENT);
            Assert.That(BannerShown, Is.True);
        });
    }

    [Test]
    public async Task Agreed_Tracking_Choice_Is_Unchanged()
    {
        ctx.UseConsentCookie(TRACKING_CHOICE);
        ctx.UseCookieLevel(CookieLevel.All.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(true);

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.DidNotReceiveWithAnyArgs().Remove(default!);
            ctx.Cookies.DidNotReceiveWithAnyArgs().Set(default!, default!, default);
            ctx.CookieLevel.DidNotReceiveWithAnyArgs().SetCurrentCookieLevel(default);
            Assert.That(BannerShown, Is.False);
            Assert.That(nextCalled, Is.True);
        });
    }

    [Test]
    public async Task Read_Only_Skips_Agreement_Check()
    {
        ctx.ReadOnly.IsReadOnly.Returns(true);
        ctx.UseConsentCookie(TRACKING_CHOICE);
        ctx.UseCookieLevel(CookieLevel.All.Level);

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Agreements.DidNotReceiveWithAnyArgs().IsAgreed(default!, default!);
            ctx.Cookies.DidNotReceive().Remove(CookieNames.CONSENT);
            ctx.CookieLevel.DidNotReceiveWithAnyArgs().SetCurrentCookieLevel(default);
            Assert.That(BannerShown, Is.False);
        });
    }

    [Test]
    public async Task Editor_Level_Is_Not_Lowered()
    {
        ctx.UseCookieLevel(CookieLevel.Editor.Level);

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.CookieLevel.DidNotReceiveWithAnyArgs().SetCurrentCookieLevel(default);
            Assert.That(BannerShown, Is.True);
        });
    }

    [Test]
    public async Task Failure_Falls_Back_To_Essential_With_Banner_And_Keeps_Choice()
    {
        ctx.UseConsentCookie(TRACKING_CHOICE);
        ctx.UseCookieLevel(CookieLevel.All.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(_ => throw new InvalidOperationException("database down"));

        await Invoke();

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            ctx.Cookies.DidNotReceive().Remove(CookieNames.CONSENT);
            Assert.That(BannerShown, Is.True);
        });
    }

    [TestCase("/getmedia/abc/file.png")]
    [TestCase("/getContentAsset/abc/def/file.webp")]
    [TestCase("/member/avatar/12")]
    [TestCase("/notification/ClearNotification")]
    [TestCase("/admin/anything")]
    public async Task Non_Page_Paths_Are_Skipped(string path)
    {
        ctx.HttpContext.Request.Path = path;
        ctx.UseCookieLevel(CookieLevel.All.Level);

        await Invoke();

        Assert.Multiple(() =>
        {
            ctx.Cookies.DidNotReceiveWithAnyArgs().Get(default!);
            ctx.CookieLevel.DidNotReceiveWithAnyArgs().SetCurrentCookieLevel(default);
            Assert.That(BannerShown, Is.False);
            Assert.That(nextCalled, Is.True);
        });
    }

    [Test]
    public async Task Consent_Endpoint_Is_Reconciled()
    {
        ctx.HttpContext.Request.Path = "/cookies/consent";
        ctx.UseConsentCookie($"v2|a=0|t=0|1760486400|rv={Guid.NewGuid():D}");

        await Invoke();

        ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=0|t=0|1760486400", Arg.Any<Microsoft.AspNetCore.Http.CookieOptions>());
    }
}

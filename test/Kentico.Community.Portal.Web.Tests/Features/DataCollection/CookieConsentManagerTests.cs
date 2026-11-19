using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Tests;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Microsoft.AspNetCore.Http;
using CookieLevel = Kentico.Web.Mvc.CookieLevel;

namespace Kentico.Community.Portal.Web.Tests.Features.DataCollection;

/// <summary>
/// Outside read-only mode: Accept agrees once on the visitor's contact, Necessary only revokes an
/// active agreement, analytics is a cookie-only record, and a read-only pending revoke completes.
/// </summary>
public class CookieConsentManagerTests : UnitTests
{
    private static readonly Guid contactGuid = Guid.Parse("8b0bd2f4-4c7e-4d0c-9d2c-5b1d5a3f1e01");

    private CookieConsentTestContext ctx = null!;
    private ContactInfo contact = null!;
    private ConsentInfo consent = null!;

    [SetUp]
    public void SetUp()
    {
        var contactFake = Fake<ContactInfo, IInfoProvider<ContactInfo>>();
        Fake<ConsentInfo>();
        contact = new ContactInfo { ContactID = 7, ContactGUID = contactGuid };
        consent = new ConsentInfo { ConsentID = 3, ConsentName = ConsentCodeNames.WEBSITE_TRACKING };
        contactFake.WithData(contact);

        ctx = new CookieConsentTestContext((IInfoProvider<ContactInfo>)contactFake.ProviderObject);
        ctx.UseConsent(consent);
        ctx.UseCurrentContact(contact);
    }

    [Test]
    public async Task Accept_Raises_Level_Agrees_And_Saves_Choice()
    {
        ctx.Agreements.IsAgreed(contact, consent).Returns(false);

        var choice = await ctx.Manager.Apply(analytics: true, tracking: true);

        Assert.Multiple(() =>
        {
            Assert.That(choice, Is.EqualTo(new ConsentCookie(true, true, CookieConsentTestContext.Now)));
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.All.Level);
            ctx.Agreements.Received(1).Agree(contact, consent);
            ctx.Cookies.Received(1).Set(
                CookieNames.CONSENT,
                "v2|a=1|t=1|1760486400",
                Arg.Is<CookieOptions>(o => o.HttpOnly && o.Secure && o.IsEssential && o.Expires == CookieConsentTestContext.Now.AddDays(365)));
        });
    }

    [Test]
    public async Task Accept_Does_Not_Agree_Twice()
    {
        ctx.Agreements.IsAgreed(contact, consent).Returns(true);

        _ = await ctx.Manager.Apply(analytics: true, tracking: true);

        Assert.Multiple(() =>
        {
            ctx.Agreements.DidNotReceiveWithAnyArgs().Agree(default!, default!);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=1|t=1|1760486400", Arg.Any<CookieOptions>());
        });
    }

    [Test]
    public async Task Accept_Without_Consent_Still_Saves_Choice()
    {
        ctx.UseConsent(null);

        var choice = await ctx.Manager.Apply(analytics: false, tracking: true);

        Assert.Multiple(() =>
        {
            Assert.That(choice.Tracking, Is.True);
            ctx.Agreements.DidNotReceiveWithAnyArgs().Agree(default!, default!);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=0|t=1|1760486400", Arg.Any<CookieOptions>());
        });
    }

    [Test]
    public async Task Necessary_Only_Revokes_Active_Agreement_And_Lowers_Level()
    {
        ctx.UseCookieLevel(CookieLevel.All.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(true);

        var choice = await ctx.Manager.Apply(analytics: false, tracking: false);

        Assert.Multiple(() =>
        {
            Assert.That(choice, Is.EqualTo(new ConsentCookie(false, false, CookieConsentTestContext.Now)));
            ctx.Agreements.Received(1).Revoke(contact, consent);
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=0|t=0|1760486400", Arg.Any<CookieOptions>());
        });
    }

    [Test]
    public async Task Necessary_Only_Below_All_Reads_No_Contact_And_Revokes_Nothing()
    {
        ctx.UseCookieLevel(CookieLevel.Essential.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(true);

        _ = await ctx.Manager.Apply(analytics: false, tracking: false);

        Assert.Multiple(() =>
        {
            ctx.CurrentContact.DidNotReceiveWithAnyArgs().GetCurrentContact(default);
            ctx.Agreements.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
        });
    }

    [Test]
    public async Task Analytics_Only_Saves_Cookie_Only_Choice()
    {
        ctx.UseCookieLevel(CookieLevel.Essential.Level);

        var choice = await ctx.Manager.Apply(analytics: true, tracking: false);

        Assert.Multiple(() =>
        {
            Assert.That(choice, Is.EqualTo(new ConsentCookie(true, false, CookieConsentTestContext.Now)));
            ctx.CookieLevel.DidNotReceive().SetCurrentCookieLevel(CookieLevel.All.Level);
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            ctx.Agreements.DidNotReceiveWithAnyArgs().Agree(default!, default!);
            ctx.Agreements.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=1|t=0|1760486400", Arg.Any<CookieOptions>());
        });
    }

    [Test]
    public async Task Complete_Pending_Revoke_Revokes_Agreement_And_Saves_Final_Choice()
    {
        ctx.Agreements.IsAgreed(Arg.Is<ContactInfo>(c => c.ContactGUID == contactGuid), consent).Returns(true);
        var pending = new ConsentCookie(true, false, CookieConsentTestContext.Now.AddDays(-2), PendingRevoke: contactGuid);

        var final = await ctx.Manager.CompletePendingRevoke(pending);

        Assert.Multiple(() =>
        {
            Assert.That(final, Is.EqualTo(pending with { PendingRevoke = null }));
            ctx.Agreements.Received(1).Revoke(Arg.Is<ContactInfo>(c => c.ContactGUID == contactGuid), consent);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=1|t=0|1760313600", Arg.Any<CookieOptions>());
        });
    }

    [Test]
    public async Task Complete_Pending_Revoke_Skips_Revoke_When_Not_Agreed()
    {
        ctx.Agreements.IsAgreed(Arg.Any<ContactInfo>(), consent).Returns(false);
        var pending = new ConsentCookie(false, false, CookieConsentTestContext.Now, PendingRevoke: contactGuid);

        var final = await ctx.Manager.CompletePendingRevoke(pending);

        Assert.Multiple(() =>
        {
            Assert.That(final.PendingRevoke, Is.Null);
            ctx.Agreements.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=0|t=0|1760486400", Arg.Any<CookieOptions>());
        });
    }

    [Test]
    public async Task Complete_Pending_Revoke_For_Unknown_Contact_Saves_Final_Choice()
    {
        var pending = new ConsentCookie(false, false, CookieConsentTestContext.Now, PendingRevoke: Guid.NewGuid());

        var final = await ctx.Manager.CompletePendingRevoke(pending);

        Assert.Multiple(() =>
        {
            Assert.That(final.PendingRevoke, Is.Null);
            ctx.Agreements.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=0|t=0|1760486400", Arg.Any<CookieOptions>());
        });
    }
}

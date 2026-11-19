using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Tests;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Http;
using CookieLevel = Kentico.Web.Mvc.CookieLevel;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Kentico.Community.Portal.Web.Tests.Features.DataCollection;

/// <summary>
/// Read-only mode: tracking can't turn on (no agreement can be recorded). A choice that asked for
/// tracking is saved as provisional (analytics only, asked again after read-only); Necessary only is
/// final, or carries a pending revoke when an active agreement can't be revoked yet. Nothing throws.
/// </summary>
public class CookieConsentManagerReadOnlyTests : UnitTests
{
    private CookieConsentTestContext ctx = null!;
    private IInfoProvider<ContactInfo> contactProvider = null!;
    private ContactInfo contact = null!;
    private ConsentInfo consent = null!;

    [SetUp]
    public void SetUp()
    {
        var contactFake = Fake<ContactInfo, IInfoProvider<ContactInfo>>();
        contactFake.WithData();
        contactProvider = (IInfoProvider<ContactInfo>)contactFake.ProviderObject;
        Fake<ConsentInfo>();
        contact = new ContactInfo { ContactID = 7, ContactGUID = Guid.Parse("8b0bd2f4-4c7e-4d0c-9d2c-5b1d5a3f1e01") };
        consent = new ConsentInfo { ConsentID = 3, ConsentName = ConsentCodeNames.WEBSITE_TRACKING };

        ctx = new CookieConsentTestContext(contactProvider);
        ctx.ReadOnly.IsReadOnly.Returns(true);
        ctx.UseConsent(consent);
        ctx.UseCurrentContact(contact);
    }

    [Test]
    public async Task Accept_Saves_Provisional_Analytics_Only_Choice_And_Keeps_Essential()
    {
        var choice = await ctx.Manager.Apply(analytics: true, tracking: true);

        Assert.Multiple(() =>
        {
            Assert.That(choice.Analytics, Is.True);
            Assert.That(choice.Tracking, Is.False);
            Assert.That(choice.Provisional, Is.True);
            ctx.CookieLevel.DidNotReceive().SetCurrentCookieLevel(CookieLevel.All.Level);
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            ctx.Cookies.Received(1).Set(
                CookieNames.CONSENT,
                "v2|a=1|t=0|1760486400|ro=1",
                Arg.Is<CookieOptions>(o => o.Secure && o.SameSite == SameSiteMode.Lax));
            ctx.Agreements.DidNotReceiveWithAnyArgs().Agree(default!, default!);
        });
    }

    [Test]
    public async Task Necessary_Only_For_Agreed_Contact_Saves_Pending_Revoke_Without_Revoke()
    {
        ctx.UseCookieLevel(CookieLevel.All.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(true);

        var choice = await ctx.Manager.Apply(analytics: false, tracking: false);

        Assert.Multiple(() =>
        {
            Assert.That(choice.PendingRevoke, Is.EqualTo(contact.ContactGUID));
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            ctx.Cookies.Received(1).Set(
                CookieNames.CONSENT,
                $"v2|a=0|t=0|1760486400|rv={contact.ContactGUID:D}",
                Arg.Any<CookieOptions>());
            ctx.Agreements.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
        });
    }

    [Test]
    public async Task Necessary_Only_Without_Agreement_Saves_Final_Choice()
    {
        ctx.UseCookieLevel(CookieLevel.All.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(false);

        var choice = await ctx.Manager.Apply(analytics: false, tracking: false);

        Assert.Multiple(() =>
        {
            Assert.That(choice.PendingRevoke, Is.Null);
            Assert.That(choice.Provisional, Is.False);
            ctx.CookieLevel.Received(1).SetCurrentCookieLevel(CookieLevel.Essential.Level);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=0|t=0|1760486400", Arg.Any<CookieOptions>());
            ctx.Agreements.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
        });
    }

    [Test]
    public async Task Accept_With_Active_Agreement_Keeps_Tracking_On()
    {
        ctx.UseCookieLevel(CookieLevel.All.Level);
        ctx.Agreements.IsAgreed(contact, consent).Returns(true);

        var choice = await ctx.Manager.Apply(analytics: true, tracking: true);

        Assert.Multiple(() =>
        {
            Assert.That(choice, Is.EqualTo(new ConsentCookie(true, true, CookieConsentTestContext.Now)));
            ctx.CookieLevel.DidNotReceive().SetCurrentCookieLevel(Arg.Any<int>());
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=1|t=1|1760486400", Arg.Any<CookieOptions>());
            ctx.Agreements.DidNotReceiveWithAnyArgs().Agree(default!, default!);
        });
    }

    [Test]
    public async Task Accept_After_Withdrawal_Drops_Pending_Revoke()
    {
        ctx.UseConsentCookie($"v2|a=0|t=0|1760486400|rv={contact.ContactGUID:D}");

        var choice = await ctx.Manager.Apply(analytics: true, tracking: true);

        Assert.Multiple(() =>
        {
            Assert.That(choice.PendingRevoke, Is.Null);
            Assert.That(choice.Provisional, Is.True);
            ctx.Cookies.Received(1).Set(CookieNames.CONSENT, "v2|a=1|t=0|1760486400|ro=1", Arg.Any<CookieOptions>());
            ctx.Agreements.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
        });
    }

    [Test]
    public async Task Necessary_Only_Keeps_Earlier_Pending_Revoke()
    {
        ctx.UseConsentCookie($"v2|a=1|t=0|1760486400|rv={contact.ContactGUID:D}");

        var choice = await ctx.Manager.Apply(analytics: false, tracking: false);

        Assert.That(choice.PendingRevoke, Is.EqualTo(contact.ContactGUID));
    }

    [Test]
    public async Task Settings_Save_Keeps_Active_Tracking()
    {
        ctx.UseCookieLevel(CookieLevel.All.Level);
        ctx.UseConsentCookie("v2|a=1|t=1|1760486400");
        ctx.Agreements.IsAgreed(contact, consent).Returns(true);

        var choice = await ctx.Manager.ApplyAnalyticsOnly(analytics: false);

        Assert.Multiple(() =>
        {
            Assert.That(choice, Is.EqualTo(new ConsentCookie(false, true, CookieConsentTestContext.Now)));
            ctx.CookieLevel.DidNotReceive().SetCurrentCookieLevel(Arg.Any<int>());
        });
    }

    [Test]
    public async Task Settings_Save_Keeps_Provisional_Choice()
    {
        ctx.UseConsentCookie("v2|a=1|t=0|1760486400|ro=1");

        var choice = await ctx.Manager.ApplyAnalyticsOnly(analytics: false);

        Assert.That(choice, Is.EqualTo(new ConsentCookie(false, false, CookieConsentTestContext.Now, Provisional: true)));
    }

    [Test]
    public async Task Settings_Save_Keeps_Pending_Revoke()
    {
        ctx.UseConsentCookie($"v2|a=0|t=0|1760486400|rv={contact.ContactGUID:D}");

        var choice = await ctx.Manager.ApplyAnalyticsOnly(analytics: true);

        Assert.That(choice, Is.EqualTo(new ConsentCookie(true, false, CookieConsentTestContext.Now, PendingRevoke: contact.ContactGUID)));
    }

    [Test]
    public async Task Complete_Pending_Revoke_Does_Nothing()
    {
        var pending = new ConsentCookie(false, false, CookieConsentTestContext.Now, PendingRevoke: contact.ContactGUID);

        var result = await ctx.Manager.CompletePendingRevoke(pending);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(pending));
            ctx.Agreements.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
            ctx.Cookies.DidNotReceiveWithAnyArgs().Set(default!, default!, default);
        });
    }

    [Test]
    public async Task Sign_In_Changes_Nothing()
    {
        await ctx.Manager.OnMemberSignedIn(new CommunityMember { Email = "member@kentico.local", UserName = "member" });

        Assert.Multiple(() =>
        {
            ctx.CookieLevel.DidNotReceive().SetCurrentCookieLevel(Arg.Any<int>());
            ctx.Cookies.DidNotReceiveWithAnyArgs().Set(default!, default!, default);
            ctx.Agreements.DidNotReceiveWithAnyArgs().Agree(default!, default!);
            Assert.That(contactProvider.Get().Count, Is.Zero, "no contact written");
        });
    }
}

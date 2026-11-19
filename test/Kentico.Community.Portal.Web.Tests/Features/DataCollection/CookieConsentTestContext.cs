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
/// Substitutes behind <see cref="CookieConsentManager"/> and <see cref="CookieConsentMiddleware"/>.
/// The Website tracking consent comes from the substituted cache (<see cref="UseConsent"/>).
/// </summary>
internal sealed class CookieConsentTestContext
{
    /// <summary>2025-10-15T00:00:00Z</summary>
    public static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1760486400);

    public ICookieAccessor Cookies { get; } = Substitute.For<ICookieAccessor>();
    public ICurrentCookieLevelProvider CookieLevel { get; } = Substitute.For<ICurrentCookieLevelProvider>();
    public IConsentAgreementService Agreements { get; } = Substitute.For<IConsentAgreementService>();
    public IReadOnlyModeProvider ReadOnly { get; } = Substitute.For<IReadOnlyModeProvider>();
    public ICurrentContactAccessor CurrentContact { get; } = Substitute.For<ICurrentContactAccessor>();
    public IProgressiveCache Cache { get; } = Substitute.For<IProgressiveCache>();
    public DefaultHttpContext HttpContext { get; } = new();
    public ConsentRetriever ConsentRetriever { get; }
    public CookieConsentManager Manager { get; }

    public CookieConsentTestContext(IInfoProvider<ContactInfo> contactProvider)
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(HttpContext);

        // A missing cookie is null (NSubstitute would return "")
        Cookies.Get(Arg.Any<string>()).Returns((string)null!);

        ConsentRetriever = new ConsentRetriever(Substitute.For<IInfoProvider<ConsentInfo>>(), Cache);

        Manager = new CookieConsentManager(
            ConsentRetriever,
            Agreements,
            CookieLevel,
            Cookies,
            httpContextAccessor,
            new MemberContactManager(
                contactProvider,
                Substitute.For<ICurrentContactProvider>(),
                CookieLevel,
                Substitute.For<IContactMergeService>(),
                ReadOnly),
            Substitute.For<UserManager<CommunityMember>>(
                Substitute.For<IUserStore<CommunityMember>>(), null, null, null, null, null, null, null, null),
            ReadOnly,
            contactProvider,
            CurrentContact,
            new FixedTimeProvider(Now),
            NullLogger<CookieConsentManager>.Instance);
    }

    /// <summary>The Website tracking consent, or null when it doesn't exist.</summary>
    public void UseConsent(ConsentInfo? consent) =>
        Cache
            .LoadAsync(Arg.Any<Func<CacheSettings, Task<ConsentInfo>>>(), Arg.Any<CacheSettings>())
            .Returns(Task.FromResult(consent!));

    /// <summary>The visitor's current contact (both <c>createAnonymous</c> values).</summary>
    public void UseCurrentContact(ContactInfo? contact) =>
        CurrentContact.GetCurrentContact(Arg.Any<bool>()).Returns(contact);

    public void UseCookieLevel(int level) =>
        CookieLevel.GetCurrentCookieLevel().Returns(level);

    public void UseConsentCookie(string? value) =>
        Cookies.Get(CookieNames.CONSENT).Returns(value!);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

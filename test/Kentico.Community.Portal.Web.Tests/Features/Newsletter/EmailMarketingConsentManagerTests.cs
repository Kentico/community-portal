using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Tests;
using Kentico.Community.Portal.Core.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Community.Portal.Web.Tests.Features.Newsletter;

/// <summary>
/// Agree/revoke are idempotent (inline calls, the event handler and the task all run for one
/// subscription), no-op in read-only mode, and never throw when the consent is missing.
/// </summary>
public class EmailMarketingConsentManagerTests : UnitTests
{
    private IInfoProvider<ConsentInfo> consentProvider = null!;
    private IConsentAgreementService agreementService = null!;
    private IReadOnlyModeProvider readOnly = null!;
    private EmailMarketingConsentManager sut = null!;
    private ContactInfo contact = null!;
    private ConsentInfo consent = null!;

    [SetUp]
    public void SetUp()
    {
        Fake<ContactInfo>();
        Fake<ConsentInfo>();
        contact = new ContactInfo();
        consent = new ConsentInfo { ConsentName = ConsentCodeNames.EMAIL_MARKETING };
        consentProvider = Substitute.For<IInfoProvider<ConsentInfo>, IInfoByNameProvider<ConsentInfo>>();
        ConsentByName().GetAsync(default!, default).ReturnsForAnyArgs(consent);
        agreementService = Substitute.For<IConsentAgreementService>();
        readOnly = Substitute.For<IReadOnlyModeProvider>();

        sut = new EmailMarketingConsentManager(consentProvider, agreementService, readOnly, NullLogger<EmailMarketingConsentManager>.Instance);
    }

    /// <summary>
    /// <c>GetAsync(codeName)</c> is an extension that needs the provider to resolve by name.
    /// </summary>
    private IInfoByNameProvider<ConsentInfo> ConsentByName() => (IInfoByNameProvider<ConsentInfo>)consentProvider;

    [Test]
    public async Task Agree_Records_Agreement_When_Not_Agreed()
    {
        agreementService.IsAgreed(contact, consent).Returns(false);

        bool agreed = await sut.Agree(contact);

        Assert.Multiple(() =>
        {
            Assert.That(agreed, Is.True);
            agreementService.Received(1).Agree(contact, consent);
        });
    }

    [Test]
    public async Task Agree_Does_Nothing_When_Already_Agreed()
    {
        agreementService.IsAgreed(contact, consent).Returns(true);

        bool agreed = await sut.Agree(contact);

        Assert.Multiple(() =>
        {
            Assert.That(agreed, Is.False);
            agreementService.DidNotReceiveWithAnyArgs().Agree(default!, default!);
        });
    }

    [Test]
    public async Task Revoke_Only_Revokes_An_Active_Agreement()
    {
        agreementService.IsAgreed(contact, consent).Returns(false, true);

        bool first = await sut.Revoke(contact);
        bool second = await sut.Revoke(contact);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.False);
            Assert.That(second, Is.True);
            agreementService.Received(1).Revoke(contact, consent);
        });
    }

    [Test]
    public async Task Read_Only_Mode_Writes_Nothing()
    {
        readOnly.IsReadOnly.Returns(true);
        agreementService.IsAgreed(contact, consent).Returns(true);

        bool agreed = await sut.Agree(contact);
        bool revoked = await sut.Revoke(contact);

        Assert.Multiple(() =>
        {
            Assert.That(agreed, Is.False);
            Assert.That(revoked, Is.False);
            agreementService.DidNotReceiveWithAnyArgs().Agree(default!, default!);
            agreementService.DidNotReceiveWithAnyArgs().Revoke(default!, default!);
        });
    }

    [Test]
    public async Task Missing_Consent_Writes_Nothing_And_Does_Not_Throw()
    {
        ConsentByName().GetAsync(default!, default).ReturnsForAnyArgs(Task.FromResult<ConsentInfo>(null!));

        bool agreed = await sut.Agree(contact);
        bool revoked = await sut.Revoke(contact);

        Assert.Multiple(() =>
        {
            Assert.That(agreed, Is.False);
            Assert.That(revoked, Is.False);
            agreementService.DidNotReceiveWithAnyArgs().Agree(default!, default!);
        });
    }
}

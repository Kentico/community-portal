using CMS.Base;
using CMS.ContactManagement;
using CMS.Core;
using CMS.Core.Internal;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.EmailMarketing;
using CMS.EmailMarketing.Internal;
using CMS.Tests;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Membership;
using Kentico.Community.Portal.Web.Tests.Features.Blog;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Community.Portal.Web.Tests.Fixtures;

/// <summary>
/// Proves the privacy fixtures create what they claim and clean up after themselves, so tests
/// built on them can trust their setup.
/// </summary>
public class PrivacyFixturesIntegrationTests : IntegrationTests
{
    private PrivacyTestData data = null!;

    protected override void RegisterTestServices()
    {
        Service.Use<IDateTimeNowService, MockDateTimeNowService>();

        base.RegisterTestServices();
    }

    [SetUp]
    public void SetUp() => data = new PrivacyTestData();

    [TearDown]
    public void TearDown() => data.Dispose();

    [Test]
    public void Contact_With_Agreement_Is_Agreed_Until_Revoked()
    {
        var consent = data.Consent().Build();

        var agreed = data.Contact().AgreedTo(consent).Build();
        var revoked = data.Contact().WithEmail().RevokedFrom(consent).Build();

        var service = Service.Resolve<IConsentAgreementService>();

        Assert.Multiple(() =>
        {
            Assert.That(service.IsAgreed(agreed, consent), Is.True);
            Assert.That(service.IsAgreed(revoked, consent), Is.False);
        });
    }

    [Test]
    public async Task Subscribed_Contact_Is_Reported_By_RecipientListManager()
    {
        var list = data.RecipientList().Build();
        var subscriber = data.Contact().WithEmail().SubscribedTo(list).Build();
        var pending = data.Contact().WithEmail().SubscribedTo(list, approved: false).Build();

        var manager = CreateRecipientListManager();

        var subscribedIDs = await manager.GetSubscribedRecipientListIDs(subscriber.ContactEmail);
        var pendingIDs = await manager.GetSubscribedRecipientListIDs(pending.ContactEmail);

        Assert.Multiple(() =>
        {
            Assert.That(subscribedIDs, Does.Contain(list.ContactGroupID));
            Assert.That(pendingIDs, Is.Empty);
        });
    }

    [Test]
    public async Task ReadOnlyModeScope_Switches_Provider_And_Restores_It()
    {
        var list = data.RecipientList().Build();
        string email = data.UniqueEmail();
        var provider = Service.Resolve<IReadOnlyModeProvider>();
        var manager = CreateRecipientListManager();

        bool before = provider.IsReadOnly;

        using (new ReadOnlyModeScope())
        {
            Assert.That(provider.IsReadOnly, Is.True);

            // Write paths must no-op rather than throw
            await manager.Subscribe(email, list.ContactGroupID);
        }

        var subscribedIDs = await manager.GetSubscribedRecipientListIDs(email);

        Assert.Multiple(() =>
        {
            Assert.That(provider.IsReadOnly, Is.EqualTo(before));
            Assert.That(subscribedIDs, Is.Empty);
        });
    }

    [Test]
    public void Dispose_Deletes_Created_Rows()
    {
        var local = new PrivacyTestData();
        var consent = local.Consent().Build();
        var list = local.RecipientList().Build();
        var contact = local.Contact().WithEmail().AgreedTo(consent).SubscribedTo(list).Build();

        local.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(Service.Resolve<IInfoProvider<ContactInfo>>().Get(contact.ContactID), Is.Null);
            Assert.That(Service.Resolve<IInfoProvider<ContactGroupInfo>>().Get(list.ContactGroupID), Is.Null);
            Assert.That(Service.Resolve<IInfoProvider<ConsentInfo>>().Get(consent.ConsentID), Is.Null);
        });
    }

    private static RecipientListManager CreateRecipientListManager() =>
        new(
            Service.Resolve<IInfoProvider<ContactGroupInfo>>(),
            Service.Resolve<IInfoProvider<ContactInfo>>(),
            Service.Resolve<IInfoProvider<ContactGroupMemberInfo>>(),
            Service.Resolve<IInfoProvider<EmailSubscriptionConfirmationInfo>>(),
            Service.Resolve<IEmailSubscriptionConfirmationService>(),
            new EmailMarketingConsentManager(
                Service.Resolve<IInfoProvider<ConsentInfo>>(),
                Service.Resolve<IConsentAgreementService>(),
                Service.Resolve<IReadOnlyModeProvider>(),
                NullLogger<EmailMarketingConsentManager>.Instance),
            Service.Resolve<IReadOnlyModeProvider>());
}

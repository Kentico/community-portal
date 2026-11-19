using CMS.DataEngine;
using CMS.Membership;
using CMS.Tests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.DataProtection;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Community.Portal.Web.Tests.Features.DataProtection;

/// <summary>
/// Erasure finds the member by email, so a failed avatar delete must surface before the identity is
/// replaced: the admin sees the failure and running the erasure again finds the member.
/// </summary>
public class MemberAnonymizerTests : UnitTests
{
    private const string EMAIL = "member@example.com";

    private IInfoProvider<MemberInfo> memberProvider = null!;
    private ILuceneClient luceneClient = null!;
    private MemberInfo member = null!;

    [SetUp]
    public void SetUp()
    {
        Fake<MemberInfo>();
        member = new MemberInfo { MemberID = 42, MemberEmail = EMAIL, MemberName = "member" };
        memberProvider = Substitute.For<IInfoProvider<MemberInfo>>();
        luceneClient = Substitute.For<ILuceneClient>();
    }

    [Test]
    public async Task Avatar_Failure_Throws_Before_The_Member_Is_Anonymized()
    {
        var sut = new FailingAvatarAnonymizer(memberProvider, luceneClient);

        _ = await Assert.ThrowsAsync<PersonalDataErasureException>(() => sut.Anonymize(member, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(member.MemberEmail, Is.EqualTo(EMAIL));
            Assert.That(member.MemberName, Is.EqualTo("member"));
            memberProvider.DidNotReceiveWithAnyArgs().Set(default!);
            _ = luceneClient.DidNotReceiveWithAnyArgs().Rebuild(default!, default);
        });
    }

    /// <summary>Stands in for a Content hub delete that fails (permissions, references).</summary>
    private sealed class FailingAvatarAnonymizer(IInfoProvider<MemberInfo> memberProvider, ILuceneClient luceneClient)
        : MemberAnonymizer(
            memberProvider,
            Substitute.For<IInfoProvider<MemberExternalLoginInfo>>(),
            Substitute.For<IInfoProvider<QAndAAnswerDataInfo>>(),
            new MemberContentItemsQuery(),
            null!,
            luceneClient)
    {
        protected override Task DeleteAvatars(int memberID, CancellationToken cancellationToken) =>
            throw new PersonalDataErasureException(
                $"Could not delete avatar content item 7 (en) of member {memberID}.",
                new UnauthorizedAccessException());
    }
}
